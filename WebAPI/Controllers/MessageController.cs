using Business.Abstract;
using Core.Utilities.Context;
using Entities.DTOs;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IClientContext _clientContext;
    private readonly IInstitutionFeatureService _featureService;

    public MessageController(
        IMessageService messageService,
        IClientContext clientContext,
        IInstitutionFeatureService featureService)
    {
        _messageService  = messageService;
        _clientContext   = clientContext;
        _featureService  = featureService;
    }

    [HttpGet("conversation/{conversationId}")]
    [RequireCapability("chat.use")]
    public IActionResult GetHistory(int conversationId, [FromQuery] int pageSize = 50, [FromQuery] int? beforeMessageId = null)
    {
        if (!ChatEnabled()) return Forbid();
        var result = _messageService.GetHistory(conversationId, _clientContext.GetUserId() ?? 0, pageSize, beforeMessageId);
        return result.Success ? Ok(new { success = true, data = result.Data }) : BadRequest(new { success = false, message = result.Message });
    }

    [HttpPost("conversation/{conversationId}")]
    [RequireCapability("chat.use")]
    public IActionResult Send(int conversationId, [FromBody] SendMessageDto dto)
    {
        if (!ChatEnabled()) return Forbid();
        var result = _messageService.Send(conversationId, _clientContext.GetUserId() ?? 0, dto);
        return result.Success ? Ok(new { success = true, data = result.Data }) : BadRequest(new { success = false, message = result.Message });
    }

    [HttpPost("conversation/{conversationId}/read")]
    [RequireCapability("chat.use")]
    public IActionResult MarkRead(int conversationId, [FromBody] MarkReadDto dto)
    {
        if (!ChatEnabled()) return Forbid();
        var result = _messageService.MarkRead(conversationId, _clientContext.GetUserId() ?? 0, dto);
        return result.Success ? Ok(new { success = true }) : BadRequest(new { success = false, message = result.Message });
    }

    [HttpDelete("{messageId}")]
    [RequireCapability("chat.use")]
    public IActionResult DeleteMessage(int messageId)
    {
        if (!ChatEnabled()) return Forbid();
        var result = _messageService.DeleteMessage(messageId, _clientContext.GetUserId() ?? 0);
        return result.Success ? Ok(new { success = true }) : BadRequest(new { success = false, message = result.Message });
    }

    private bool ChatEnabled()
    {
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        return _featureService.IsFeatureEnabled(institutionId, "Communication.EnableChat", false);
    }
}
