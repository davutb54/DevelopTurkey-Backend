using Business.Abstract;
using Business.Models;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using System.Collections.Generic;
using System.Linq;

namespace Business.Concrete;

public class ProblemFollowManager : IProblemFollowService
{
    private readonly IProblemFollowDal _problemFollowDal;
    private readonly IWorkflowEventBus _eventBus;

    public ProblemFollowManager(IProblemFollowDal problemFollowDal, IWorkflowEventBus eventBus)
    {
        _problemFollowDal = problemFollowDal;
        _eventBus = eventBus;
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

            _ = _eventBus.PublishAsync("problem.unfollowed", new RuleContext
            {
                SystemUserId = userId,
                Metadata = new Dictionary<string, object?>
                {
                    ["ProblemId"] = problemId
                }
            });

            return new SuccessDataResult<bool>(false, "Takipten çıkıldı");
        }
        else
        {
            _problemFollowDal.Add(new ProblemFollow { ProblemId = problemId, UserId = userId });

            _ = _eventBus.PublishAsync("problem.followed", new RuleContext
            {
                SystemUserId = userId,
                Metadata = new Dictionary<string, object?>
                {
                    ["ProblemId"] = problemId
                }
            });

            return new SuccessDataResult<bool>(true, "Takip edildi");
        }
    }
}
