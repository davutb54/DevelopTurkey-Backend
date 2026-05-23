using Business.Abstract;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InstitutionFeaturesController : ControllerBase
{
    private readonly IInstitutionFeatureService _institutionFeatureService;

    public InstitutionFeaturesController(IInstitutionFeatureService institutionFeatureService)
    {
        _institutionFeatureService = institutionFeatureService;
    }

    /// <summary>
    /// Bir kurumun tüm feature değerlerini getirir (key -> value map)
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
        _institutionFeatureService.InvalidateCache(institutionId);
        return Ok(new { success = true, message = "Cache temizlendi." });
    }
}

public class SetFeatureRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
