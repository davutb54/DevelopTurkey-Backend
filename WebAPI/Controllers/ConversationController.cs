using Business.Abstract;
using Core.Utilities.Context;
using Entities.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Filters;
using WebAPI.Hubs;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IClientContext _clientContext;
    private readonly IInstitutionFeatureService _featureService;
    private readonly IHubContext<ChatHub> _hub;

    public ConversationController(
        IConversationService conversationService,
        IClientContext clientContext,
        IInstitutionFeatureService featureService,
        IHubContext<ChatHub> hub)
    {
        _conversationService = conversationService;
        _clientContext       = clientContext;
        _featureService      = featureService;
        _hub                 = hub;
    }

    // ── Sorgular ─────────────────────────────────────────────────────────────

    [HttpGet]
    [RequireCapability("chat.use")]
    public IActionResult GetMyConversations()
    {
        if (!ChatEnabled()) return Forbid();
        int userId        = _clientContext.GetUserId() ?? 0;
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        var result = _conversationService.GetMyConversations(userId, institutionId);
        return result.Success ? Ok(new { success = true, data = result.Data }) : BadRequest(new { success = false, message = result.Message });
    }

    [HttpGet("{id}")]
    [RequireCapability("chat.use")]
    public IActionResult GetDetail(int id)
    {
        if (!ChatEnabled()) return Forbid();
        var result = _conversationService.GetDetail(id, _clientContext.GetUserId() ?? 0);
        return result.Success ? Ok(new { success = true, data = result.Data }) : BadRequest(new { success = false, message = result.Message });
    }

    /// <summary>Destek havuzu — capability'ye göre filtrelenmiş pending + aktif destek konuşmaları.</summary>
    [HttpGet("pool")]
    [RequireCapability("chat.use")]
    public IActionResult GetPool()
    {
        if (!ChatEnabled()) return Forbid();
        int userId        = _clientContext.GetUserId() ?? 0;
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        var result = _conversationService.GetPool(userId, institutionId);
        return result.Success ? Ok(new { success = true, data = result.Data }) : BadRequest(new { success = false, message = result.Message });
    }

    // ── Oluşturma ─────────────────────────────────────────────────────────────

    [HttpPost("direct")]
    [RequireCapability("chat.use")]
    public async Task<IActionResult> StartDirect([FromBody] StartDirectConversationDto dto)
    {
        if (!ChatEnabled()) return Forbid();
        int userId        = _clientContext.GetUserId() ?? 0;
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        var result = _conversationService.StartDirect(userId, institutionId, dto);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });

        // Karşı taraftaki kullanıcıya yeni konuşmayı push et (bug fix)
        await _hub.Clients.Group($"user_{dto.TargetUserId}")
            .SendAsync("ConversationAdded", result.Data);

        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("group")]
    [RequireCapability("chat.institution_manage")]
    public async Task<IActionResult> StartGroup([FromBody] StartGroupConversationDto dto)
    {
        if (!ChatEnabled()) return Forbid();
        int userId        = _clientContext.GetUserId() ?? 0;
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        var result = _conversationService.StartGroup(userId, institutionId, dto);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });

        // Tüm katılımcılara (yaratıcı dahil) yeni konuşmayı push et (bug fix)
        var participantIds = _conversationService.GetParticipantUserIds(result.Data.Id);
        foreach (var uid in participantIds.Where(uid => uid != userId))
            await _hub.Clients.Group($"user_{uid}").SendAsync("ConversationAdded", result.Data);

        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("support")]
    [RequireCapability("chat.use")]
    public async Task<IActionResult> StartSupport([FromBody] StartSupportConversationDto dto)
    {
        if (!SupportChatEnabled()) return Forbid();
        int userId        = _clientContext.GetUserId() ?? 0;
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        var result = _conversationService.StartSupport(userId, institutionId, dto);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });

        // Kategori bazlı destek havuzu grubuna bildirim gönder
        var category  = result.Data.SupportCategory ?? "general";
        var poolGroup = GetPoolGroupName(category, institutionId);
        await _hub.Clients.Group(poolGroup).SendAsync("SupportNew", result.Data);

        return Ok(new { success = true, data = result.Data });
    }

    // ── Katılımcı Yönetimi ────────────────────────────────────────────────────

    [HttpPost("{id}/participants")]
    [RequireCapability("chat.institution_manage")]
    public async Task<IActionResult> AddParticipant(int id, [FromBody] AddParticipantDto dto)
    {
        if (!ChatEnabled()) return Forbid();
        var result = _conversationService.AddParticipant(id, _clientContext.GetUserId() ?? 0, dto);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });

        // Yeni eklenen kullanıcıya konuşmayı push et (bug fix)
        var detail = _conversationService.GetDetail(id, _clientContext.GetUserId() ?? 0);
        if (detail.Success)
            await _hub.Clients.Group($"user_{dto.UserId}").SendAsync("ConversationAdded", detail.Data);

        return Ok(new { success = true, message = result.Message });
    }

    [HttpDelete("{id}/participants/{userId}")]
    [RequireCapability("chat.use")]
    public async Task<IActionResult> RemoveParticipant(int id, int userId)
    {
        if (!ChatEnabled()) return Forbid();
        var result = _conversationService.RemoveParticipant(id, _clientContext.GetUserId() ?? 0, userId);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });

        // Çıkarılan kullanıcıya bildir
        await _hub.Clients.Group($"user_{userId}")
            .SendAsync("ConversationRemoved", new { conversationId = id });

        return Ok(new { success = true, message = result.Message });
    }

    // ── Destek Eylemleri ──────────────────────────────────────────────────────

    [HttpPost("{id}/claim")]
    [RequireCapability("chat.use")]
    public async Task<IActionResult> Claim(int id)
    {
        if (!ChatEnabled()) return Forbid();
        int staffUserId   = _clientContext.GetUserId() ?? 0;
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        var result = _conversationService.Claim(id, staffUserId);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });

        // Tüm pool gruplarına SupportClaimed yayınla (client hangisinde olduğunu bilmez)
        foreach (var poolGroup in GetAllPoolGroups(institutionId))
            await _hub.Clients.Group(poolGroup).SendAsync("SupportClaimed", new { conversationId = id, staffUserId });

        // Talep sahibine (conversation katılımcılarına) konuşma güncelleme bilgisi gönder
        var detail = _conversationService.GetDetail(id, staffUserId);
        if (detail.Success)
        {
            await _hub.Clients.Group($"conv_{id}").SendAsync("ConversationUpdated", detail.Data);
            // Yetkili artık katılımcı — kendi user grubundan da bildir
            await _hub.Clients.Group($"user_{staffUserId}").SendAsync("ConversationAdded", detail.Data);
        }

        return Ok(new { success = true, message = result.Message });
    }

    [HttpPost("{id}/close")]
    [RequireCapability("chat.use")]
    public async Task<IActionResult> CloseConversation(int id)
    {
        if (!ChatEnabled()) return Forbid();
        int userId        = _clientContext.GetUserId() ?? 0;
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        var result = _conversationService.CloseConversation(id, userId);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });

        // Tüm katılımcılara konuşmanın kapandığını bildir
        await _hub.Clients.Group($"conv_{id}").SendAsync("ConversationClosed", new { conversationId = id });
        // Tüm pool gruplarına SupportClosed yayınla
        foreach (var poolGroup in GetAllPoolGroups(institutionId))
            await _hub.Clients.Group(poolGroup).SendAsync("SupportClosed", new { conversationId = id });

        return Ok(new { success = true, message = result.Message });
    }

    [HttpPatch("{id}/title")]
    [RequireCapability("chat.institution_manage")]
    public async Task<IActionResult> UpdateTitle(int id, [FromBody] UpdateConversationTitleDto dto)
    {
        if (!ChatEnabled()) return Forbid();
        int userId = _clientContext.GetUserId() ?? 0;
        var result = _conversationService.UpdateTitle(id, userId, dto.Title);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });

        await _hub.Clients.Group($"conv_{id}")
            .SendAsync("ConversationUpdated", new { conversationId = id, title = dto.Title });

        return Ok(new { success = true, message = result.Message });
    }

    // ── Silme ────────────────────────────────────────────────────────────────

    [HttpDelete("{id}")]
    [RequireCapability("chat.institution_manage")]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        if (!ChatEnabled()) return Forbid();
        int userId        = _clientContext.GetUserId() ?? 0;
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        var result = _conversationService.DeleteConversation(id, userId);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });

        // Tüm katılımcılara konuşmanın silindiğini bildir
        await _hub.Clients.Group($"conv_{id}").SendAsync("ConversationRemoved", new { conversationId = id });
        // Havuz gruplarına da yayınla (support konuşma olabilir)
        foreach (var poolGroup in GetAllPoolGroups(institutionId))
            await _hub.Clients.Group(poolGroup).SendAsync("SupportClosed", new { conversationId = id });

        return Ok(new { success = true, message = result.Message });
    }

    // ── Yardımcı ──────────────────────────────────────────────────────────────

    private bool ChatEnabled()
    {
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        return _featureService.IsFeatureEnabled(institutionId, "Communication.EnableChat", false)
            || _featureService.IsFeatureEnabled(institutionId, "Communication.EnableSupportChat", false);
    }

    private bool SupportChatEnabled()
    {
        int institutionId = _clientContext.GetInstitutionId() ?? 1;
        // EnableSupportChat VEYA EnableChat açık ise destek talebi çalışır
        return _featureService.IsFeatureEnabled(institutionId, "Communication.EnableSupportChat", false)
            || _featureService.IsFeatureEnabled(institutionId, "Communication.EnableChat", false);
    }

    /// <summary>Destek kategorisine göre SignalR pool group adını döner.</summary>
    private static string GetPoolGroupName(string category, int institutionId) => category switch
    {
        "expert"    => $"support_pool_expert_{institutionId}",
        "moderator" => $"support_pool_moderator_{institutionId}",
        "admin"     => "support_pool_admin_global",
        _           => $"support_pool_general_{institutionId}",
    };

    /// <summary>Bir kurum için tüm pool grup adlarını döner (broadcast için).</summary>
    private static IEnumerable<string> GetAllPoolGroups(int institutionId) =>
    [
        $"support_pool_general_{institutionId}",
        $"support_pool_expert_{institutionId}",
        $"support_pool_moderator_{institutionId}",
        "support_pool_admin_global",
    ];
}
