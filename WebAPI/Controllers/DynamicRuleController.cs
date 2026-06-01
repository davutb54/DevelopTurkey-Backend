using System.Text.Json;
using Business.Abstract;
using Business.Models;
using Core.Utilities.Authorization;
using Core.Utilities.Context;
using Entities.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DynamicRuleController : ControllerBase
{
    private readonly IDynamicRuleService _dynamicRuleService;
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly IClientContext _clientContext;

    public DynamicRuleController(
        IDynamicRuleService dynamicRuleService,
        IWorkflowOrchestrator orchestrator,
        IClientContext clientContext)
    {
        _dynamicRuleService = dynamicRuleService;
        _orchestrator = orchestrator;
        _clientContext = clientContext;
    }

    [HttpGet("getall")]
    [RequireCapability("admin.rule_read")]
    public IActionResult GetAll()
    {
        var currentUserId = _clientContext.GetUserId();
        var currentInstitutionId = _clientContext.GetInstitutionId();

        if (currentUserId.HasValue && currentInstitutionId.HasValue)
        {
            var resolver = HttpContext.RequestServices.GetRequiredService<ICapabilityResolver>();
            var isGlobal = resolver.Allows(currentUserId.Value, "admin.rule_read", ctx: null);

            if (!isGlobal)
            {
                var scopedResult = _dynamicRuleService.GetByInstitutionId(currentInstitutionId.Value);
                return scopedResult.Success ? Ok(scopedResult) : BadRequest(scopedResult);
            }
        }

        var result = _dynamicRuleService.GetAll();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("getbyid")]
    [RequireCapability("admin.rule_read")]
    public IActionResult GetById(int id)
    {
        var result = _dynamicRuleService.GetById(id);
        if (!result.Success || result.Data is null) return BadRequest(result);

        var currentUserId = _clientContext.GetUserId();
        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentUserId.HasValue && currentInstitutionId.HasValue)
        {
            var resolver = HttpContext.RequestServices.GetRequiredService<ICapabilityResolver>();
            var isGlobal = resolver.Allows(currentUserId.Value, "admin.rule_read", ctx: null);
            if (!isGlobal && result.Data.InstitutionId != currentInstitutionId.Value)
                return Forbid();
        }

        return Ok(result);
    }

    [HttpGet("getbyinstitution")]
    [RequireCapability("admin.rule_read")]
    public IActionResult GetByInstitution(int institutionId)
    {
        var currentUserId = _clientContext.GetUserId();
        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentUserId.HasValue && currentInstitutionId.HasValue)
        {
            var resolver = HttpContext.RequestServices.GetRequiredService<ICapabilityResolver>();
            var isGlobal = resolver.Allows(currentUserId.Value, "admin.rule_read", ctx: null);
            if (!isGlobal && institutionId != currentInstitutionId.Value)
                return Forbid();
        }

        var result = _dynamicRuleService.GetByInstitutionId(institutionId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("save")]
    [RequireCapability("admin.rule_create")]
    public async Task<IActionResult> Save([FromBody] SaveWorkflowDto dto)
    {
        var result = await _dynamicRuleService.SaveWorkflowAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    [RequireCapability("admin.rule_delete")]
    public IActionResult Delete(int id)
    {
        var currentUserId = _clientContext.GetUserId();
        var currentInstitutionId = _clientContext.GetInstitutionId();

        if (currentUserId.HasValue && currentInstitutionId.HasValue)
        {
            var ruleResult = _dynamicRuleService.GetById(id);
            if (!ruleResult.Success || ruleResult.Data is null) return BadRequest(ruleResult);

            var resolver = HttpContext.RequestServices.GetRequiredService<ICapabilityResolver>();
            var isGlobal = resolver.Allows(currentUserId.Value, "admin.rule_delete", ctx: null);
            if (!isGlobal && ruleResult.Data.InstitutionId != currentInstitutionId.Value)
                return Forbid();
        }

        var result = _dynamicRuleService.Delete(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/test-run")]
    [RequireCapability("admin.rule_test_run")]
    public async Task<IActionResult> TestRun(int id, [FromBody] TestRunRequestDto dto)
    {
        var ruleResult = _dynamicRuleService.GetById(id);
        if (!ruleResult.Success || ruleResult.Data is null)
            return NotFound(ruleResult);

        var rule = ruleResult.Data;

        var currentUserId = _clientContext.GetUserId();
        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentUserId.HasValue && currentInstitutionId.HasValue)
        {
            var resolver = HttpContext.RequestServices.GetRequiredService<ICapabilityResolver>();
            var isGlobal = resolver.Allows(currentUserId.Value, "admin.rule_test_run", ctx: null);
            if (!isGlobal && rule.InstitutionId != currentInstitutionId.Value)
                return Forbid();
        }

        const int SupportedSchemaVersion = 1;
        if (rule.FlowJsonSchemaVersion > SupportedSchemaVersion)
            return BadRequest(new { Success = false, Message = $"Desteklenmeyen FlowJson şema versiyonu: {rule.FlowJsonSchemaVersion}. Desteklenen: {SupportedSchemaVersion}." });

        var userId = _clientContext.GetUserId() ?? 0;
        var triggerEvent = string.IsNullOrWhiteSpace(dto.TriggerEvent) ? rule.TriggerEvent : dto.TriggerEvent;

        var context = new RuleContext
        {
            SystemUserId     = dto.SampleUserId ?? userId,
            InstitutionId    = rule.InstitutionId,
            ProblemId        = dto.SampleProblemId,
            TriggerEventName = triggerEvent,
            ExecutedAt       = DateTime.UtcNow,
        };

        var contextJson = JsonSerializer.Serialize(context);

        var result = await _orchestrator.StartSingleAsync(
            definitionId:      rule.Id,
            versionId:         rule.Version,
            flowJson:          rule.FlowJson,
            triggerEvent:      triggerEvent,
            institutionId:     rule.InstitutionId,
            contextJson:       contextJson,
            eventId:           Guid.NewGuid(),
            triggeredByUserId: userId,
            isDryRun:          true);

        if (!result.Success)
            return BadRequest(new { result.Success, result.Message });

        return Ok(new
        {
            Success      = true,
            result.Message,
            RunId        = result.Data?.Id,
            IsDryRun     = true,
            StartedAt    = result.Data?.StartedAt,
            TriggerEvent = triggerEvent,
            RuleName     = rule.Name,
        });
    }

    [HttpPatch("{id}/toggle")]
    [RequireCapability("admin.rule_activate")]
    public IActionResult ToggleActive(int id)
    {
        var ruleResult = _dynamicRuleService.GetById(id);
        if (!ruleResult.Success || ruleResult.Data == null)
            return BadRequest(ruleResult);

        var rule = ruleResult.Data;

        var currentUserId = _clientContext.GetUserId();
        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentUserId.HasValue && currentInstitutionId.HasValue)
        {
            var resolver = HttpContext.RequestServices.GetRequiredService<ICapabilityResolver>();
            var isGlobal = resolver.Allows(currentUserId.Value, "admin.rule_activate", ctx: null);
            if (!isGlobal && rule.InstitutionId != currentInstitutionId.Value)
                return Forbid();
        }

        rule.IsActive = !rule.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        var result = _dynamicRuleService.Update(rule);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
