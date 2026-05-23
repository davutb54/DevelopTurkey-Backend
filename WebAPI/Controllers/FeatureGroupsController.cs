using Business.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FeatureGroupsController : ControllerBase
{
    private readonly IFeatureGroupService _featureGroupService;

    public FeatureGroupsController(IFeatureGroupService featureGroupService)
    {
        _featureGroupService = featureGroupService;
    }

    [HttpGet("getall")]
    public IActionResult GetAll()
    {
        var result = _featureGroupService.GetAll();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("getbyid")]
    public IActionResult GetById(int id)
    {
        var result = _featureGroupService.GetById(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("add")]
    [RequireCapability("admin.feature_group_manage")]
    public IActionResult Add(FeatureGroup featureGroup)
    {
        var result = _featureGroupService.Add(featureGroup);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update")]
    [RequireCapability("admin.feature_group_manage")]
    public IActionResult Update(FeatureGroup featureGroup)
    {
        var result = _featureGroupService.Update(featureGroup);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("delete")]
    [RequireCapability("admin.feature_group_manage")]
    public IActionResult Delete(int id)
    {
        var result = _featureGroupService.Delete(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
