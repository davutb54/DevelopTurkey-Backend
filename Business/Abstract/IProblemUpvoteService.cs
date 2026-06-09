using Core.Utilities.Results;
using Entities.DTOs;

namespace Business.Abstract;

public interface IProblemUpvoteService
{
    IDataResult<bool> ToggleUpvote(int problemId, int userId);
    int GetUpvoteCount(int problemId);
    bool CheckUpvote(int problemId, int userId);
    IDataResult<List<ProblemUpvoterDto>> GetUpvoters(int problemId);
}
