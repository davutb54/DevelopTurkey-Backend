using Business.Abstract;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;

namespace Business.Concrete;

public class TopicManager : ITopicService
{
    private readonly ITopicDal _topicDal;
	private readonly ILogDal _logDal;
	public TopicManager(ITopicDal topicDal, ILogDal logDal)
	{
		_topicDal = topicDal;
		_logDal = logDal;
    }

    public IDataResult<Topic?> GetById(int id)
	{
		return new SuccessDataResult<Topic?>(_topicDal.Get(topic => topic.Id == id));
	}

	public IDataResult<List<Topic>> GetAll()
	{
		return new SuccessDataResult<List<Topic>>(_topicDal.GetAll());
	}

	public IResult Add(Topic topic)
	{
		_topicDal.Add(topic);

		_logDal.Add(new Log
        {
			CreationDate = DateTime.Now,
			Message = "New topic added: " + topic.Name,
			Type = "Topic,Add,Info"
        });
		return new SuccessResult(Messages.TopicAdded);
	}

	public IResult Update(Topic topic)
	{
		_topicDal.Update(topic);
		_logDal.Add(new Log
		{
			CreationDate = DateTime.Now,
			Message = "Topic updated: " + topic.Name,
			Type = "Topic,Update,Info"
		});
        return new SuccessResult(Messages.TopicUpdated);
	}

	public IResult Delete(Topic topic)
	{
		_topicDal.Delete(topic);
		_logDal.Add(new Log
		{
			CreationDate = DateTime.Now,
			Message = "Topic deleted: " + topic.Name,
			Type = "Topic,Delete,Info"
		});
        return new SuccessResult(Messages.TopicDeleted);
	}
}