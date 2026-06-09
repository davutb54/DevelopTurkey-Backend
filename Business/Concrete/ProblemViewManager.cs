using Business.Abstract;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class ProblemViewManager : IProblemViewService
{
    private readonly IProblemViewDal _problemViewDal;
    private readonly IUserDal _userDal;

    public ProblemViewManager(IProblemViewDal problemViewDal, IUserDal userDal)
    {
        _problemViewDal = problemViewDal;
        _userDal = userDal;
    }

    public IResult RecordView(int problemId, int? userId, int institutionId)
    {
        if (userId.HasValue)
        {
            var today = DateTime.Today;
            var exists = _problemViewDal.Get(
                v => v.ProblemId == problemId && v.UserId == userId && v.ViewedAt >= today);
            if (exists != null) return new SuccessResult();
        }

        _problemViewDal.Add(new ProblemView
        {
            ProblemId = problemId,
            UserId = userId,
            InstitutionId = institutionId,
            ViewedAt = DateTime.Now
        });

        return new SuccessResult();
    }

    public IDataResult<List<ProblemViewerDto>> GetViewers(int problemId)
    {
        var views = _problemViewDal
            .GetAll(v => v.ProblemId == problemId && v.UserId.HasValue)
            .ToList();

        var userIds = views
            .Select(v => v.UserId!.Value)
            .Distinct()
            .ToList();

        var users = _userDal
            .GetAll(u => userIds.Contains(u.Id))
            .ToDictionary(u => u.Id);

        // Her kullanıcı için yalnızca en son görüntüleme kaydını al
        var result = views
            .GroupBy(v => v.UserId!.Value)
            .Select(g =>
            {
                var latest = g.OrderByDescending(v => v.ViewedAt).First();
                users.TryGetValue(g.Key, out var user);
                return new ProblemViewerDto
                {
                    UserId = latest.UserId,
                    Username = user?.UserName,
                    ProfileImageUrl = user?.ProfileImageUrl,
                    ViewedAt = latest.ViewedAt
                };
            })
            .OrderByDescending(v => v.ViewedAt)
            .ToList();

        return new SuccessDataResult<List<ProblemViewerDto>>(result);
    }
}
