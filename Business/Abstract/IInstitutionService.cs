using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract;

public interface IInstitutionService
{
    IDataResult<Institution> GetById(int id);
    IDataResult<List<Institution>> GetAll();
    IDataResult<Institution> GetByDomain(string domain);
    IResult Add(Institution institution);
    IResult Update(Institution institution);
    IResult Delete(int id);
}
