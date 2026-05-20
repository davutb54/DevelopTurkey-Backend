using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;
using System.Threading.Tasks;

namespace Business.Abstract;

public interface IDynamicRuleService
{
    IDataResult<DynamicRule> GetById(int id);
    IDataResult<List<DynamicRule>> GetAll();
    IDataResult<List<DynamicRule>> GetByInstitutionId(int institutionId);
    Task<IDataResult<DynamicRule>> SaveWorkflowAsync(SaveWorkflowDto dto);
    Task<IDataResult<List<DynamicRule>>> GetActiveByInstitutionAsync(int institutionId);
    Task<IDataResult<List<DynamicRule>>> GetByTriggerEventAsync(string triggerEventName, int institutionId);
    IResult Add(DynamicRule dynamicRule);
    IResult Update(DynamicRule dynamicRule);
    IResult Delete(int id);
}
