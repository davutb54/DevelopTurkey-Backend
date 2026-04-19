using Business.Abstract;
using Core.Utilities.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IClientContext _clientContext;

    public NotificationController(INotificationService notificationService, IClientContext clientContext)
    {
        _notificationService = notificationService;
        _clientContext = clientContext;
    }

    [HttpGet("get-unread")]
    public IActionResult GetUnread()
    {
        var userId = _clientContext.GetUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = _notificationService.GetUnreadByUserId(userId.Value);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("get-all")]
    public IActionResult GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = _clientContext.GetUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = _notificationService.GetAllByUserId(userId.Value, page, pageSize);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("mark-read")]
    public IActionResult MarkRead([FromBody] MarkReadRequest request)
    {
        var userId = _clientContext.GetUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = _notificationService.MarkAsRead(request.NotificationId, userId.Value);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("mark-all-read")]
    public IActionResult MarkAllRead()
    {
        var userId = _clientContext.GetUserId();
        if (!userId.HasValue) return Unauthorized();

        var result = _notificationService.MarkAllAsRead(userId.Value);
        if (result.Success) return Ok(result);
        return BadRequest(result);
    }
}

public class MarkReadRequest
{
    public int NotificationId { get; set; }
}
