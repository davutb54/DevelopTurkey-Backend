using Business.Abstract;
using Core.Utilities.Authorization;
using Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebAPI.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IInstitutionFeatureService _featureService;
    private readonly IMessageService _messageService;
    private readonly IConversationService _conversationService;
    private readonly ICapabilityResolver _resolver;

    public ChatHub(
        IInstitutionFeatureService featureService,
        IMessageService messageService,
        IConversationService conversationService,
        ICapabilityResolver resolver)
    {
        _featureService      = featureService;
        _messageService      = messageService;
        _conversationService = conversationService;
        _resolver            = resolver;
    }

    public override async Task OnConnectedAsync()
    {
        int institutionId = GetInstitutionId();

        // Hub'a bağlanmak için ya genel sohbet ya da destek sohbeti feature'ından biri açık olmalı
        bool chatOk    = _featureService.IsFeatureEnabled(institutionId, "Communication.EnableChat",        false);
        bool supportOk = _featureService.IsFeatureEnabled(institutionId, "Communication.EnableSupportChat", false);
        if (!chatOk && !supportOk)
        {
            Context.Abort();
            return;
        }

        int userId = GetUserId();
        if (!_resolver.Allows(userId, "chat.use", null))
        {
            Context.Abort();
            return;
        }

        // Kullanıcıyı kendi personal grubuna ekle (doğrudan bildirim için)
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        // Hiyerarşik destek havuzu grupları — kümülatif (üst tier alt tier'ları da kapsar)
        bool hasHandleSupport     = _resolver.Allows(userId, "chat.handle_support", null);
        bool hasHandleEscalations = _resolver.Allows(userId, "chat.handle_escalations", null);
        bool hasInstitutionManage = _resolver.Allows(userId, "chat.institution_manage", null);
        bool hasGlobalManage      = _resolver.Allows(userId, "chat.global_manage", null);

        if (hasHandleSupport || hasHandleEscalations || hasInstitutionManage || hasGlobalManage)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"support_pool_general_{institutionId}");

        if (hasHandleEscalations || hasInstitutionManage || hasGlobalManage)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"support_pool_expert_{institutionId}");

        if (hasInstitutionManage || hasGlobalManage)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"support_pool_moderator_{institutionId}");

        if (hasGlobalManage)
            await Groups.AddToGroupAsync(Context.ConnectionId, "support_pool_admin_global");

        await base.OnConnectedAsync();
    }

    // ── Hub Metotları ─────────────────────────────────────────────────────────

    /// <summary>Konuşma odasına katıl (mesajları almak için).</summary>
    public async Task JoinConversation(int conversationId)
    {
        int userId        = GetUserId();
        int institutionId = GetInstitutionId();

        if (!_featureService.IsFeatureEnabled(institutionId, "Communication.EnableChat", false)
            && !_featureService.IsFeatureEnabled(institutionId, "Communication.EnableSupportChat", false))
            throw new HubException("Sohbet özelliği etkin değil.");

        if (!_resolver.Allows(userId, "chat.use", null))
            throw new HubException("Sohbet yetkisi yok.");

        var detailResult = _conversationService.GetDetail(conversationId, userId);
        if (!detailResult.Success)
            throw new HubException(detailResult.Message);

        var conv = detailResult.Data;
        if (conv.Scope == "global" && !_resolver.Allows(userId, "chat.global_manage", null))
            throw new HubException("Global konuşmalara erişim için chat.global_manage yetkisi gerekiyor.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
    }

    /// <summary>Konuşma odasından ayrıl.</summary>
    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
    }

    /// <summary>Mesaj gönder — doğrulama + persist + diğer katılımcılara dağıt.</summary>
    public async Task SendMessage(int conversationId, string body)
    {
        int userId        = GetUserId();
        int institutionId = GetInstitutionId();

        if (!_featureService.IsFeatureEnabled(institutionId, "Communication.EnableChat", false)
            && !_featureService.IsFeatureEnabled(institutionId, "Communication.EnableSupportChat", false))
            throw new HubException("Sohbet özelliği etkin değil.");

        if (!_resolver.Allows(userId, "chat.use", null))
            throw new HubException("Mesaj gönderme yetkisi yok.");

        var result = _messageService.Send(conversationId, userId, new SendMessageDto { Body = body });
        if (!result.Success)
            throw new HubException(result.Message);

        await Clients.Group($"conv_{conversationId}").SendAsync("ReceiveMessage", result.Data);
    }

    /// <summary>Okundu işareti — kullanıcının son okuduğu mesajı kaydet.</summary>
    public async Task MarkRead(int conversationId, int lastReadMessageId)
    {
        int userId = GetUserId();
        _messageService.MarkRead(conversationId, userId, new MarkReadDto { LastReadMessageId = lastReadMessageId });
        await Clients.Group($"user_{userId}").SendAsync("ReadAck", new { conversationId, lastReadMessageId });
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private int GetUserId()
    {
        var claim = Context.User?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }

    private int GetInstitutionId()
    {
        var claim = Context.User?.Claims.FirstOrDefault(c => c.Type == "InstitutionId");
        return claim != null ? int.Parse(claim.Value) : 1;
    }
}
