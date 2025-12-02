using Core.DataAccess;
using Entities.Concrete; 
using Entities.DTOs;

namespace DataAccess.Abstract;

public interface ILogDal : IEntityRepository<Log>
{
    List<Log> GetListByFilter(LogFilterDto filter);
}