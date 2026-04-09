using Core.Utilities.Results;
using Entities.DTOs;

namespace Business.Abstract;

public interface IAdminService
{
    IDataResult<AdminDashboardDto> GetDashboardStats();
    IDataResult<DashboardAnalyticsDto> GetDashboardAnalytics();
    IDataResult<SystemHealthDto> GetSystemHealthStatus();
}
