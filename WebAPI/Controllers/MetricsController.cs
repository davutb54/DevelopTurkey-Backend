using Business.Abstract;
using Entities.DTOs.Metrics;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly IAdminService _adminService;

    public MetricsController(IMetricsService metricsService, IAdminService adminService)
    {
        _metricsService = metricsService;
        _adminService = adminService;
    }

    [HttpGet("overview")]
    [RequireCapability("admin.dashboard_view")]
    public IActionResult GetOverview()
    {
        var result = _metricsService.GetOverview();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("capabilities")]
    [RequireCapability("admin.metrics_capability_view")]
    public IActionResult GetCapabilities([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = _metricsService.GetCapabilityMetrics(from, to);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("workflow")]
    [RequireCapability("admin.metrics_workflow_view")]
    public IActionResult GetWorkflow([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = _metricsService.GetWorkflowMetrics(from, to);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("users")]
    [RequireCapability("admin.metrics_user_view")]
    public IActionResult GetUsers([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = _metricsService.GetUserMetrics(from, to);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("system-health")]
    [RequireCapability("admin.metrics_system_health_view")]
    public IActionResult GetSystemHealth()
    {
        var result = _adminService.GetSystemHealthStatus();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("audit-log")]
    [RequireCapability("admin.audit_read")]
    public IActionResult GetAuditLog([FromQuery] CapabilityAuditFilterDto filter)
    {
        var result = _metricsService.GetAuditLog(filter);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
