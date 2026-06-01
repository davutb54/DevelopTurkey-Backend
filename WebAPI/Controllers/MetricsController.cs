using Business.Abstract;
using Core.Utilities.Context;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using Entities.DTOs.Capability;
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
    private readonly IWorkflowRunService _runService;
    private readonly IWorkflowLogService _workflowLogService;
    private readonly IWorkflowDeadLetterDal _deadLetterDal;
    private readonly IClientContext _clientContext;

    public MetricsController(
        IMetricsService metricsService,
        IAdminService adminService,
        IWorkflowRunService runService,
        IWorkflowLogService workflowLogService,
        IWorkflowDeadLetterDal deadLetterDal,
        IClientContext clientContext)
    {
        _metricsService = metricsService;
        _adminService = adminService;
        _runService = runService;
        _workflowLogService = workflowLogService;
        _deadLetterDal = deadLetterDal;
        _clientContext = clientContext;
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

    [HttpGet("workflow/runs")]
    [RequireCapability("admin.metrics_workflow_view")]
    public IActionResult GetWorkflowRuns([FromQuery] int definitionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = _runService.GetSummaryByDefinition(definitionId, page, pageSize);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("workflow/runs/{runId}")]
    [RequireCapability("admin.metrics_workflow_view")]
    public IActionResult GetWorkflowRunDetail(Guid runId)
    {
        var result = _runService.GetDetail(runId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // ── 2B: Gerçek event çalışma logları (WorkflowLog) ──────────────────────────
    [HttpGet("workflow/event-logs")]
    [RequireCapability("admin.metrics_workflow_view")]
    public IActionResult GetWorkflowEventLogs([FromQuery] WorkflowLogFilterDto filter)
    {
        var items  = _workflowLogService.GetListByFilter(filter);
        var count  = _workflowLogService.CountByFilter(filter);
        return Ok(new { success = true, data = new { items = items.Data, total = count.Data } });
    }

    // ── 2A: Dead-Letter listesi ──────────────────────────────────────────────────
    [HttpGet("workflow/dead-letters")]
    [RequireCapability("admin.metrics_workflow_view")]
    public IActionResult GetDeadLetters([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var all   = _deadLetterDal.GetPending(page, pageSize);
        var total = _deadLetterDal.GetAll(x => !x.IsRequeued).Count;
        return Ok(new { success = true, data = new { items = all, total } });
    }

    [HttpPost("workflow/dead-letters/{id}/requeue")]
    [RequireCapability("admin.metrics_workflow_view")]
    public IActionResult RequeueDeadLetter(Guid id)
    {
        var entry = _deadLetterDal.Get(x => x.Id == id);
        if (entry is null) return NotFound(new { success = false, message = "Kayıt bulunamadı." });

        entry.IsRequeued       = true;
        entry.RequeuedAt       = DateTime.Now;
        entry.RequeuedByUserId = _clientContext.GetUserId();
        _deadLetterDal.Update(entry);

        return Ok(new { success = true, message = "Yeniden kuyruğa alındı." });
    }
}
