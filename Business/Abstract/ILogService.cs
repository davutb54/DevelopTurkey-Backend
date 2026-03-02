using Core.Utilities.Results;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface ILogService
{
    IDataResult<List<Log>> GetListByFilter(LogFilterDto filter);

    void LogInfo(string category, string action, string message, string? details = null);
    void LogWarning(string category, string action, string message, string? details = null);
    void LogError(string category, string action, string message, string? details = null);
    void LogCritical(string category, string action, string message, string? details = null);
}