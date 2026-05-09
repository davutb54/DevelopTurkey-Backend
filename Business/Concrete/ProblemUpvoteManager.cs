using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;

namespace Business.Concrete;

public class ProblemUpvoteManager : IProblemUpvoteService
{
    private readonly IProblemUpvoteDal _problemUpvoteDal;

    public ProblemUpvoteManager(IProblemUpvoteDal problemUpvoteDal)
    {
        _problemUpvoteDal = problemUpvoteDal;
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
        var existing = _problemUpvoteDal.Get(u => u.ProblemId == problemId && u.UserId == userId);
        
        if (existing != null)
        {
            _problemUpvoteDal.Delete(existing);
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
            return new SuccessDataResult<bool>(true, "Destek eklendi");
        }
    }
}
