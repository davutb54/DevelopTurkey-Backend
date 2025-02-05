using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface ITopicService
{
	IDataResult<Topic?> GetById(int id);
	IDataResult<List<Topic>> GetAll();
	IResult Add(Topic topic);
	IResult Update(Topic topic);
	IResult Delete(Topic topic);
}