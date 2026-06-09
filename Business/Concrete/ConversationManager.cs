using Business.Abstract;
using Core.Utilities.Authorization;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;

namespace Business.Concrete;

public class ConversationManager : IConversationService
{
    private readonly IConversationDal _conversationDal;
    private readonly IConversationParticipantDal _participantDal;
    private readonly IMessageDal _messageDal;
    private readonly IUserDal _userDal;
    private readonly ICapabilityResolver _resolver;

    public ConversationManager(
        IConversationDal conversationDal,
        IConversationParticipantDal participantDal,
        IMessageDal messageDal,
        IUserDal userDal,
        ICapabilityResolver resolver)
    {
        _conversationDal = conversationDal;
        _participantDal  = participantDal;
        _messageDal      = messageDal;
        _userDal         = userDal;
        _resolver        = resolver;
    }

    // ── Direct Konuşma ───────────────────────────────────────────────────────

    public IDataResult<ConversationDetailDto> StartDirect(int callerUserId, int callerInstitutionId, StartDirectConversationDto dto)
    {
        if (dto.TargetUserId == callerUserId)
            return new ErrorDataResult<ConversationDetailDto>(null!, "Kendinizle konuşma başlatamazsınız.");

        var target = _userDal.Get(u => u.Id == dto.TargetUserId);
        if (target == null)
            return new ErrorDataResult<ConversationDetailDto>(null!, "Hedef kullanıcı bulunamadı.");

        bool targetSameInstitution = target.InstitutionId == callerInstitutionId;
        bool canGlobal = _resolver.Allows(callerUserId, "chat.global_manage", null);
        if (!targetSameInstitution && !canGlobal)
            return new ErrorDataResult<ConversationDetailDto>(null!, "Farklı kurum kullanıcısıyla konuşmak için chat.global_manage yetkisi gerekiyor.");

        // Kurum yöneticisi ve global yönetici herhangi birine DM başlatabilir
        bool callerIsManager = _resolver.Allows(callerUserId, "chat.institution_manage", null)
                            || _resolver.Allows(callerUserId, "chat.global_manage", null);

        if (!callerIsManager)
        {
            bool targetCanReceive = _resolver.Allows(dto.TargetUserId, "chat.official_channel", null)
                                 || _resolver.Allows(dto.TargetUserId, "chat.institution_manage", null)
                                 || _resolver.Allows(dto.TargetUserId, "chat.global_manage", null)
                                 || _resolver.Allows(dto.TargetUserId, "chat.handle_support", null)
                                 || _resolver.Allows(dto.TargetUserId, "chat.handle_escalations", null);
            bool callerHasEscalation = _resolver.Allows(callerUserId, "chat.contact_admin", null)
                                    || _resolver.Allows(callerUserId, "chat.support_request", null);

            if (!targetCanReceive && !callerHasEscalation)
                return new ErrorDataResult<ConversationDetailDto>(null!, "Bu kullanıcı mesaj kabul etmiyor.");
        }

        // Zaten var mı?
        var existingPairs = _participantDal.GetAll(p => p.UserId == callerUserId).Select(p => p.ConversationId).ToHashSet();
        var targetPairs   = _participantDal.GetAll(p => p.UserId == dto.TargetUserId).Select(p => p.ConversationId).ToHashSet();
        foreach (var sid in existingPairs.Intersect(targetPairs))
        {
            var shared = _conversationDal.Get(c => c.Id == sid && c.Type == "direct");
            if (shared != null)
                return new SuccessDataResult<ConversationDetailDto>(BuildDetail(shared), "Mevcut konuşma döndürüldü.");
        }

        var scope = targetSameInstitution ? "institution" : "global";
        var conv = new Conversation
        {
            Type            = "direct",
            Scope           = scope,
            InstitutionId   = scope == "institution" ? callerInstitutionId : null,
            CreatedByUserId = callerUserId,
            Status          = "active",
        };
        _conversationDal.Add(conv);
        _participantDal.Add(new ConversationParticipant { ConversationId = conv.Id, UserId = callerUserId,      Role = "admin" });
        _participantDal.Add(new ConversationParticipant { ConversationId = conv.Id, UserId = dto.TargetUserId, Role = "member" });

        return new SuccessDataResult<ConversationDetailDto>(BuildDetail(conv), "Konuşma başlatıldı.");
    }

