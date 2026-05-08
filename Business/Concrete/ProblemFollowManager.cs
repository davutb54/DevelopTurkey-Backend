using Business.Abstract;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using System.Collections.Generic;
using System.Linq;

namespace Business.Concrete;

public class ProblemFollowManager : IProblemFollowService
{
    private readonly IProblemFollowDal _problemFollowDal;

    public ProblemFollowManager(IProblemFollowDal problemFollowDal)
    {
        _problemFollowDal = problemFollowDal;
    }

    public List<int> GetFollowerIds(int problemId)
    {
        return _problemFollowDal.GetAll(p => p.ProblemId == problemId).Select(p => p.UserId).ToList();
    }

    public bool CheckFollow(int problemId, int userId)
    {
        return _problemFollowDal.Get(p => p.ProblemId == problemId && p.UserId == userId) != null;
    }

    public IDataResult<bool> ToggleFollow(int problemId, int userId)
    {
        var existing = _problemFollowDal.Get(p => p.ProblemId == problemId && p.UserId == userId);
        if (existing != null)
        {
            _problemFollowDal.Delete(existing);
            return new SuccessDataResult<bool>(false, "Takipten çıkıldı");
        }
        else
        {
            _problemFollowDal.Add(new ProblemFollow { ProblemId = problemId, UserId = userId });
            return new SuccessDataResult<bool>(true, "Takip edildi");
        }
    }
}
