using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;

namespace WebAPI.Controllers;

/// <summary>
/// Workflow builder UI'ın kullandığı statik referans kataloğu:
/// Trigger, Field, Action listeleri. Seeder ile DB'ye yazılıp nadiren değişir;
/// İ6 fix: bu yüzden 1 saatlik in-memory cache uygulanır — admin paneli her
/// açılışta 3 ayrı SQL sorgusu yapmaz.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class WorkflowReferenceController : ControllerBase
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
    private const string CacheKeyTriggers = "WorkflowReference.Triggers";
    private const string CacheKeyFields   = "WorkflowReference.Fields";
    private const string CacheKeyActions  = "WorkflowReference.Actions";

    private readonly IWorkflowTriggerService _workflowTriggerService;
    private readonly IWorkflowFieldService _workflowFieldService;
    private readonly IWorkflowActionService _workflowActionService;
    private readonly IMemoryCache _cache;

    public WorkflowReferenceController(
        IWorkflowTriggerService workflowTriggerService,
        IWorkflowFieldService workflowFieldService,
        IWorkflowActionService workflowActionService,
        IMemoryCache cache)
    {
        _workflowTriggerService = workflowTriggerService;
        _workflowFieldService = workflowFieldService;
        _workflowActionService = workflowActionService;
        _cache = cache;
    }

    [HttpGet("triggers")]
    public async Task<IActionResult> GetTriggers()
    {
        if (_cache.TryGetValue(CacheKeyTriggers, out IActionResult? cached) && cached != null)
            return cached;
        var result = await _workflowTriggerService.GetAllActiveAsync();
        IActionResult response = result.Success ? Ok(result) : BadRequest(result);
        if (result.Success) _cache.Set(CacheKeyTriggers, response, CacheTtl);
        return response;
    }

    [HttpGet("fields")]
    public async Task<IActionResult> GetFields()
    {
        if (_cache.TryGetValue(CacheKeyFields, out IActionResult? cached) && cached != null)
            return cached;
        var result = await _workflowFieldService.GetAllActiveAsync();
        IActionResult response = result.Success ? Ok(result) : BadRequest(result);
        if (result.Success) _cache.Set(CacheKeyFields, response, CacheTtl);
        return response;
    }

    [HttpGet("actions")]
    public async Task<IActionResult> GetActions()
    {
        if (_cache.TryGetValue(CacheKeyActions, out IActionResult? cached) && cached != null)
            return cached;
        var result = await _workflowActionService.GetAllActiveAsync();
        IActionResult response = result.Success ? Ok(result) : BadRequest(result);
        if (result.Success) _cache.Set(CacheKeyActions, response, CacheTtl);
        return response;
    }
}