    // ── Grup Konuşması ───────────────────────────────────────────────────────

    public IDataResult<ConversationDetailDto> StartGroup(int callerUserId, int callerInstitutionId, StartGroupConversationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return new ErrorDataResult<ConversationDetailDto>(null!, "Grup adı boş olamaz.");

        if (!_resolver.Allows(callerUserId, "chat.institution_manage", null)
            && !_resolver.Allows(callerUserId, "chat.global_manage", null))
            return new ErrorDataResult<ConversationDetailDto>(null!, "Grup konuşması oluşturmak için chat.institution_manage yetkisi gerekiyor.");

        var conv = new Conversation
        {
            Type            = "group",
            Scope           = "institution",
            InstitutionId   = callerInstitutionId,
            Title           = dto.Title.Trim(),
            CreatedByUserId = callerUserId,
            Status          = "active",
        };
        _conversationDal.Add(conv);
        _participantDal.Add(new ConversationParticipant { ConversationId = conv.Id, UserId = callerUserId, Role = "admin" });
        foreach (var uid in dto.ParticipantUserIds.Distinct().Where(id => id != callerUserId))
        {
            if (_userDal.Get(u => u.Id == uid && u.InstitutionId == callerInstitutionId) != null)
                _participantDal.Add(new ConversationParticipant { ConversationId = conv.Id, UserId = uid, Role = "member" });
        }

        return new SuccessDataResult<ConversationDetailDto>(BuildDetail(conv), "Grup konuşması oluşturuldu.");
    }

    // ── Destek Talebi ────────────────────────────────────────────────────────

    public IDataResult<ConversationDetailDto> StartSupport(int callerUserId, int callerInstitutionId, StartSupportConversationDto dto)
    {
        // "official" → "expert" backward compat normalizasyonu
        var rawCat = dto.Category is "general" or "expert" or "official" or "moderator" or "admin"
            ? dto.Category : "general";
        var category = rawCat == "official" ? "expert" : rawCat;

        // Hiyerarşik yetki kontrolü
        var (requiredCap, errorMsg) = category switch
        {
            "general"   => ("chat.support_request",      "Destek talebi açmak için chat.support_request yetkisi gerekiyor."),
            "expert"    => ("chat.escalate",             "Moderatör kanalına ulaşmak için chat.escalate yetkisi gerekiyor."),
            "moderator" => ("chat.contact_admin",        "Admin kanalına ulaşmak için chat.contact_admin yetkisi gerekiyor."),
            "admin"     => ("chat.contact_global_admin", "Global admin kanalına ulaşmak için chat.contact_global_admin yetkisi gerekiyor."),
            _           => ("chat.support_request",      "Geçersiz destek kategorisi."),
        };

        if (!_resolver.Allows(callerUserId, requiredCap, null))
            return new ErrorDataResult<ConversationDetailDto>(null!, errorMsg);

        var title = category switch
        {
            "expert"    => "Yöneticiye Ulaşma Talebi",
            "moderator" => "Admine Ulaşma Talebi",
            "admin"     => "Global Admin Destek Talebi",
            _           => "Destek Talebi",
        };

        var conv = new Conversation
        {
            Type            = "support",
            Scope           = "institution",
            InstitutionId   = callerInstitutionId,
            Title           = title,
            CreatedByUserId = callerUserId,
            Status          = "pending",
            SupportCategory = category,
        };
        _conversationDal.Add(conv);
        _participantDal.Add(new ConversationParticipant { ConversationId = conv.Id, UserId = callerUserId, Role = "member" });

        return new SuccessDataResult<ConversationDetailDto>(BuildDetail(conv), "Destek talebi oluşturuldu.");
    }

    // ── Sorgular ─────────────────────────────────────────────────────────────

