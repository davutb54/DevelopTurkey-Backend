using Business.Abstract;
using Business.Models;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;

namespace Business.Concrete;

public class TopicFollowManager : ITopicFollowService
{
    private readonly ITopicFollowDal _topicFollowDal;
    private readonly IWorkflowEventBus _eventBus;

    public TopicFollowManager(ITopicFollowDal topicFollowDal, IWorkflowEventBus eventBus)
    {
        _topicFollowDal = topicFollowDal;
        _eventBus = eventBus;
    }

    public IDataResult<bool> ToggleFollow(int topicId, int userId)
    {
        var existing = _topicFollowDal.Get(t => t.TopicId == topicId && t.UserId == userId);
        if (existing != null)
        {
            _topicFollowDal.Delete(existing);

            _ = _eventBus.PublishAsync("topic.unfollowed", new RuleContext
            {
                SystemUserId = userId,
                Metadata = new Dictionary<string, object?>
                {
                    ["TopicId"] = topicId
                }
            });

            return new SuccessDataResult<bool>(false, "Konu takipten çıkıldı");
        }
        else
        {
            _topicFollowDal.Add(new TopicFollow { TopicId = topicId, UserId = userId });

            _ = _eventBus.PublishAsync("topic.followed", new RuleContext
            {
                SystemUserId = userId,
                Metadata = new Dictionary<string, object?>
                {
                    ["TopicId"] = topicId
                }
            });

            return new SuccessDataResult<bool>(true, "Konu takip edildi");
        }
    }

    public bool CheckFollow(int topicId, int userId)
    {
        var existing = _topicFollowDal.Get(t => t.TopicId == topicId && t.UserId == userId);
        return existing != null;
    }

    public List<int> GetFollowerIdsByTopicIds(List<int> topicIds)
    {
        return _topicFollowDal.GetAll(t => topicIds.Contains(t.TopicId))
                              .Select(t => t.UserId)
                              .Distinct()
                              .ToList();
    }
}
