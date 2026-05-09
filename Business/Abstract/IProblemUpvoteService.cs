using Core.Utilities.Results;

namespace Business.Abstract;

public interface IProblemUpvoteService
{
    IDataResult<bool> ToggleUpvote(int problemId, int userId);
    int GetUpvoteCount(int problemId);
    bool CheckUpvote(int problemId, int userId);
}
