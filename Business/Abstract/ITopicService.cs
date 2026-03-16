using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface ITopicService
{
    IResult Add(Topic topic);
    IResult Update(Topic topic);
    IResult Delete(Topic topic);
    IDataResult<List<Topic>> GetAll();
    IDataResult<List<Topic>> GetAllForAdmin();
    IDataResult<Topic?> GetById(int id);
}