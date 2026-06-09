using Business.Abstract;
using Entities.DTOs;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SecurityController : Controller
{
    private readonly ISecurityEventService _securityEventService;

    public SecurityController(ISecurityEventService securityEventService)
    {
        _securityEventService = securityEventService;
    }

    [HttpGet("events")]
    [RequireCapability("admin.security_monitor")]
    public IActionResult GetEvents([FromQuery] SecurityEventFilterDto filter)
    {
        if (filter.PageSize > 200) filter.PageSize = 200;
        var (items, total) = _securityEventService.GetPaged(filter);
        return Ok(new { success = true, data = items, totalCount = total });
    }
}
