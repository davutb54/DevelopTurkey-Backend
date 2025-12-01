using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface ISolutionVoteService
{
    IResult Vote(int userId, int solutionId, bool isUpvote);
    IDataResult<int> GetSolutionVoteCount(int solutionId);
}