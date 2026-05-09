using Core.Utilities.Results;

namespace Business.Abstract;

public interface ITopicFollowService
{
    IDataResult<bool> ToggleFollow(int topicId, int userId);
    List<int> GetFollowerIdsByTopicIds(List<int> topicIds);
    bool CheckFollow(int topicId, int userId);
}
