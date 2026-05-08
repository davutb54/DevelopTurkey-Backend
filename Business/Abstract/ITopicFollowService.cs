using Core.Utilities.Results;

namespace Business.Abstract;

public interface ITopicFollowService
{
    IDataResult<bool> ToggleFollow(int topicId, int userId);
}
