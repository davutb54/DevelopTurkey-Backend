using Core.Utilities.Results;
using Entities.DTOs.Capability;
using Entities.DTOs.Metrics;

namespace Business.Abstract;

public interface IMetricsService
{
    IDataResult<OverviewMetricsDto> GetOverview();
    IDataResult<CapabilityMetricsDto> GetCapabilityMetrics(DateTime? from, DateTime? to);
    IDataResult<WorkflowMetricsDto> GetWorkflowMetrics(DateTime? from, DateTime? to);
    IDataResult<UserMetricsDto> GetUserMetrics(DateTime? from, DateTime? to);
    IDataResult<PagedResult<CapabilityAuditLogDto>> GetAuditLog(CapabilityAuditFilterDto filter);
}
