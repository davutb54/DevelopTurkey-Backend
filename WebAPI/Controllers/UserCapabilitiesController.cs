using Business.Abstract;
using Entities.DTOs.Capability;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/users/{userId:int}/capabilities")]
[ApiController]
public class UserCapabilitiesController : ControllerBase
{
    private readonly IUserCapabilityService _userCapabilityService;

    public UserCapabilitiesController(IUserCapabilityService userCapabilityService)
    {
        _userCapabilityService = userCapabilityService;
    }

    [HttpGet]
    [RequireCapability("admin.capability_catalog_read")]
    public IActionResult GetByUser(
        int userId,
        [FromQuery] int? institutionId = null,
        [FromQuery] bool includeExpired = false)
    {
        var result = _userCapabilityService.GetByUser(userId, institutionId, includeExpired);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("grant")]
    [RequireCapability("admin.capability_grant")]
    public async Task<IActionResult> Grant(int userId, [FromBody] GrantCapabilityDto dto)
    {
        var result = await _userCapabilityService.GrantAsync(userId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("revoke")]
    [RequireCapability("admin.capability_revoke")]
    public async Task<IActionResult> Revoke(int userId, [FromBody] RevokeCapabilityDto dto)
    {
        var result = await _userCapabilityService.RevokeAsync(userId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
