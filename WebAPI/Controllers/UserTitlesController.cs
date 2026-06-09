using Business.Abstract;
using Core.Utilities.Context;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/user-titles")]
[ApiController]
public class UserTitlesController : ControllerBase
{
    private readonly IUserTitleService _userTitleService;
    private readonly IClientContext _clientContext;

    public UserTitlesController(IUserTitleService userTitleService, IClientContext clientContext)
    {
        _userTitleService = userTitleService;
        _clientContext = clientContext;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetByUser([FromQuery] int userId)
    {
        var result = _userTitleService.GetByUser(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    [RequireCapability("admin.user_title_assign")]
    public IActionResult Assign([FromBody] UserTitleAddDto dto)
    {
        var result = _userTitleService.Assign(dto);
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    [RequireCapability("admin.user_title_assign")]
    public IActionResult Remove(int id)
    {
        var result = _userTitleService.Remove(id);
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }
}
