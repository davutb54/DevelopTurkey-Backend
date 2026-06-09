using Business.Abstract;
using Core.Entities.Concrete;
using Core.Utilities.Authorization;
using Core.Utilities.Context;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class OfficialResponseManager : IOfficialResponseService
{
    private readonly IOfficialResponseDal _officialResponseDal;
    private readonly IUserTitleDal _userTitleDal;
    private readonly IProblemDal _problemDal;
    private readonly IClientContext _clientContext;
    private readonly ICapabilityPolicy _capabilityPolicy;
    private readonly INotificationService _notificationService;

    public OfficialResponseManager(
        IOfficialResponseDal officialResponseDal,
        IUserTitleDal userTitleDal,
        IProblemDal problemDal,
        IClientContext clientContext,
        ICapabilityPolicy capabilityPolicy,
        INotificationService notificationService)
    {
        _officialResponseDal = officialResponseDal;
        _userTitleDal = userTitleDal;
        _problemDal = problemDal;
        _clientContext = clientContext;
        _capabilityPolicy = capabilityPolicy;
        _notificationService = notificationService;
    }

    public IDataResult<List<OfficialResponseDto>> GetByProblem(int problemId)
    {
        var responses = _officialResponseDal.GetDetails(r => r.ProblemId == problemId);

        if (responses.Count > 0)
        {
            var authorIds = responses.Select(r => r.AuthorUserId).Distinct().ToList();
            var allTitles = _userTitleDal.GetAll(t => authorIds.Contains(t.UserId) && t.IsVisible);
            var titlesByUser = allTitles.GroupBy(t => t.UserId).ToDictionary(
                g => g.Key,
                g => g.Select(t => new UserTitleDto
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    Label = t.Label,
                    Kind = t.Kind,
                    Color = t.Color,
                    Icon = t.Icon,
                    IsVisible = t.IsVisible,
                    AssignedAt = t.AssignedAt
                }).ToList());

            foreach (var r in responses)
                r.AuthorTitles = titlesByUser.GetValueOrDefault(r.AuthorUserId, new List<UserTitleDto>());
        }

        return new SuccessDataResult<List<OfficialResponseDto>>(responses);
    }

    public IResult Add(OfficialResponseAddDto dto)
    {
        _capabilityPolicy.Require("official.response_create");

        var authorId = _clientContext.GetUserId() ?? 0;
        var institutionId = _clientContext.GetInstitutionId() ?? 0;

        var response = new OfficialResponse
        {
            ProblemId = dto.ProblemId,
            AuthorUserId = authorId,
            InstitutionId = institutionId,
            Body = dto.Body,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow
        };

        _officialResponseDal.Add(response);

        try
        {
            var problem = _problemDal.Get(p => p.Id == dto.ProblemId);
            if (problem != null && problem.SenderId != authorId)
            {
                _notificationService.Add(new Notification
                {
                    UserId = problem.SenderId,
                    Title = "Sorununuza resmi yanıt geldi",
                    Message = $"\"{problem.Title}\" sorununuz için kurum yanıtı eklendi.",
                    Type = "OfficialResponse",
                    ReferenceLink = $"/problem/{dto.ProblemId}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        catch { /* bildirim kritik değil */ }

        return new SuccessResult("Resmi yanıt eklendi.");
    }

    public IResult UpdateStatus(int id, OfficialResponseUpdateStatusDto dto)
    {
        _capabilityPolicy.Require("official.response_update");

        var response = _officialResponseDal.Get(r => r.Id == id);
        if (response == null) return new ErrorResult("Yanıt bulunamadı.");

        var institutionId = _clientContext.GetInstitutionId();
        if (institutionId.HasValue && response.InstitutionId != institutionId.Value)
            return new ErrorResult("Bu yanıtı güncelleme yetkiniz yok.");

        response.Status = dto.Status;
        if (!string.IsNullOrWhiteSpace(dto.Body))
            response.Body = dto.Body;
        response.UpdatedAt = DateTime.UtcNow;

        _officialResponseDal.Update(response);
        return new SuccessResult("Yanıt güncellendi.");
    }

    public IResult Delete(int id)
    {
        _capabilityPolicy.Require("official.response_create");

        var response = _officialResponseDal.Get(r => r.Id == id);
        if (response == null) return new ErrorResult("Yanıt bulunamadı.");

        var institutionId = _clientContext.GetInstitutionId();
        if (institutionId.HasValue && response.InstitutionId != institutionId.Value)
            return new ErrorResult("Bu yanıtı silme yetkiniz yok.");

        _officialResponseDal.Delete(response);
        return new SuccessResult("Yanıt silindi.");
    }
}
