using Business.Abstract;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CapabilitiesController : ControllerBase
{
    private readonly ICapabilityService _capabilityService;

    public CapabilitiesController(ICapabilityService capabilityService)
    {
        _capabilityService = capabilityService;
    }

    [HttpGet]
    [RequireCapability("admin.capability_catalog_read")]
    public IActionResult GetAll()
    {
        var result = _capabilityService.GetAll();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{code}")]
    [RequireCapability("admin.capability_catalog_read")]
    public IActionResult GetByCode(string code)
    {
        var result = _capabilityService.GetByCode(code);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("category/{category}")]
    [RequireCapability("admin.capability_catalog_read")]
    public IActionResult GetByCategory(string category)
    {
        var result = _capabilityService.GetByCategory(category);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
