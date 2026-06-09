using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface ISolutionVoteService
{
    IResult Vote(int solutionId, bool isUpvote);
    IDataResult<int> GetSolutionVoteCount(int solutionId);
    IDataResult<List<SolutionVoterDto>> GetVoters(int solutionId);
}