using Business.Abstract;
using Entities.DTOs.Capability;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/capability-templates")]
[ApiController]
public class CapabilityTemplatesController : ControllerBase
{
    private readonly ICapabilityTemplateService _templateService;

    public CapabilityTemplatesController(ICapabilityTemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpGet]
    [RequireCapability("admin.capability_catalog_read")]
    public IActionResult GetAll()
    {
        var result = _templateService.GetAll();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    [RequireCapability("admin.capability_catalog_read")]
    public IActionResult GetById(int id)
    {
        var result = _templateService.GetById(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id:int}/versions")]
    [RequireCapability("admin.capability_catalog_read")]
    public IActionResult GetVersions(int id)
    {
        var result = _templateService.GetVersions(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    [RequireCapability("admin.capability_template_create")]
    public IActionResult Create([FromBody] CreateTemplateDto dto)
    {
        var result = _templateService.Create(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/publish")]
    [RequireCapability("admin.capability_template_publish")]
    public IActionResult Publish(int id, [FromBody] PublishTemplateVersionDto dto)
    {
        var result = _templateService.PublishVersion(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/apply")]
    [RequireCapability("admin.capability_template_apply")]
    public async Task<IActionResult> Apply(int id, [FromBody] ApplyTemplateDto dto)
    {
        var result = await _templateService.ApplyAsync(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/revoke")]
    [RequireCapability("admin.capability_revoke")]
    public async Task<IActionResult> RevokeApplied(int id, [FromBody] RevokeAppliedTemplateDto dto)
    {
        var result = await _templateService.RevokeAppliedAsync(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:int}/deactivate")]
    [RequireCapability("admin.capability_catalog_write")]
    public IActionResult Deactivate(int id)
    {
        var result = _templateService.Deactivate(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
