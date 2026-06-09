using Core.DataAccess;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Abstract;

public interface ISecurityEventDal : IEntityRepository<SecurityEvent>
{
    (List<SecurityEvent> Items, int TotalCount) GetPaged(SecurityEventFilterDto filter);
}
