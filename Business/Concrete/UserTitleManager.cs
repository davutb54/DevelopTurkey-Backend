using Business.Abstract;
using Core.Utilities.Authorization;
using Core.Utilities.Context;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class UserTitleManager : IUserTitleService
{
    private readonly IUserTitleDal _userTitleDal;
    private readonly IClientContext _clientContext;
    private readonly ICapabilityPolicy _capabilityPolicy;

    public UserTitleManager(IUserTitleDal userTitleDal, IClientContext clientContext, ICapabilityPolicy capabilityPolicy)
    {
        _userTitleDal = userTitleDal;
        _clientContext = clientContext;
        _capabilityPolicy = capabilityPolicy;
    }

    public IDataResult<List<UserTitleDto>> GetByUser(int userId)
    {
        var titles = _userTitleDal.GetAll(t => t.UserId == userId && t.IsVisible)
            .Select(t => new UserTitleDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Label = t.Label,
                Kind = t.Kind,
                Color = t.Color,
                Icon = t.Icon,
                IsVisible = t.IsVisible,
                AssignedAt = t.AssignedAt
            }).ToList();

        return new SuccessDataResult<List<UserTitleDto>>(titles);
    }

    public IResult Assign(UserTitleAddDto dto)
    {
        _capabilityPolicy.Require("admin.user_title_assign");

        var assignedBy = _clientContext.GetUserId() ?? 0;
        var institutionId = _clientContext.GetInstitutionId() ?? 0;

        var title = new UserTitle
        {
            UserId = dto.UserId,
            Label = dto.Label,
            Kind = dto.Kind,
            Color = dto.Color,
            Icon = dto.Icon,
            InstitutionId = institutionId,
            AssignedByUserId = assignedBy,
            IsVisible = true,
            AssignedAt = DateTime.UtcNow
        };

        _userTitleDal.Add(title);
        return new SuccessResult("Unvan atandı.");
    }

    public IResult Remove(int id)
    {
        _capabilityPolicy.Require("admin.user_title_assign");

        var title = _userTitleDal.Get(t => t.Id == id);
        if (title == null) return new ErrorResult("Unvan bulunamadı.");

        _userTitleDal.Delete(title);
        return new SuccessResult("Unvan kaldırıldı.");
    }
}
