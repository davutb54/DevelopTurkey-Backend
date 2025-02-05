using Business.Abstract;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;

namespace Business.Concrete;

public class TopicManager : ITopicService
{
	private readonly ITopicDal _topicDal = new EfTopicDal();

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
		return new SuccessResult(Messages.TopicAdded);
	}

	public IResult Update(Topic topic)
	{
		_topicDal.Update(topic);
		return new SuccessResult(Messages.TopicUpdated);
	}

	public IResult Delete(Topic topic)
	{
		_topicDal.Delete(topic);
		return new SuccessResult(Messages.TopicDeleted);
	}
}