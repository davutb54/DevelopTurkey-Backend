using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface IInstitutionService
{
    IDataResult<Institution> GetById(int id);
    IDataResult<List<Institution>> GetAll();
    IDataResult<Institution> GetByDomain(string domain);
    IDataResult<InstitutionPublicInfoDto> GetPublicInfo(string domain);
    IDataResult<InstitutionPublicInfoDto> GetPublicInfoBySubdomain(string slug);
    IResult Add(Institution institution);
    IResult Update(Institution institution);
    IResult Delete(int id);
}
