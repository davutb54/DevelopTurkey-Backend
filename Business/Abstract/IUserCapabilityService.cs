using Core.Utilities.Results;
using Entities.DTOs.Capability;

namespace Business.Abstract;

public interface IUserCapabilityService
{
    IDataResult<List<UserCapabilityDto>> GetByUser(int userId, int? institutionId = null, bool includeExpired = false);
    Task<IResult> GrantAsync(int userId, GrantCapabilityDto dto);
    Task<IResult> RevokeAsync(int userId, RevokeCapabilityDto dto);
    Task<IResult> RevokeBulkAsync(int userId, RevokeBulkDto dto);
}
