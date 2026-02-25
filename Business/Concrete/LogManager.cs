using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class LogManager : ILogService
{
    private readonly ILogDal _logDal;

    public LogManager(ILogDal logDal)
    {
        _logDal = logDal;
    }

    public IDataResult<List<Log>> GetList(LogFilterDto filter)
    {
        return new SuccessDataResult<List<Log>>(_logDal.GetListByFilter(filter));
    }

    public IResult Add(Log log)
    {
        _logDal.Add(log);
        return new SuccessResult("Log başarıyla eklendi.");
    }
}