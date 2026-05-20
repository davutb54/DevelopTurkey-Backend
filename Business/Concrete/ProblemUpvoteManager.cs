using Business.Abstract;
using Business.Models;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class ProblemUpvoteManager : IProblemUpvoteService
{
    private readonly IProblemUpvoteDal _problemUpvoteDal;
    private readonly IProblemDal _problemDal;
    private readonly IInstitutionFeatureService _featureService;
    private readonly IWorkflowEventBus _eventBus;

    public ProblemUpvoteManager(IProblemUpvoteDal problemUpvoteDal, IProblemDal problemDal, IInstitutionFeatureService featureService, IWorkflowEventBus eventBus)
    {
        _problemUpvoteDal = problemUpvoteDal;
        _problemDal = problemDal;
        _featureService = featureService;
        _eventBus = eventBus;
    }

    public bool CheckUpvote(int problemId, int userId)
    {
        var existing = _problemUpvoteDal.Get(u => u.ProblemId == problemId && u.UserId == userId);
        return existing != null;
    }

    public int GetUpvoteCount(int problemId)
    {
        return _problemUpvoteDal.GetAll(u => u.ProblemId == problemId).Count;
    }

    public IDataResult<bool> ToggleUpvote(int problemId, int userId)
    {
        var problem = _problemDal.Get(p => p.Id == problemId);
        if (problem == null) return new ErrorDataResult<bool>(false, "Sorun bulunamadı.");

        if (!_featureService.IsFeatureEnabled(problem.InstitutionId, "Social.EnableUpvote"))
        {
            return new ErrorDataResult<bool>(false, "Bu kurumda oylama devre dışıdır.");
        }

        var existing = _problemUpvoteDal.Get(u => u.ProblemId == problemId && u.UserId == userId);
        
        if (existing != null)
        {
            _problemUpvoteDal.Delete(existing);

            _ = _eventBus.PublishAsync("problem.unvoted", new RuleContext
            {
                SystemUserId = userId,
                Metadata = new Dictionary<string, object?>
                {
                    ["ProblemId"] = problemId
                }
            });

            return new SuccessDataResult<bool>(false, "Destek geri çekildi");
        }
        else
        {
            var upvote = new ProblemUpvote
            {
                ProblemId = problemId,
                UserId = userId,
                CreatedAt = DateTime.Now
            };
            _problemUpvoteDal.Add(upvote);

            _ = _eventBus.PublishAsync("problem.upvoted", new RuleContext
            {
                SystemUserId = userId,
                Metadata = new Dictionary<string, object?>
                {
                    ["ProblemId"] = problemId
                }
            });

            return new SuccessDataResult<bool>(true, "Destek eklendi");
        }
    }
}
