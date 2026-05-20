using Business.Abstract;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class WorkflowLogController : ControllerBase
{
    private readonly IWorkflowLogService _workflowLogService;

    public WorkflowLogController(IWorkflowLogService workflowLogService)
    {
        _workflowLogService = workflowLogService;
    }

    [HttpGet("list")]
    public IActionResult GetList([FromQuery] WorkflowLogFilterDto filter)
    {
        var result = _workflowLogService.GetListByFilter(filter);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("count")]
    public IActionResult GetCount([FromQuery] WorkflowLogFilterDto filter)
    {
        var result = _workflowLogService.CountByFilter(filter);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var result = _workflowLogService.GetById(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
