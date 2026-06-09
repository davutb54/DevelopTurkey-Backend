using System.Linq.Expressions;
using Core.DataAccess;
using Entities.Concrete;
using Entities.DTOs;

namespace DataAccess.Abstract;

public interface IOfficialResponseDal : IEntityRepository<OfficialResponse>
{
    List<OfficialResponseDto> GetDetails(Expression<Func<OfficialResponseDto, bool>>? filter = null);
}
