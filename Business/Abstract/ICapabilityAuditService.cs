using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs.Capability;

namespace Business.Abstract;

public interface ICapabilityAuditService
{
    IResult Add(CapabilityAuditLog log);
    IDataResult<List<CapabilityAuditLog>> GetByFilter(CapabilityAuditFilterDto filter);
}
