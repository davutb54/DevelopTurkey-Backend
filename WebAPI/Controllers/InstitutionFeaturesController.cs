using Business.Abstract;
using Core.Utilities.Authorization;
using Core.Utilities.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InstitutionFeaturesController : ControllerBase
{
    private readonly IInstitutionFeatureService _institutionFeatureService;
    private readonly IClientContext _clientContext;

    public InstitutionFeaturesController(IInstitutionFeatureService institutionFeatureService, IClientContext clientContext)
    {
        _institutionFeatureService = institutionFeatureService;
        _clientContext = clientContext;
    }

    /// <summary>
    /// Bir kurumun tüm feature değerlerini getirir (key -> value map).
    /// Feature konfigürasyonu hassas veri değildir; herkes okuyabilir.
    /// </summary>
    [HttpGet("getall/{institutionId}")]
    public IActionResult GetAll(int institutionId)
    {
        var result = _institutionFeatureService.GetAllForInstitution(institutionId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Bir kurumun tek bir feature değerini ayarlar
    /// </summary>
    [HttpPost("set/{institutionId}")]
    [RequireCapability("admin.institution_feature_write")]
    public IActionResult SetFeature(int institutionId, [FromBody] SetFeatureRequest request)
    {
        var currentUserId = _clientContext.GetUserId();
        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentUserId.HasValue && currentInstitutionId.HasValue)
        {
            var resolver = HttpContext.RequestServices.GetRequiredService<ICapabilityResolver>();
            var isGlobal = resolver.Allows(currentUserId.Value, "admin.institution_feature_write", ctx: null);
            if (!isGlobal && institutionId != currentInstitutionId.Value)
                return Forbid();
        }

        var result = _institutionFeatureService.SetFeatureValue(institutionId, request.Key, request.Value);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Bir kurumun birden fazla feature değerini toplu ayarlar
    /// </summary>
    [HttpPost("setbulk/{institutionId}")]
    [RequireCapability("admin.institution_feature_write")]
    public IActionResult SetFeaturesBulk(int institutionId, [FromBody] Dictionary<string, string> values)
    {
        var currentUserId = _clientContext.GetUserId();
        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentUserId.HasValue && currentInstitutionId.HasValue)
        {
            var resolver = HttpContext.RequestServices.GetRequiredService<ICapabilityResolver>();
            var isGlobal = resolver.Allows(currentUserId.Value, "admin.institution_feature_write", ctx: null);
            if (!isGlobal && institutionId != currentInstitutionId.Value)
                return Forbid();
        }

        var result = _institutionFeatureService.SetFeatureValues(institutionId, values);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Bir kurumun feature cache'ini temizler
    /// </summary>
    [HttpPost("invalidatecache/{institutionId}")]
    [RequireCapability("admin.institution_feature_write")]
    public IActionResult InvalidateCache(int institutionId)
    {
        var currentUserId = _clientContext.GetUserId();
        var currentInstitutionId = _clientContext.GetInstitutionId();
        if (currentUserId.HasValue && currentInstitutionId.HasValue)
        {
            var resolver = HttpContext.RequestServices.GetRequiredService<ICapabilityResolver>();
            var isGlobal = resolver.Allows(currentUserId.Value, "admin.institution_feature_write", ctx: null);
            if (!isGlobal && institutionId != currentInstitutionId.Value)
                return Forbid();
        }

        _institutionFeatureService.InvalidateCache(institutionId);
        return Ok(new { success = true, message = "Cache temizlendi." });
    }
}

public class SetFeatureRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