    public IDataResult<List<ConversationSummaryDto>> GetMyConversations(int callerUserId, int callerInstitutionId)
    {
        var myConvIds = _participantDal.GetAll(p => p.UserId == callerUserId).Select(p => p.ConversationId).ToList();
        var result = new List<ConversationSummaryDto>();

        foreach (var cid in myConvIds)
        {
            var conv = _conversationDal.Get(c => c.Id == cid);
            if (conv == null) continue;
            if (conv.Scope == "institution" && conv.InstitutionId != callerInstitutionId) continue;

            var participants = _participantDal.GetAll(p => p.ConversationId == cid);
            result.Add(MapToSummary(conv, participants.Count));
        }

        return new SuccessDataResult<List<ConversationSummaryDto>>(
            result.OrderByDescending(c => c.CreatedAt).ToList());
    }

    public IDataResult<List<ConversationSummaryDto>> GetPool(int staffUserId, int institutionId)
    {
        // Hiyerarşi: her seviye kümülatif kategorileri görür
        var allowedCategories = new List<string>();

        if (_resolver.Allows(staffUserId, "chat.global_manage", null))
            allowedCategories.AddRange(["general", "expert", "moderator", "admin"]);
        else if (_resolver.Allows(staffUserId, "chat.institution_manage", null))
            allowedCategories.AddRange(["general", "expert", "moderator"]);
        else if (_resolver.Allows(staffUserId, "chat.handle_escalations", null))
            allowedCategories.AddRange(["general", "expert"]);
        else if (_resolver.Allows(staffUserId, "chat.handle_support", null))
            allowedCategories.Add("general");

        if (allowedCategories.Count == 0)
            return new ErrorDataResult<List<ConversationSummaryDto>>(null!, "Destek havuzunu görüntülemek için yetki gerekiyor.");

        // Pending (sahipsiz) + bu yetkilinin üstlendiği active support konuşmaları
        // EF global query filter kuruma göre izolasyonu zaten sağlar; global_manage bypass eder
        var all = _conversationDal.GetAll(c =>
            c.Type == "support"
            && allowedCategories.Contains(c.SupportCategory ?? "general")
            && (c.Status == "pending" || (c.Status == "active" && c.AssignedToUserId == staffUserId)));

        var result = all.Select(c =>
        {
            var cnt = _participantDal.GetAll(p => p.ConversationId == c.Id).Count;
            return MapToSummary(c, cnt);
        }).OrderBy(c => c.Status == "pending" ? 0 : 1)
          .ThenByDescending(c => c.CreatedAt)
          .ToList();

        return new SuccessDataResult<List<ConversationSummaryDto>>(result);
    }

    public IDataResult<ConversationDetailDto> GetDetail(int conversationId, int callerUserId)
    {
        var conv = _conversationDal.Get(c => c.Id == conversationId);
        if (conv == null)
            return new ErrorDataResult<ConversationDetailDto>(null!, "Konuşma bulunamadı.");

        // Destek havuzundaki pending konuşmalar yetkili tarafından görülebilir (katılımcı olmasa bile)
        bool isParticipant = IsParticipant(conversationId, callerUserId);
        bool isStaff = _resolver.Allows(callerUserId, "chat.institution_manage",  null)
                    || _resolver.Allows(callerUserId, "chat.global_manage",        null)
                    || _resolver.Allows(callerUserId, "chat.handle_support",       null)
                    || _resolver.Allows(callerUserId, "chat.handle_escalations",   null);

        if (!isParticipant && !(conv.Type == "support" && isStaff))
            return new ErrorDataResult<ConversationDetailDto>(null!, "Bu konuşmaya erişim yetkiniz yok.");

        return new SuccessDataResult<ConversationDetailDto>(BuildDetail(conv));
    }

    // ── Katılımcı Yönetimi ───────────────────────────────────────────────────

    public IResult AddParticipant(int conversationId, int callerUserId, AddParticipantDto dto)
    {
        if (!IsAdminOfConversation(conversationId, callerUserId)
            && !_resolver.Allows(callerUserId, "chat.institution_manage", null))
            return new ErrorResult("Bu konuşmaya katılımcı ekleme yetkiniz yok.");

        if (_participantDal.Get(p => p.ConversationId == conversationId && p.UserId == dto.UserId) != null)
            return new SuccessResult("Kullanıcı zaten konuşmada.");

        _participantDal.Add(new ConversationParticipant { ConversationId = conversationId, UserId = dto.UserId, Role = "member" });
        return new SuccessResult("Katılımcı eklendi.");
    }

