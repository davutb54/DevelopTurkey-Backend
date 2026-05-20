using Business.Abstract;
using Core.Utilities.Context;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Business.Concrete;

public class LogManager : ILogService
{
    private readonly ILogDal _logDal;
    private readonly IClientContext _clientContext;

    public LogManager(ILogDal logDal, IClientContext clientContext)
    {
        _logDal = logDal;
        _clientContext = clientContext;
    }

    private void CreateLog(string category, string action, string level, string message, string? details, int? institutionId = null)
    {
        _logDal.Add(new Log
        {
            UserId = _clientContext.GetUserId(),
            UserName = _clientContext.GetUserName(),
            IpAddress = _clientContext.GetIpAddress(),
            Port = _clientContext.GetPort(),
            InstitutionId = institutionId,
            Category = category,
            Action = action,
            Level = level,
            Message = message,
            Details = details,
            CreationDate = DateTime.Now
        });
    }
    
    public void LogInfo(string category, string action, string message, string? details = null, int? institutionId = null)
        => CreateLog(category, action, "Info", message, details, institutionId);

    public void LogWarning(string category, string action, string message, string? details = null, int? institutionId = null)
        => CreateLog(category, action, "Warning", message, details, institutionId);

    public void LogError(string category, string action, string message, string? details = null, int? institutionId = null)
        => CreateLog(category, action, "Error", message, details, institutionId);

    public void LogCritical(string category, string action, string message, string? details = null, int? institutionId = null)
        => CreateLog(category, action, "Critical", message, details, institutionId);

    public IDataResult<List<Log>> GetListByFilter(LogFilterDto filter)
    {
        return new SuccessDataResult<List<Log>>(_logDal.GetListByFilter(filter));
    }
}