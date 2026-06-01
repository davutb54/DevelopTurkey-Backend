using Business.Abstract;
using Core.Utilities.Authorization;
using Core.Utilities.Context;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Filters;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;
    private readonly IClientContext       _clientContext;

    public AnnouncementsController(
        IAnnouncementService announcementService,
        IClientContext clientContext)
    {
        _announcementService = announcementService;
        _clientContext        = clientContext;
    }

    // ── GET /api/announcements — Aktif duyurular (public) ────────────────────
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetActive([FromQuery] int? institutionId)
    {
        var result = _announcementService.GetActive(institutionId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ── GET /api/announcements/all — Admin: tüm duyurular ───────────────────
    [HttpGet("all")]
    [RequireCapability("admin.announcement_read")]
    public IActionResult GetAll()
    {
        var result = _announcementService.GetAll();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ── POST /api/announcements — Duyuru oluştur ─────────────────────────────
    [HttpPost]
    [RequireCapability("admin.announcement_create")]
    public IActionResult Create([FromBody] CreateAnnouncementDto dto)
    {
        var userId = (int)(_clientContext.GetUserId() ?? 0);
        var result = _announcementService.Create(dto, userId);
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }

    // ── PUT /api/announcements/{id}/deactivate ────────────────────────────────
    [HttpPut("{id}/deactivate")]
    [RequireCapability("admin.announcement_create")]
    public IActionResult Deactivate(int id)
    {
        var result = _announcementService.Deactivate(id);
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }

    // ── DELETE /api/announcements/{id} ───────────────────────────────────────
    [HttpDelete("{id}")]
    [RequireCapability("admin.announcement_delete")]
    public IActionResult Delete(int id)
    {
        var result = _announcementService.Delete(id);
        return result.Success ? Ok(new { success = true, message = result.Message }) : BadRequest(result);
    }
}