    public IResult RemoveParticipant(int conversationId, int callerUserId, int targetUserId)
    {
        if (!IsAdminOfConversation(conversationId, callerUserId) && callerUserId != targetUserId)
            return new ErrorResult("Bu işlem için yetkiniz yok.");

        var participant = _participantDal.Get(p => p.ConversationId == conversationId && p.UserId == targetUserId);
        if (participant == null)
            return new ErrorResult("Katılımcı bulunamadı.");

        _participantDal.Delete(participant);
        return new SuccessResult("Katılımcı kaldırıldı.");
    }

    // ── Destek Eylemleri ─────────────────────────────────────────────────────

    public IResult Claim(int conversationId, int staffUserId)
    {
        bool hasHandleSupport     = _resolver.Allows(staffUserId, "chat.handle_support",     null);
        bool hasHandleEscalations = _resolver.Allows(staffUserId, "chat.handle_escalations", null);
        bool hasInstitutionManage = _resolver.Allows(staffUserId, "chat.institution_manage", null);
        bool hasGlobalManage      = _resolver.Allows(staffUserId, "chat.global_manage",      null);
        bool isStaff = hasHandleSupport || hasHandleEscalations || hasInstitutionManage || hasGlobalManage;
        if (!isStaff)
            return new ErrorResult("Destek talebini sahiplenmek için yetki gerekiyor.");

        var conv = _conversationDal.Get(c => c.Id == conversationId && c.Type == "support");
        if (conv == null)
            return new ErrorResult("Destek konuşması bulunamadı.");
        if (conv.Status != "pending")
            return new ErrorResult("Bu konuşma zaten sahiplenilmiş veya kapatılmış.");

        // Kategori yetki kontrolü — alt tier'lar sadece ilgili kategorileri sahiplenebilir
        var category = conv.SupportCategory ?? "general";
        if (!hasInstitutionManage && !hasGlobalManage)
        {
            if (hasHandleEscalations && category != "general" && category != "expert")
                return new ErrorResult("Bu kategori için yetkiniz yok.");
            if (hasHandleSupport && !hasHandleEscalations && category != "general")
                return new ErrorResult("Bu kategori için yetkiniz yok.");
        }

        conv.Status           = "active";
        conv.AssignedToUserId = staffUserId;
        _conversationDal.Update(conv);

        // Yetkiliyi katılımcı olarak ekle
        if (_participantDal.Get(p => p.ConversationId == conversationId && p.UserId == staffUserId) == null)
            _participantDal.Add(new ConversationParticipant { ConversationId = conversationId, UserId = staffUserId, Role = "admin" });

        return new SuccessResult("Destek talebi sahiplenildi.");
    }

    public IResult CloseConversation(int conversationId, int callerUserId)
    {
        var conv = _conversationDal.Get(c => c.Id == conversationId);
        if (conv == null)
            return new ErrorResult("Konuşma bulunamadı.");

        bool isParticipant = IsParticipant(conversationId, callerUserId);
        bool isStaff = _resolver.Allows(callerUserId, "chat.institution_manage",  null)
                    || _resolver.Allows(callerUserId, "chat.global_manage",        null)
                    || _resolver.Allows(callerUserId, "chat.handle_support",       null)
                    || _resolver.Allows(callerUserId, "chat.handle_escalations",   null);

        if (!isParticipant && !isStaff)
            return new ErrorResult("Bu konuşmayı kapatma yetkiniz yok.");

        if (conv.Status == "closed")
            return new ErrorResult("Konuşma zaten kapalı.");

        conv.Status = "closed";
        _conversationDal.Update(conv);
        return new SuccessResult("Konuşma kapatıldı.");
    }

