using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;

namespace WebAPI.Seeders;

public static class CapabilitySeeder
{
    // Code → GroupKey eşlemesi (idempotent update için de kullanılır)
    private static readonly Dictionary<string, string> GroupKeyMap =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // admin.system
        ["admin.system_access"]           = "admin.system",
        ["admin.system_monitor"]          = "admin.system",
        ["admin.security_monitor"]        = "admin.system",
        ["admin.system_settings_read"]    = "admin.system",
        ["admin.system_settings_write"]   = "admin.system",
        // admin.users
        ["admin.user_read"]               = "admin.users",
        ["admin.user_create"]             = "admin.users",
        ["admin.user_update"]             = "admin.users",
        ["admin.user_delete"]             = "admin.users",
        ["admin.user_ban"]                = "admin.users",
        ["admin.user_unban"]              = "admin.users",
        ["admin.user_role_change"]        = "admin.users",
        ["admin.user_impersonate"]        = "admin.users",
        ["admin.user_warning_read_all"]   = "admin.users",
        // admin.institutions
        ["admin.institution_read"]             = "admin.institutions",
        ["admin.institution_create"]           = "admin.institutions",
        ["admin.institution_update"]           = "admin.institutions",
        ["admin.institution_deactivate"]       = "admin.institutions",
        ["admin.institution_feature_write"]    = "admin.institutions",
        // admin.audit
        ["admin.audit_read"]              = "admin.audit",
        ["admin.audit_export"]            = "admin.audit",
        ["admin.capability_audit_read"]   = "admin.audit",
        // admin.features
        ["admin.feature_group_manage"]       = "admin.features",
        ["admin.feature_definition_manage"]  = "admin.features",
        // admin.content
        ["admin.legal_agreement_manage"]  = "admin.content",
        ["admin.email_template_manage"]   = "admin.content",
        ["admin.about_page_manage"]       = "admin.content",
        ["admin.feedback_read"]           = "admin.content",
        // admin.workflow
        ["admin.rule_read"]                    = "admin.workflow",
        ["admin.rule_create"]                  = "admin.workflow",
        ["admin.rule_update"]                  = "admin.workflow",
        ["admin.rule_delete"]                  = "admin.workflow",
        ["admin.rule_activate"]                = "admin.workflow",
        ["admin.rule_test_run"]                = "admin.workflow",
        ["admin.workflow_log_read"]            = "admin.workflow",
        ["admin.workflow_reference_manage"]    = "admin.workflow",
        // admin.capabilities
        ["admin.capability_catalog_read"]      = "admin.capabilities",
        ["admin.capability_catalog_write"]     = "admin.capabilities",
        ["admin.capability_grant"]             = "admin.capabilities",
        ["admin.capability_revoke"]            = "admin.capabilities",
        ["admin.capability_template_create"]   = "admin.capabilities",
        ["admin.capability_template_publish"]  = "admin.capabilities",
        ["admin.capability_template_apply"]    = "admin.capabilities",
        // admin.announcements
        ["admin.announcement_read"]    = "admin.announcements",
        ["admin.announcement_create"]  = "admin.announcements",
        ["admin.announcement_delete"]  = "admin.announcements",
        // admin.tenancy
        ["admin.cross_institution_read"]    = "admin.tenancy",
        ["admin.user_institution_change"]   = "admin.tenancy",
        // admin.killswitch
        ["admin.killswitch_read"]       = "admin.killswitch",
        ["admin.killswitch_soft"]       = "admin.killswitch",
        ["admin.killswitch_hard"]       = "admin.killswitch",
        ["admin.killswitch_emergency"]  = "admin.killswitch",
        // admin.metrics
        ["admin.dashboard_view"]               = "admin.metrics",
        ["admin.metrics_capability_view"]      = "admin.metrics",
        ["admin.metrics_workflow_view"]        = "admin.metrics",
        ["admin.metrics_user_view"]            = "admin.metrics",
        ["admin.metrics_system_health_view"]   = "admin.metrics",
        ["admin.metrics_export"]               = "admin.metrics",
        // moderation
        ["moderation.content_review"]          = "moderation.content",
        ["moderation.problem_moderate"]        = "moderation.problems",
        ["moderation.problem_delete"]          = "moderation.problems",
        ["moderation.problem_highlight"]       = "moderation.problems",
        ["moderation.problem_resolve"]         = "moderation.problems",
        ["moderation.problem_close"]           = "moderation.problems",
        ["moderation.problem_reopen"]          = "moderation.problems",
        ["moderation.problem_hide"]            = "moderation.problems",
        ["moderation.solution_moderate"]       = "moderation.solutions",
        ["moderation.solution_delete"]         = "moderation.solutions",
        ["moderation.solution_highlight"]      = "moderation.solutions",
        ["moderation.comment_moderate"]        = "moderation.comments",
        ["moderation.comment_delete"]          = "moderation.comments",
        ["moderation.user_warn"]               = "moderation.users",
        ["moderation.user_warn_revoke"]        = "moderation.users",
        ["moderation.user_warn_read"]          = "moderation.users",
        ["moderation.report_review"]           = "moderation.reports",
        ["moderation.report_resolve"]          = "moderation.reports",
        ["moderation.topic_create"]            = "moderation.topics",
        ["moderation.topic_update"]            = "moderation.topics",
        ["moderation.topic_delete"]            = "moderation.topics",
        ["moderation.bulk_notification_send"]  = "moderation.notifications",
        ["moderation.audit_view_institution"]  = "moderation.audit",
        // expert
        ["expert.solution_approve"]       = "expert.solutions",
        ["expert.solution_reject"]        = "expert.solutions",
        ["expert.solution_quality_score"] = "expert.solutions",
        ["expert.problem_quality_score"]  = "expert.problems",
        ["expert.problem_difficulty_set"] = "expert.problems",
        ["expert.content_highlight"]      = "expert.content",
        ["expert.badge_assign"]           = "expert.badges",
        ["expert.badge_revoke"]           = "expert.badges",
        ["expert.workflow_test_run"]      = "expert.workflow",
        ["expert.csharp_execute"]         = "expert.csharp",
        // user.problems
        ["user.problem_create"]      = "user.problems",
        ["user.problem_update_own"]  = "user.problems",
        ["user.problem_delete_own"]  = "user.problems",
        // user.solutions
        ["user.solution_create"]      = "user.solutions",
        ["user.solution_update_own"]  = "user.solutions",
        ["user.solution_delete_own"]  = "user.solutions",
        // user.comments
        ["user.comment_create"]      = "user.comments",
        ["user.comment_update_own"]  = "user.comments",
        ["user.comment_delete_own"]  = "user.comments",
        // user.voting
        ["user.problem_upvote"]   = "user.voting",
        ["user.problem_unvote"]   = "user.voting",
        ["user.solution_upvote"]  = "user.voting",
        ["user.solution_downvote"] = "user.voting",
        ["user.vote_retract"]     = "user.voting",
        // user.social
        ["user.problem_follow"]  = "user.social",
        ["user.topic_follow"]    = "user.social",
        ["user.solution_save"]   = "user.social",
        ["user.content_share"]   = "user.social",
        ["user.mention_user"]    = "user.social",
        // user.reporting
        ["user.content_report"]  = "user.reporting",
        ["user.user_report"]     = "user.reporting",
        // user.profile
        ["user.profile_update"]          = "user.profile",
        ["user.profile_avatar_change"]   = "user.profile",
        ["user.profile_email_change"]    = "user.profile",
        ["user.profile_password_change"] = "user.profile",
        ["user.profile_username_change"] = "user.profile",
        // user.account
        ["user.content_export"]       = "user.account",
        ["user.account_delete"]       = "user.account",
        ["user.notifications_manage"] = "user.account",
        ["user.feedback_send"]        = "user.account",
        // admin.titles
        ["admin.user_title_assign"]   = "admin.titles",
        ["admin.user_title_read"]     = "admin.titles",
        // official
        ["official.response_create"]  = "official",
        ["official.response_update"]  = "official",
        // chat
        ["chat.use"]                    = "chat",
        ["chat.institution_manage"]     = "chat",
        ["chat.global_manage"]          = "chat",
        ["chat.contact_admin"]          = "chat",
        ["chat.official_channel"]       = "chat",
        ["chat.support_request"]        = "chat",
        ["chat.escalate"]               = "chat",
        ["chat.contact_global_admin"]   = "chat",
        ["chat.handle_support"]         = "chat",
        ["chat.handle_escalations"]     = "chat",
    };

    private static string ResolveGroupKey(string code)
    {
        if (GroupKeyMap.TryGetValue(code, out var mapped)) return mapped;
        if (code.StartsWith("workflow.action.", StringComparison.OrdinalIgnoreCase)) return "workflow.action";
        if (code.StartsWith("page.admin.", StringComparison.OrdinalIgnoreCase)) return "page.admin";
        return code.Split('.')[0]; // fallback: ilk segment
    }

    public static void Seed(DevelopTurkeyContext context)
    {
        var definitions = new (string Code, string Description, string Category)[]
        {
            // ══════════════════════════════════════════════════════════════════
            // ADMIN — Sistem yönetimi (global scope, sadece SUPER_ADMIN/ADMIN)
            // ══════════════════════════════════════════════════════════════════

            ("admin.system_access",          "Yönetim paneline erişim sağlar. Bu yetki olmadan admin panelindeki hiçbir sayfaya girilemez.", "admin"),
            ("admin.system_monitor",         "Sistem sağlık metrikleri, sunucu uptime ve kaynak kullanımı (CPU/RAM) gibi teknik izleme verilerini görüntüleyebilirsiniz.", "admin"),
            ("admin.security_monitor",       "Başarısız giriş denemeleri, rate-limit aşımı ve yetkisiz erişim (403) gibi güvenlik olaylarını izleyebilirsiniz.", "admin"),
            ("admin.system_settings_read",   "Sistem genelindeki ayarları (site adı, dil, zaman dilimi vb.) okuyabilirsiniz.", "admin"),
            ("admin.system_settings_write",  "Sistem genelindeki ayarları güncelleyebilirsiniz.", "admin"),

            ("admin.user_read",              "Tüm kullanıcıları, kurum ayrımı gözetmeksizin listeleme ve profillerini görüntüleyebilirsiniz.", "admin"),
            ("admin.user_create",            "Yönetim panelinden manuel olarak yeni kullanıcı hesabı oluşturabilirsiniz.", "admin"),
            ("admin.user_update",            "Kullanıcının adını, e-postasını veya kurumunu güncelleyebilirsiniz.", "admin"),
            ("admin.user_delete",            "Bir kullanıcı hesabını kalıcı olarak silebilirsiniz. Bu işlem geri alınamaz.", "admin"),
            ("admin.user_ban",               "Kullanıcıyı platforma erişimden kısıtlayabilirsiniz (süreli veya kalıcı).", "admin"),
            ("admin.user_unban",             "Önceden kısıtlanmış bir kullanıcının erişimini yeniden açabilirsiniz.", "admin"),
            ("admin.user_role_change",       "Kullanıcıya eski rol bayraklarını (IsAdmin/IsExpert/IsOfficial) atayabilirsiniz. Bu yetki eski sistemden geriye dönük uyumluluk için mevcuttur; yeni projelerde kullanılmamalıdır.", "admin"),
            ("admin.user_impersonate",       "Başka bir kullanıcı kimliğine geçerek (sudo) platformu onun gözünden görebilirsiniz. Bu eylem güvenlik loguna kaydedilir.", "admin"),
            ("admin.user_warning_read_all",  "Tüm kullanıcılara verilmiş uyarıları okuyabilirsiniz (kurum sınırlaması olmadan).", "admin"),

            ("admin.institution_read",          "Tüm kurumları listeleyebilir ve detaylarını görüntüleyebilirsiniz.", "admin"),
            ("admin.institution_create",        "Yeni bir kurum oluşturabilirsiniz.", "admin"),
            ("admin.institution_update",        "Kurum adı, logosu ve iletişim bilgileri gibi alanları güncelleyebilirsiniz.", "admin"),
            ("admin.institution_deactivate",    "Bir kurumu pasif hale getirebilirsiniz. Pasif kurumların kullanıcıları platforma erişemez.", "admin"),
            ("admin.institution_feature_write", "Kuruma özel özellik (feature) değerlerini ayarlayabilirsiniz. Örneğin, belirli bir kurumun sohbet modülünü açıp kapayabilirsiniz.", "admin"),

            ("admin.audit_read",        "Sistemdeki tüm AuditLog kayıtlarını görüntüleyebilirsiniz.", "admin"),
            ("admin.audit_export",      "Audit log verilerini CSV veya JSON formatında dışa aktarabilirsiniz.", "admin"),
            ("admin.capability_audit_read", "Yetki (capability) grant/revoke işlemlerine ait denetim kayıtlarını okuyabilirsiniz.", "admin"),

            ("admin.feature_group_manage",      "Platform genelindeki özellik gruplarını (FeatureGroup) oluşturabilir ve düzenleyebilirsiniz.", "admin"),
            ("admin.feature_definition_manage", "Özellik tanımlarını (FeatureDefinition) oluşturabilir ve düzenleyebilirsiniz.", "admin"),
            ("admin.legal_agreement_manage",    "KVKK, gizlilik politikası ve kullanım koşullarını oluşturabilir, güncelleyebilir ve yayımlayabilirsiniz.", "admin"),
            ("admin.email_template_manage",     "Sistemin gönderdiği otomatik e-postaların (doğrulama, bildirim vb.) şablonlarını düzenleyebilirsiniz.", "admin"),
            ("admin.about_page_manage",         "'Hakkımızda' sayfasının içeriğini (AboutPageSection) düzenleyebilirsiniz.", "admin"),
            ("admin.feedback_read",             "Kullanıcıların sisteme gönderdiği geri bildirimleri okuyabilirsiniz.", "admin"),

            ("admin.rule_read",             "Dinamik kural (DynamicRule / Workflow) listesini ve detaylarını görüntüleyebilirsiniz.", "admin"),
            ("admin.rule_create",           "Yeni dinamik kural oluşturabilirsiniz.", "admin"),
            ("admin.rule_update",           "Mevcut bir kuralın tanımını ve akış şemasını düzenleyebilirsiniz.", "admin"),
            ("admin.rule_delete",           "Bir dinamik kuralı silebilirsiniz.", "admin"),
            ("admin.rule_activate",         "Bir kuralı aktif veya pasif hale getirebilirsiniz.", "admin"),
            ("admin.rule_test_run",         "Bir kuralı gerçek veri etkilemeden deneme (dry-run) modunda çalıştırabilirsiniz.", "admin"),
            ("admin.workflow_log_read",     "Workflow çalıştırma geçmişini ve sonuçlarını görüntüleyebilirsiniz.", "admin"),
            ("admin.workflow_reference_manage", "Workflow tetikleyicileri, alanları ve action kataloğunu yönetebilirsiniz.", "admin"),

            ("admin.capability_catalog_read",    "Sistemdeki tüm yetki (capability) listesini okuyabilirsiniz.", "admin"),
            ("admin.capability_catalog_write",   "Yeni özel yetki oluşturabilir ve mevcut yetkiyi düzenleyebilirsiniz.", "admin"),
            ("admin.capability_grant",           "Bir kullanıcıya yetki (capability) verebilirsiniz.", "admin"),
            ("admin.capability_revoke",          "Bir kullanıcıdan yetki kaldırabilirsiniz.", "admin"),
            ("admin.capability_template_create", "Yeni bir yetki şablonu oluşturabilirsiniz.", "admin"),
            ("admin.capability_template_publish","Bir yetki şablonunun yeni versiyonunu yayımlayabilirsiniz.", "admin"),
            ("admin.capability_template_apply",  "Bir yetki şablonunu bir veya birden fazla kullanıcıya uygulayabilirsiniz.", "admin"),

            ("admin.announcement_read",   "Admin panelindeki duyuruları görüntüleyebilirsiniz.", "admin"),
            ("admin.announcement_create", "Yeni duyuru oluşturabilirsiniz.", "admin"),
            ("admin.announcement_delete", "Mevcut bir duyuruyu silebilirsiniz.", "admin"),

            ("admin.cross_institution_read",  "Tüm kurumların verilerini kurum filtresi olmadan okuyabilirsiniz (global admin için).", "admin"),
            ("admin.user_institution_change", "Bir kullanıcının bağlı olduğu kurumu değiştirebilirsiniz.", "admin"),

            ("admin.killswitch_read",      "Kill switch'in mevcut durumunu (aktif/pasif, mod) okuyabilirsiniz.", "admin"),
            ("admin.killswitch_soft",      "Soft Kill'i etkinleştirebilirsiniz: yeni workflow çalıştırma istekleri reddedilir, devam edenler tamamlanır.", "admin"),
            ("admin.killswitch_hard",      "Hard Kill'i etkinleştirebilirsiniz: tüm worker'lar durdurulur, yeni iş akışı kabul edilmez.", "admin"),
            ("admin.killswitch_emergency", "Emergency Kill'i etkinleştirebilirsiniz: tüm workflow altyapısı anında dondurulur. Bu yetki yalnızca Süper Admin'e verilmelidir.", "admin"),

            ("admin.dashboard_view",             "Admin paneli ana dashboard (Genel Bakış) sayfasına erişebilirsiniz.", "admin"),
            ("admin.metrics_capability_view",    "Yetki (capability) sistemi metriklerini görüntüleyebilirsiniz.", "admin"),
            ("admin.metrics_workflow_view",      "Workflow çalıştırma istatistiklerini görüntüleyebilirsiniz.", "admin"),
            ("admin.metrics_user_view",          "Kullanıcı aktivite metriklerini görüntüleyebilirsiniz.", "admin"),
            ("admin.metrics_system_health_view", "CPU, bellek, veritabanı bağlantısı gibi sistem sağlık göstergelerini görüntüleyebilirsiniz.", "admin"),
            ("admin.metrics_export",             "Metrik verilerini CSV veya JSON formatında dışa aktarabilirsiniz.", "admin"),

            // ══════════════════════════════════════════════════════════════════
            // MODERATION — Kurum moderatörü (institution scope)
            // ══════════════════════════════════════════════════════════════════

            ("moderation.content_review",      "Bekleyen veya raporlanan içerikleri inceleyebilir ve değerlendirme yapabilirsiniz.", "moderation"),
            ("moderation.problem_moderate",    "Sorunları düzenleyebilir, taşıyabilir veya etiketleyebilirsiniz.", "moderation"),
            ("moderation.problem_delete",      "Kurallara aykırı veya yanıltıcı sorun bildirimlerini silebilirsiniz.", "moderation"),
            ("moderation.problem_highlight",   "Önemli bir sorunu ana sayfada veya listede öne çıkarabilirsiniz.", "moderation"),
            ("moderation.problem_resolve",     "Bir sorunu 'Çözüldü' olarak işaretleyebilirsiniz.", "moderation"),
            ("moderation.problem_close",       "Bir sorunu kapatabilirsiniz. Kapalı sorunlara yeni çözüm veya yorum eklenemez.", "moderation"),
            ("moderation.problem_reopen",      "Kapatılmış bir sorunu yeniden açabilirsiniz.", "moderation"),
            ("moderation.problem_hide",        "Bir sorunu herkese görünmez yapabilirsiniz. Gizlenen sorun listelerden kaybolur ancak admin panelinde görünür kalır.", "moderation"),
            ("moderation.solution_moderate",   "Çözümleri düzenleyebilir veya etiketleyebilirsiniz.", "moderation"),
            ("moderation.solution_delete",     "Kurallara aykırı çözümleri silebilirsiniz.", "moderation"),
            ("moderation.solution_highlight",  "İyi bir çözümü listede öne çıkarabilirsiniz.", "moderation"),
            ("moderation.comment_moderate",    "Yorumları düzenleyebilir veya gizleyebilirsiniz.", "moderation"),
            ("moderation.comment_delete",      "Uygunsuz yorumları silebilirsiniz.", "moderation"),
            ("moderation.user_warn",           "Bir kullanıcıya uyarı verebilirsiniz.", "moderation"),
            ("moderation.user_warn_revoke",    "Daha önce verilmiş bir kullanıcı uyarısını geri alabilirsiniz.", "moderation"),
            ("moderation.user_warn_read",      "Kurumunuzdaki kullanıcıların uyarı geçmişini okuyabilirsiniz.", "moderation"),
            ("moderation.report_review",       "Kullanıcıların raporladığı içerikleri inceleyebilirsiniz.", "moderation"),
            ("moderation.report_resolve",      "Raporu karara bağlayabilirsiniz (kabul/ret).", "moderation"),
            ("moderation.topic_create",        "Yeni bir konu (topic/kategori) oluşturabilirsiniz.", "moderation"),
            ("moderation.topic_update",        "Mevcut bir konuyu düzenleyebilirsiniz.", "moderation"),
            ("moderation.topic_delete",        "Bir konuyu silebilirsiniz.", "moderation"),
            ("moderation.bulk_notification_send", "Kurumunuzdaki tüm kullanıcılara veya belirli bir gruba toplu bildirim gönderebilirsiniz.", "moderation"),
            ("moderation.audit_view_institution", "Kurumunuza ait audit log kayıtlarını görüntüleyebilirsiniz.", "moderation"),

            // ══════════════════════════════════════════════════════════════════
            // EXPERT — Çözüm onaylayıcı uzman
            // ══════════════════════════════════════════════════════════════════

            ("expert.solution_approve",       "Bir çözümün geçerli ve kaliteli olduğunu onaylayabilirsiniz. Onaylanan çözüm, ilgili sorunu otomatik olarak 'Çözüldü' olarak işaretler.", "expert"),
            ("expert.solution_reject",        "Kalitesiz veya yanlış bir çözümü reddedebilirsiniz.", "expert"),
            ("expert.solution_quality_score", "Bir çözüme 1-5 arası kalite puanı verebilirsiniz.", "expert"),
            ("expert.problem_quality_score",  "Bir sorunun netliğini ve kalitesini değerlendirerek puan verebilirsiniz.", "expert"),
            ("expert.problem_difficulty_set", "Bir sorunun çözüm zorluğunu belirleyebilirsiniz.", "expert"),
            ("expert.content_highlight",      "Kendi alanınızdaki iyi içeriklere uzman vurgusu ekleyebilirsiniz.", "expert"),
            ("expert.badge_assign",           "Katkıda bulunan kullanıcılara alan bazlı rozet verebilirsiniz.", "expert"),
            ("expert.badge_revoke",           "Daha önce verdiğiniz rozeti geri alabilirsiniz.", "expert"),
            ("expert.workflow_test_run",      "Workflow kurallarını deneme (dry-run) modunda çalıştırabilirsiniz (admin değil, teknik operatör için).", "expert"),
            ("expert.csharp_execute",         "Workflow içindeki C# (Roslyn) node'larını çalıştırabilirsiniz. Güvenli sandbox ortamında çalışır.", "expert"),

            // ══════════════════════════════════════════════════════════════════
            // USER — Standart kullanıcı (self scope, tüm yeni kayıtlara verilir)
            // ══════════════════════════════════════════════════════════════════

            ("user.problem_create",       "Platforma yeni bir sorun bildirimi ekleyebilirsiniz.", "user"),
            ("user.problem_update_own",   "Kendi oluşturduğunuz bir sorunu düzenleyebilirsiniz.", "user"),
            ("user.problem_delete_own",   "Kendi oluşturduğunuz bir sorunu silebilirsiniz.", "user"),
            ("user.solution_create",      "Platforma yeni bir çözüm önerisi paylaşabilirsiniz.", "user"),
            ("user.solution_update_own",  "Kendi paylaştığınız çözümü düzenleyebilirsiniz.", "user"),
            ("user.solution_delete_own",  "Kendi paylaştığınız çözümü silebilirsiniz.", "user"),
            ("user.comment_create",       "Bir sorun veya çözüme yorum yazabilirsiniz.", "user"),
            ("user.comment_update_own",   "Kendi yazdığınız yorumu düzenleyebilirsiniz.", "user"),
            ("user.comment_delete_own",   "Kendi yazdığınız yorumu silebilirsiniz.", "user"),

            ("user.problem_upvote",    "'Ben de yaşıyorum' diyerek bir sorunu destekleyebilirsiniz.", "user"),
            ("user.problem_unvote",    "Bir soruna verdiğiniz desteği geri çekebilirsiniz.", "user"),
            ("user.solution_upvote",   "Bir çözüme olumlu oy verebilirsiniz.", "user"),
            ("user.solution_downvote", "Bir çözüme olumsuz oy verebilirsiniz.", "user"),
            ("user.vote_retract",      "Verdiğiniz bir oyu geri alabilirsiniz.", "user"),

            ("user.problem_follow",  "Bir sorunu takip edebilir ve güncellemelerden haberdar olabilirsiniz.", "user"),
            ("user.topic_follow",    "Bir konuyu (topic) takip edebilirsiniz.", "user"),
            ("user.solution_save",   "Beğendiğiniz bir çözümü kaydedilen listenize ekleyebilirsiniz.", "user"),
            ("user.content_share",   "Bir içeriği WhatsApp, X (Twitter) veya diğer platformlarda paylaşabilirsiniz.", "user"),
            ("user.mention_user",    "Yorumda '@' ile başka bir kullanıcıyı etiketleyebilirsiniz.", "user"),

            ("user.content_report",  "Kurallara aykırı gördüğünüz bir içeriği (sorun/çözüm/yorum) raporlayabilirsiniz.", "user"),
            ("user.user_report",     "Uygunsuz davranış sergileyen bir kullanıcıyı raporlayabilirsiniz.", "user"),

            ("user.profile_update",          "Profil adınızı ve diğer genel bilgilerinizi güncelleyebilirsiniz.", "user"),
            ("user.profile_avatar_change",   "Profil fotoğrafınızı değiştirebilirsiniz.", "user"),
            ("user.profile_email_change",    "E-posta adresinizi değiştirebilirsiniz. Değişiklik e-posta doğrulaması gerektirir.", "user"),
            ("user.profile_password_change", "Şifrenizi değiştirebilirsiniz.", "user"),
            ("user.profile_username_change", "Kullanıcı adınızı (username) değiştirebilirsiniz.", "user"),

            ("user.content_export",       "Kendi oluşturduğunuz içerikleri ve hesap verilerinizi dışa aktarabilirsiniz (KVKK veri portabilitesi).", "user"),
            ("user.account_delete",       "Kendi hesabınızı kalıcı olarak silebilirsiniz.", "user"),
            ("user.notifications_manage", "Hangi tür bildirimlerin size gönderileceğini yönetebilirsiniz.", "user"),
            ("user.feedback_send",        "Platform hakkında geri bildirim gönderebilirsiniz.", "user"),

            // ══════════════════════════════════════════════════════════════════
            // WORKFLOW ACTION GATING — her workflow action için ayrı kapı
            // ══════════════════════════════════════════════════════════════════

            ("workflow.action.send_email",             "Otomasyon kuralı içinde kullanıcıya e-posta gönderebilirsiniz.", "workflow.action"),
            ("workflow.action.send_notification",      "Otomasyon kuralı içinde tek bir kullanıcıya anlık bildirim gönderebilirsiniz.", "workflow.action"),
            ("workflow.action.send_bulk_notification", "Otomasyon kuralı içinde birden fazla kullanıcıya aynı anda toplu bildirim gönderebilirsiniz.", "workflow.action"),
            ("workflow.action.ban_user",               "Otomasyon kuralı içinde belirli bir koşul gerçekleştiğinde kullanıcıyı otomatik kısıtlayabilirsiniz.", "workflow.action"),
            ("workflow.action.unban_user",             "Otomasyon kuralı içinde bir kullanıcının kısıtlamasını otomatik kaldırabilirsiniz.", "workflow.action"),
            ("workflow.action.warn_user",              "Otomasyon kuralı içinde kullanıcıya otomatik uyarı verebilirsiniz.", "workflow.action"),
            ("workflow.action.grant_capability",           "Otomasyon kuralı içinde bir kullanıcıya belirtilen yetki (capability) kodunu verebilirsiniz.", "workflow.action"),
            ("workflow.action.apply_capability_template",  "Otomasyon kuralı içinde bir kullanıcıya yetki şablonu uygulayabilirsiniz.", "workflow.action"),
            ("workflow.action.resolve_problem",            "Otomasyon kuralı içinde bir sorunu 'Çözüldü' olarak işaretleyebilirsiniz.", "workflow.action"),
            ("workflow.action.highlight_problem",          "Otomasyon kuralı içinde bir sorunu öne çıkarabilirsiniz.", "workflow.action"),
            ("workflow.action.delete_problem",             "Otomasyon kuralı içinde bir sorunu silebilirsiniz.", "workflow.action"),
            ("workflow.action.report_problem",             "Otomasyon kuralı içinde bir sorunu otomatik raporlayabilirsiniz.", "workflow.action"),
            ("workflow.action.assign_problem_institution", "Otomasyon kuralı içinde bir sorunu farklı bir kuruma atayabilirsiniz.", "workflow.action"),
            ("workflow.action.change_problem_status",      "Otomasyon kuralı içinde bir sorunun durumunu değiştirebilirsiniz.", "workflow.action"),
            ("workflow.action.approve_solution",           "Otomasyon kuralı içinde bir çözümü otomatik onaylayabilirsiniz.", "workflow.action"),
            ("workflow.action.reject_solution",            "Otomasyon kuralı içinde bir çözümü otomatik reddedebilirsiniz.", "workflow.action"),
            ("workflow.action.highlight_solution",         "Otomasyon kuralı içinde bir çözümü öne çıkarabilirsiniz.", "workflow.action"),
            ("workflow.action.delete_solution",            "Otomasyon kuralı içinde bir çözümü silebilirsiniz.", "workflow.action"),
            ("workflow.action.delete_comment",             "Otomasyon kuralı içinde bir yorumu silebilirsiniz.", "workflow.action"),
            ("workflow.action.log_event",                  "Otomasyon kuralı içinde sisteme özel bir log kaydı yazabilirsiniz.", "workflow.action"),
            ("workflow.action.webhook",                    "Otomasyon kuralı içinde dışarıdaki bir sisteme HTTP webhook isteği gönderebilirsiniz.", "workflow.action"),
            ("workflow.action.create_announcement",        "Otomasyon kuralı içinde yeni bir duyuru oluşturabilirsiniz.", "workflow.action"),
            ("workflow.action.trigger_workflow",           "Otomasyon kuralı içinde başka bir workflow kuralını tetikleyebilirsiniz.", "workflow.action"),
            ("workflow.action.send_chat_message",          "Otomasyon kuralı içinde sohbet kanalına mesaj gönderebilirsiniz.", "workflow.action"),

            // ══════════════════════════════════════════════════════════════════
            // PAGE — Admin panel sekme görünürlüğü (PageScope = "Page")
            // ══════════════════════════════════════════════════════════════════

            ("page.admin.monitor",              "Admin panelinde Canlı Radar (Command Center) sayfasını görebilirsiniz.", "page"),
            ("page.admin.overview",             "Admin panelinde Genel Bakış (Dashboard) sayfasını görebilirsiniz.", "page"),
            ("page.admin.problems",             "Admin panelinde Sorunlar moderasyon sayfasını görebilirsiniz.", "page"),
            ("page.admin.solutions",            "Admin panelinde Çözümler moderasyon sayfasını görebilirsiniz.", "page"),
            ("page.admin.topics",               "Admin panelinde Kategoriler (Topics) sayfasını görebilirsiniz.", "page"),
            ("page.admin.expert_approvals",     "Admin panelinde Uzman Onayları sayfasını görebilirsiniz.", "page"),
            ("page.admin.users",                "Admin panelinde Kullanıcılar sayfasını görebilirsiniz.", "page"),
            ("page.admin.institutions",         "Admin panelinde Kurumlar sayfasını görebilirsiniz.", "page"),
            ("page.admin.reports",              "Admin panelinde Şikayet Merkezi sayfasını görebilirsiniz.", "page"),
            ("page.admin.feedbacks",            "Admin panelinde Gelen Kutusu (Geri Bildirimler) sayfasını görebilirsiniz.", "page"),
            ("page.admin.security",             "Admin panelinde Güvenlik İzleme sayfasını görebilirsiniz.", "page"),
            ("page.admin.settings",             "Admin panelinde Sistem Ayarları sayfasını görebilirsiniz.", "page"),
            ("page.admin.announcements",        "Admin panelinde Duyurular sayfasını görebilirsiniz.", "page"),
            ("page.admin.logs",                 "Admin panelinde Sistem Logları sayfasını görebilirsiniz.", "page"),
            ("page.admin.activity_logs",        "Admin panelinde Aksiyon Geçmişi sayfasını görebilirsiniz.", "page"),
            ("page.admin.kill_switch",          "Admin panelinde Kill Switch (Acil Durdurma) sayfasını görebilirsiniz.", "page"),
            ("page.admin.agreements",           "Admin panelinde Sözleşmeler (KVKK, gizlilik vb.) sayfasını görebilirsiniz.", "page"),
            ("page.admin.email_templates",      "Admin panelinde E-posta Şablonları sayfasını görebilirsiniz.", "page"),
            ("page.admin.corporate",            "Admin panelinde Hakkımızda sayfasını görebilirsiniz.", "page"),
            ("page.admin.features",             "Admin panelinde Modül Yönetimi sayfasını görebilirsiniz.", "page"),
            ("page.admin.workflow",             "Admin panelinde Workflow Builder sayfasını görebilirsiniz.", "page"),
            ("page.admin.capabilities",         "Admin panelinde Yetki Yönetimi sayfasını görebilirsiniz.", "page"),
            ("page.admin.capability_templates", "Admin panelinde Yetki Şablonları sayfasını görebilirsiniz.", "page"),
            ("page.admin.capability_audit",     "Admin panelinde Yetki Logları sayfasını görebilirsiniz.", "page"),
            ("page.admin.metrics",              "Admin panelinde Metrikler sayfasını görebilirsiniz.", "page"),

            // ══════════════════════════════════════════════════════════════════
            // ADMIN TITLES — Kullanıcı unvan yönetimi
            // ══════════════════════════════════════════════════════════════════

            ("admin.user_title_assign", "Kullanıcıya unvan (rozet) atayabilir ve mevcut unvanı kaldırabilirsiniz.", "admin"),
            ("admin.user_title_read",   "Kullanıcılara atanmış unvanları listeleyebilirsiniz.", "admin"),

            // ══════════════════════════════════════════════════════════════════
            // OFFICIAL — Kurum yetkilisi yanıtları
            // ══════════════════════════════════════════════════════════════════

            ("official.response_create", "Kurum adına bir soruna resmi yanıt yazabilirsiniz.", "official"),
            ("official.response_update", "Daha önce yazdığınız resmi yanıtın içeriğini veya durumunu (inceleniyor/bilgi verildi/kapatıldı) güncelleyebilirsiniz.", "official"),

            // ══════════════════════════════════════════════════════════════════
            // CHAT — Sohbet Sistemi (Communication.EnableChat feature ile korumalı)
            // ══════════════════════════════════════════════════════════════════

            ("chat.use",                  "Sohbet özelliğine erişebilir, konuşma listesini görebilir ve mesaj gönderebilirsiniz.", "chat"),
            ("chat.institution_manage",   "Kurum içi grup konuşmaları oluşturabilir ve katılımcıları yönetebilirsiniz.", "chat"),
            ("chat.global_manage",        "Kurumlar arası (global) konuşmalar oluşturabilir ve yönetebilirsiniz.", "chat"),
            ("chat.contact_admin",        "Moderatör → admin kanalına doğrudan destek talebi açabilirsiniz.", "chat"),
            ("chat.official_channel",     "Resmi / uzman kanalı olarak kullanıcılardan gelen mesajları kabul edebilirsiniz.", "chat"),
            ("chat.support_request",      "Genel destek talebi açabilirsiniz. Bu yetki tüm kayıtlı kullanıcılara verilir.", "chat"),
            ("chat.escalate",             "Moderatör kanalına tırmanma (escalation) talebi açabilirsiniz. Uzmanlar için önerilir.", "chat"),
            ("chat.contact_global_admin", "Global admin kanalına doğrudan destek talebi açabilirsiniz. Kurum adminleri için önerilir.", "chat"),
            ("chat.handle_support",       "Genel destek taleplerini (support) görüntüleyip sahiplenebilirsiniz.", "chat"),
            ("chat.handle_escalations",   "Uzman kanalına gelen tırmanma taleplerini görüntüleyip sahiplenebilirsiniz.", "chat"),

            // ══════════════════════════════════════════════════════════════════
            // PAGE — Chat admin sekmesi
            // ══════════════════════════════════════════════════════════════════

            ("page.admin.chat", "Admin panelinde Sohbet Yönetimi sayfasını görebilirsiniz.", "page"),
        };

        // ── 1. Eksik capability'leri ekle ──────────────────────────────────────
        var existingCodes = context.Capabilities
            .Select(c => c.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addedAny = false;
        foreach (var (code, description, category) in definitions)
        {
            if (existingCodes.Contains(code))
                continue;

            var pageScope = code.StartsWith("page.", StringComparison.OrdinalIgnoreCase)
                ? "Page" : "Action";

            context.Capabilities.Add(new Capability
            {
                Code        = code,
                Description = description,
                Category    = category,
                GroupKey    = ResolveGroupKey(code),
                PageScope   = pageScope,
                IsSystem    = true,
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow,
            });
            addedAny = true;
        }

        if (addedAny)
            context.SaveChanges();

        // ── 2. GroupKey / PageScope / Description idempotent güncelle ─────────
        var definitionMap = definitions.ToDictionary(d => d.Code, d => d, StringComparer.OrdinalIgnoreCase);
        var allCaps = context.Capabilities.ToList();

        var updatedAny = false;
        foreach (var cap in allCaps)
        {
            var expectedGroupKey  = ResolveGroupKey(cap.Code);
            var expectedPageScope = cap.Code.StartsWith("page.", StringComparison.OrdinalIgnoreCase)
                ? "Page" : "Action";

            var changed = false;
            if (cap.GroupKey != expectedGroupKey)             { cap.GroupKey  = expectedGroupKey;  changed = true; }
            if (cap.PageScope != expectedPageScope)           { cap.PageScope = expectedPageScope; changed = true; }

            // Açıklama güncellemesi — DB'deki tanımsız veya eski kısa açıklamalar yenilenir
            if (definitionMap.TryGetValue(cap.Code, out var def) &&
                !string.IsNullOrWhiteSpace(def.Description) &&
                cap.Description != def.Description)
            {
                cap.Description = def.Description;
                changed = true;
            }

            if (changed) updatedAny = true;
        }

        if (updatedAny)
            context.SaveChanges();
    }
}
