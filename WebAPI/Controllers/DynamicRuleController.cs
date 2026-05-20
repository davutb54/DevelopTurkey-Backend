using Business.Abstract;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DynamicRuleController : ControllerBase
{
    private readonly IDynamicRuleService _dynamicRuleService;

    public DynamicRuleController(IDynamicRuleService dynamicRuleService)
    {
        _dynamicRuleService = dynamicRuleService;
    }

    [HttpGet("getall")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAll()
    {
        var result = _dynamicRuleService.GetAll();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("getbyid")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetById(int id)
    {
        var result = _dynamicRuleService.GetById(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("getbyinstitution")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetByInstitution(int institutionId)
    {
        var result = _dynamicRuleService.GetByInstitutionId(institutionId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("save")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] SaveWorkflowDto dto)
    {
        var result = await _dynamicRuleService.SaveWorkflowAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id)
    {
        var result = _dynamicRuleService.Delete(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id}/toggle")]
    [Authorize(Roles = "Admin")]
    public IActionResult ToggleActive(int id)
    {
        var ruleResult = _dynamicRuleService.GetById(id);
        if (!ruleResult.Success || ruleResult.Data == null)
            return BadRequest(ruleResult);

        var rule = ruleResult.Data;
        rule.IsActive = !rule.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        var result = _dynamicRuleService.Update(rule);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
