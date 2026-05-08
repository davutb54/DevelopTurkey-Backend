using Core.Utilities.Results;
using System.Collections.Generic;

namespace Business.Abstract;

public interface IProblemFollowService
{
    IDataResult<bool> ToggleFollow(int problemId, int userId);
    List<int> GetFollowerIds(int problemId);
    bool CheckFollow(int problemId, int userId);
}
