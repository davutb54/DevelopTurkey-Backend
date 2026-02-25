using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface ILogService
{
    IDataResult<List<Log>> GetList(LogFilterDto filter);
    IResult Add(Log log);
}