using Entities.Concrete;
using Entities.DTOs;

namespace Business.Abstract;

public interface ISecurityEventService
{
    void LogEvent(
        string eventType,
        string severity,
        string ipAddress,
        string? path,
        string? detail,
        int? userId = null,
        int? institutionId = null);

    (List<SecurityEvent> Items, int TotalCount) GetPaged(SecurityEventFilterDto filter);
}
