using Business.Abstract;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;

namespace Business.Concrete;

public class TopicFollowManager : ITopicFollowService
{
    private readonly ITopicFollowDal _topicFollowDal;

    public TopicFollowManager(ITopicFollowDal topicFollowDal)
    {
        _topicFollowDal = topicFollowDal;
    }

    public IDataResult<bool> ToggleFollow(int topicId, int userId)
    {
        var existing = _topicFollowDal.Get(t => t.TopicId == topicId && t.UserId == userId);
        if (existing != null)
        {
            _topicFollowDal.Delete(existing);
            return new SuccessDataResult<bool>(false, "Konu takipten çıkıldı");
        }
        else
        {
            _topicFollowDal.Add(new TopicFollow { TopicId = topicId, UserId = userId });
            return new SuccessDataResult<bool>(true, "Konu takip edildi");
        }
    }
}
