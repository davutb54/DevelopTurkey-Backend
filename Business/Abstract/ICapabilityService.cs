using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs.Capability;

namespace Business.Abstract;

public interface ICapabilityService
{
    IDataResult<List<CapabilityDto>> GetAll();
    IDataResult<CapabilityDto> GetByCode(string code);
    IDataResult<List<CapabilityDto>> GetByCategory(string category);
}
