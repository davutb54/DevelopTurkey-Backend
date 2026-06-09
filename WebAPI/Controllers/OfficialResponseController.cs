using Business.Abstract;
using Core.Utilities.Context;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/official-responses")]
[ApiController]
public class OfficialResponseController : ControllerBase
{
    private readonly IOfficialResponseService _responseService;
    private readonly IClientContext _clientContext;

    public OfficialResponseController(IOfficialResponseService responseService, IClientContext clientContext)
    {
        _responseService = responseService;
        _clientContext = clientContext;
    }

    [HttpGet("by-problem/{problemId}")]
    [AllowAnonymous]
    public IActionResult GetByProblem(int problemId)
    {
        var result = _responseService.GetByProblem(problemId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    [RequireCapability("official.response_create")]
    public IActionResult Add([FromBody] OfficialResponseAddDto dto)
    {
        var result = _responseService.Add(dto);
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }

    [HttpPut("{id}/status")]
    [RequireCapability("official.response_update")]
    public IActionResult UpdateStatus(int id, [FromBody] OfficialResponseUpdateStatusDto dto)
    {
        var result = _responseService.UpdateStatus(id, dto);
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    [RequireCapability("official.response_create")]
    public IActionResult Delete(int id)
    {
        var result = _responseService.Delete(id);
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }
}