    public IResult DeleteConversation(int conversationId, int callerUserId)
    {
        bool canDelete = _resolver.Allows(callerUserId, "chat.institution_manage", null)
                      || _resolver.Allows(callerUserId, "chat.global_manage", null);
        if (!canDelete)
            return new ErrorResult("Konuşma silmek için yetki gerekiyor.");

        var conv = _conversationDal.Get(c => c.Id == conversationId);
        if (conv == null)
            return new ErrorResult("Konuşma bulunamadı.");

        // Mesajları sil
        var msgs = _messageDal.GetAll(m => m.ConversationId == conversationId);
        foreach (var m in msgs) _messageDal.Delete(m);

        // Katılımcıları sil
        var parts = _participantDal.GetAll(p => p.ConversationId == conversationId);
        foreach (var p in parts) _participantDal.Delete(p);

        _conversationDal.Delete(conv);
        return new SuccessResult("Konuşma silindi.");
    }

    public IResult UpdateTitle(int conversationId, int callerUserId, string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            return new ErrorResult("Başlık boş olamaz.");

        var conv = _conversationDal.Get(c => c.Id == conversationId);
        if (conv == null)
            return new ErrorResult("Konuşma bulunamadı.");

        if (!IsAdminOfConversation(conversationId, callerUserId)
            && !_resolver.Allows(callerUserId, "chat.institution_manage", null))
            return new ErrorResult("Başlık değiştirme yetkiniz yok.");

        conv.Title = newTitle.Trim();
        _conversationDal.Update(conv);
        return new SuccessResult("Başlık güncellendi.");
    }

    public List<int> GetParticipantUserIds(int conversationId)
        => _participantDal.GetAll(p => p.ConversationId == conversationId).Select(p => p.UserId).ToList();

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private bool IsParticipant(int conversationId, int userId)
        => _participantDal.Get(p => p.ConversationId == conversationId && p.UserId == userId) != null;

    private bool IsAdminOfConversation(int conversationId, int userId)
    {
        var p = _participantDal.Get(p => p.ConversationId == conversationId && p.UserId == userId);
        return p?.Role == "admin";
    }

    private ConversationSummaryDto MapToSummary(Conversation conv, int participantCount)
    {
        string? assignedUsername = null;
        if (conv.AssignedToUserId.HasValue)
            assignedUsername = _userDal.Get(u => u.Id == conv.AssignedToUserId.Value)?.UserName;

        return new ConversationSummaryDto
        {
            Id               = conv.Id,
            Type             = conv.Type,
            Scope            = conv.Scope,
            InstitutionId    = conv.InstitutionId,
            Title            = conv.Title,
            CreatedByUserId  = conv.CreatedByUserId,
            CreatedAt        = conv.CreatedAt,
            ParticipantCount = participantCount,
            UnreadCount      = 0,
            Status           = conv.Status,
            SupportCategory  = conv.SupportCategory,
            AssignedToUserId = conv.AssignedToUserId,
            AssignedToUsername = assignedUsername,
        };
    }

    private ConversationDetailDto BuildDetail(Conversation conv)
    {
        var participants = _participantDal.GetAll(p => p.ConversationId == conv.Id);
        var participantDtos = participants.Select(p =>
        {
            var user = _userDal.Get(u => u.Id == p.UserId);
            return new ParticipantDto
            {
                UserId            = p.UserId,
                Username          = user?.UserName ?? string.Empty,
                Role              = p.Role,
                LastReadMessageId = p.LastReadMessageId,
                JoinedAt          = p.JoinedAt,
            };
        }).ToList();

        string? assignedUsername = null;
        if (conv.AssignedToUserId.HasValue)
            assignedUsername = _userDal.Get(u => u.Id == conv.AssignedToUserId.Value)?.UserName;

        return new ConversationDetailDto
        {
            Id                 = conv.Id,
            Type               = conv.Type,
            Scope              = conv.Scope,
            InstitutionId      = conv.InstitutionId,
            Title              = conv.Title,
            CreatedByUserId    = conv.CreatedByUserId,
            CreatedAt          = conv.CreatedAt,
            ParticipantCount   = participantDtos.Count,
            UnreadCount        = 0,
            Status             = conv.Status,
            SupportCategory    = conv.SupportCategory,
            AssignedToUserId   = conv.AssignedToUserId,
            AssignedToUsername = assignedUsername,
            Participants       = participantDtos,
        };
    }
}
