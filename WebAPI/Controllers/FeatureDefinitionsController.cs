using Business.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FeatureDefinitionsController : ControllerBase
{
    private readonly IFeatureDefinitionService _featureDefinitionService;

    public FeatureDefinitionsController(IFeatureDefinitionService featureDefinitionService)
    {
        _featureDefinitionService = featureDefinitionService;
    }

    [HttpGet("getall")]
    public IActionResult GetAll()
    {
        var result = _featureDefinitionService.GetAll();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("getbyid")]
    public IActionResult GetById(int id)
    {
        var result = _featureDefinitionService.GetById(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("getbygroupid")]
    public IActionResult GetByGroupId(int groupId)
    {
        var result = _featureDefinitionService.GetByGroupId(groupId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("add")]
    [Authorize(Roles = "Admin")]
    public IActionResult Add(FeatureDefinition featureDefinition)
    {
        var result = _featureDefinitionService.Add(featureDefinition);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("update")]
    [Authorize(Roles = "Admin")]
    public IActionResult Update(FeatureDefinition featureDefinition)
    {
        var result = _featureDefinitionService.Update(featureDefinition);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("delete")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id)
    {
        var result = _featureDefinitionService.Delete(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
