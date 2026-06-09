using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;

namespace WebAPI.Seeders;

public static class CapabilityTemplateSeeder
{
    // ── Ortak capability kümeleri ────────────────────────────────────────────

    private static readonly string[] AllPageAdminCodes =
    [
        "page.admin.monitor", "page.admin.overview", "page.admin.problems", "page.admin.solutions",
        "page.admin.topics", "page.admin.expert_approvals", "page.admin.users", "page.admin.institutions",
        "page.admin.reports", "page.admin.feedbacks", "page.admin.security", "page.admin.settings",
        "page.admin.announcements", "page.admin.logs", "page.admin.activity_logs", "page.admin.kill_switch",
        "page.admin.agreements", "page.admin.email_templates", "page.admin.corporate",
        "page.admin.features", "page.admin.workflow",
        "page.admin.capabilities", "page.admin.capability_templates", "page.admin.capability_audit",
        "page.admin.metrics", "page.admin.chat",
    ];

    private static readonly string[] AllUserCodes =
    [
        "user.problem_create", "user.problem_update_own", "user.problem_delete_own",
        "user.solution_create", "user.solution_update_own", "user.solution_delete_own",
        "user.comment_create", "user.comment_update_own", "user.comment_delete_own",
        "user.problem_upvote", "user.problem_unvote", "user.solution_upvote", "user.solution_downvote",
        "user.vote_retract", "user.problem_follow", "user.topic_follow",
        "user.solution_save", "user.content_share", "user.mention_user",
        "user.content_report", "user.user_report",
        "user.profile_update", "user.profile_avatar_change", "user.profile_email_change",
        "user.profile_password_change", "user.profile_username_change",
        "user.content_export", "user.account_delete", "user.notifications_manage", "user.feedback_send",
    ];

    private static readonly string[] AllModerationCodes =
    [
        "moderation.content_review", "moderation.problem_moderate", "moderation.problem_delete",
        "moderation.problem_highlight", "moderation.problem_resolve",
        "moderation.problem_close", "moderation.problem_reopen", "moderation.problem_hide",
        "moderation.solution_moderate", "moderation.solution_delete", "moderation.solution_highlight",
        "moderation.comment_moderate", "moderation.comment_delete",
        "moderation.user_warn", "moderation.user_warn_revoke", "moderation.user_warn_read",
        "moderation.report_review", "moderation.report_resolve",
        "moderation.topic_create", "moderation.topic_update", "moderation.topic_delete",
        "moderation.bulk_notification_send", "moderation.audit_view_institution",
    ];

    private static readonly string[] AllWorkflowActionCodes =
    [
        "workflow.action.send_email", "workflow.action.send_notification", "workflow.action.send_bulk_notification",
        "workflow.action.ban_user", "workflow.action.unban_user", "workflow.action.warn_user",
        "workflow.action.grant_capability", "workflow.action.apply_capability_template",
        "workflow.action.resolve_problem", "workflow.action.highlight_problem", "workflow.action.delete_problem",
        "workflow.action.report_problem", "workflow.action.assign_problem_institution",
        "workflow.action.change_problem_status",
        "workflow.action.approve_solution", "workflow.action.reject_solution",
        "workflow.action.highlight_solution", "workflow.action.delete_solution",
        "workflow.action.delete_comment", "workflow.action.log_event", "workflow.action.webhook",
        "workflow.action.create_announcement", "workflow.action.trigger_workflow",
        "workflow.action.send_chat_message",
    ];

    // ── ROL ŞABLONLARI (Kind = 0) ────────────────────────────────────────────

    private static readonly string[] SuperAdminCodes =
    [
        // Admin — tümü (killswitch emergency dahil)
        "admin.system_access", "admin.system_monitor", "admin.security_monitor",
        "admin.system_settings_read", "admin.system_settings_write",
        "admin.user_read", "admin.user_create", "admin.user_update", "admin.user_delete",
        "admin.user_ban", "admin.user_unban", "admin.user_role_change", "admin.user_impersonate",
        "admin.user_warning_read_all",
        "admin.institution_read", "admin.institution_create", "admin.institution_update",
        "admin.institution_deactivate", "admin.institution_feature_write",
        "admin.audit_read", "admin.audit_export", "admin.capability_audit_read",
        "admin.feature_group_manage", "admin.feature_definition_manage",
        "admin.legal_agreement_manage", "admin.email_template_manage",
        "admin.about_page_manage", "admin.feedback_read",
        "admin.rule_read", "admin.rule_create", "admin.rule_update", "admin.rule_delete",
        "admin.rule_activate", "admin.rule_test_run", "admin.workflow_log_read",
        "admin.workflow_reference_manage",
        "admin.capability_catalog_read", "admin.capability_catalog_write",
        "admin.capability_grant", "admin.capability_revoke",
        "admin.capability_template_create", "admin.capability_template_publish", "admin.capability_template_apply",
        "admin.announcement_read", "admin.announcement_create", "admin.announcement_delete",
        "admin.cross_institution_read", "admin.user_institution_change",
        "admin.killswitch_read", "admin.killswitch_soft", "admin.killswitch_hard", "admin.killswitch_emergency",
        "admin.dashboard_view",
        "admin.metrics_capability_view", "admin.metrics_workflow_view",
        "admin.metrics_user_view", "admin.metrics_system_health_view", "admin.metrics_export",
        "admin.user_title_assign", "admin.user_title_read",
        // Moderation — tümü
        ..AllModerationCodes,
        // Expert — tümü (csharp_execute dahil)
        "expert.solution_approve", "expert.solution_reject", "expert.solution_quality_score",
        "expert.problem_quality_score", "expert.problem_difficulty_set",
        "expert.content_highlight", "expert.badge_assign", "expert.badge_revoke",
        "expert.workflow_test_run", "expert.csharp_execute",
        // Official
        "official.response_create", "official.response_update",
        // User
        ..AllUserCodes,
        // Chat — tümü
        "chat.use", "chat.institution_manage", "chat.global_manage", "chat.contact_admin",
        "chat.official_channel", "chat.support_request", "chat.escalate", "chat.contact_global_admin",
        "chat.handle_support", "chat.handle_escalations",
        // Workflow actions — tümü
        ..AllWorkflowActionCodes,
        // Page — tüm admin sayfaları
        ..AllPageAdminCodes,
    ];

    private static readonly string[] AdminCodes =
    [
        // Admin — killswitch_emergency ve user_role_change hariç
        "admin.system_access", "admin.system_monitor", "admin.security_monitor",
        "admin.system_settings_read", "admin.system_settings_write",
        "admin.user_read", "admin.user_create", "admin.user_update", "admin.user_delete",
        "admin.user_ban", "admin.user_unban", "admin.user_impersonate", "admin.user_warning_read_all",
        "admin.institution_read", "admin.institution_create", "admin.institution_update",
        "admin.institution_deactivate", "admin.institution_feature_write",
        "admin.audit_read", "admin.audit_export", "admin.capability_audit_read",
        "admin.feature_group_manage", "admin.feature_definition_manage",
        "admin.legal_agreement_manage", "admin.email_template_manage",
        "admin.about_page_manage", "admin.feedback_read",
        "admin.rule_read", "admin.rule_create", "admin.rule_update", "admin.rule_delete",
        "admin.rule_activate", "admin.rule_test_run", "admin.workflow_log_read",
        "admin.workflow_reference_manage",
        "admin.capability_catalog_read", "admin.capability_catalog_write",
        "admin.capability_grant", "admin.capability_revoke",
        "admin.capability_template_create", "admin.capability_template_publish", "admin.capability_template_apply",
        "admin.announcement_read", "admin.announcement_create", "admin.announcement_delete",
        "admin.killswitch_read", "admin.killswitch_soft", "admin.killswitch_hard",
        "admin.dashboard_view",
        "admin.metrics_capability_view", "admin.metrics_workflow_view",
        "admin.metrics_user_view", "admin.metrics_system_health_view", "admin.metrics_export",
        "admin.user_title_assign", "admin.user_title_read",
        // Moderation — tümü
        ..AllModerationCodes,
        // Expert — csharp_execute hariç
        "expert.solution_approve", "expert.solution_reject", "expert.solution_quality_score",
        "expert.problem_quality_score", "expert.problem_difficulty_set",
        "expert.content_highlight", "expert.badge_assign", "expert.badge_revoke",
        "expert.workflow_test_run",
        // Official
        "official.response_create", "official.response_update",
        // User
        ..AllUserCodes,
        // Chat — tümü
        "chat.use", "chat.institution_manage", "chat.global_manage", "chat.contact_admin",
        "chat.official_channel", "chat.support_request", "chat.escalate", "chat.contact_global_admin",
        "chat.handle_support", "chat.handle_escalations",
        // Workflow actions — tümü
        ..AllWorkflowActionCodes,
        // Page — tüm admin sayfaları
        ..AllPageAdminCodes,
    ];

    private static readonly string[] KurumAdminiCodes =
    [
        "admin.system_access", "admin.dashboard_view",
        "admin.user_read", "admin.user_create", "admin.user_update",
        "admin.user_ban", "admin.user_unban", "admin.user_warning_read_all",
        "admin.user_title_assign", "admin.user_title_read",
        "admin.institution_read", "admin.institution_update", "admin.institution_feature_write",
        "admin.audit_read", "admin.capability_audit_read",
        "admin.capability_catalog_read", "admin.capability_grant", "admin.capability_revoke",
        "admin.capability_template_apply",
        "admin.announcement_read", "admin.announcement_create", "admin.announcement_delete",
        "admin.metrics_user_view", "admin.metrics_system_health_view",
        // Moderation — tümü
        ..AllModerationCodes,
        // Expert — csharp/workflow_test_run hariç
        "expert.solution_approve", "expert.solution_reject", "expert.solution_quality_score",
        "expert.problem_quality_score", "expert.problem_difficulty_set",
        "expert.content_highlight", "expert.badge_assign", "expert.badge_revoke",
        // Official
        "official.response_create", "official.response_update",
        // User
        ..AllUserCodes,
        // Chat — kurum yönetimi
        "chat.use", "chat.support_request", "chat.institution_manage", "chat.handle_support",
        // Page — kurum adminine uygun sayfalar
        "page.admin.overview", "page.admin.problems", "page.admin.solutions", "page.admin.topics",
        "page.admin.expert_approvals", "page.admin.users", "page.admin.institutions",
        "page.admin.reports", "page.admin.feedbacks",
        "page.admin.announcements", "page.admin.logs",
        "page.admin.capabilities", "page.admin.capability_templates", "page.admin.capability_audit",
        "page.admin.metrics", "page.admin.settings", "page.admin.chat",
    ];

    private static readonly string[] ModeratörCodes =
    [
        "admin.system_access",
        "admin.announcement_read",
        "admin.user_title_read",
        // Moderation — tümü (user_warn VAR, user_ban YOK)
        ..AllModerationCodes,
        // User
        ..AllUserCodes,
        // Chat — destek karşılama
        "chat.use", "chat.support_request", "chat.handle_support",
        // Page — moderatöre uygun sayfalar
        "page.admin.overview", "page.admin.problems", "page.admin.solutions",
        "page.admin.topics", "page.admin.reports", "page.admin.feedbacks",
        "page.admin.announcements",
    ];

    private static readonly string[] UzmanCodes =
    [
        // Expert — çözüm onaylayıcı; csharp/workflow_test_run hariç
        "expert.solution_approve", "expert.solution_reject", "expert.solution_quality_score",
        "expert.problem_quality_score", "expert.problem_difficulty_set",
        "expert.content_highlight", "expert.badge_assign", "expert.badge_revoke",
        // Admin panel girişi (sadece uzman sayfaları için)
        "admin.system_access",
        // Page — yalnızca uzman sayfaları
        "page.admin.overview", "page.admin.expert_approvals", "page.admin.solutions",
        // User
        ..AllUserCodes,
        // Chat — destek/tırmanma
        "chat.use", "chat.support_request", "chat.escalate",
    ];

    private static readonly string[] StandartKullaniciCodes =
    [
        // User — tümü
        ..AllUserCodes,
        // Chat — temel erişim
        "chat.use", "chat.support_request",
    ];

    private static readonly string[] ResmiGörevliCodes =
    [
        // User — tümü
        ..AllUserCodes,
        // Official
        "official.response_create", "official.response_update",
        // Admin panel girişi
        "admin.system_access",
        // Page — sorun/çözüm görüntüleme
        "page.admin.overview", "page.admin.problems", "page.admin.solutions",
        // Chat
        "chat.use", "chat.support_request", "chat.official_channel",
    ];

    private static readonly string[] WorkflowOperatörCodes =
    [
        "admin.system_access", "admin.dashboard_view",
        "admin.rule_read", "admin.rule_create", "admin.rule_update", "admin.rule_delete",
        "admin.rule_activate", "admin.rule_test_run", "admin.workflow_log_read",
        "admin.workflow_reference_manage",
        "admin.metrics_capability_view", "admin.metrics_workflow_view",
        "admin.metrics_user_view", "admin.metrics_system_health_view",
        "expert.workflow_test_run", "expert.csharp_execute",
        ..AllWorkflowActionCodes,
        // Page — workflow sayfaları
        "page.admin.overview", "page.admin.workflow", "page.admin.metrics",
    ];

    // ── YETKİ PAKETLERİ (Kind = 1) ──────────────────────────────────────────

    private static readonly string[] PkgProblemModCodes =
    [
        "admin.system_access",
        "moderation.content_review",
        "moderation.problem_moderate", "moderation.problem_delete",
        "moderation.problem_highlight", "moderation.problem_resolve",
        "moderation.problem_close", "moderation.problem_reopen", "moderation.problem_hide",
        "page.admin.problems",
    ];

    private static readonly string[] PkgSolutionReviewCodes =
    [
        "admin.system_access",
        "expert.solution_approve", "expert.solution_reject", "expert.solution_quality_score",
        "page.admin.expert_approvals", "page.admin.solutions",
    ];

    private static readonly string[] PkgUserMgmtCodes =
    [
        "admin.system_access",
        "admin.user_read", "admin.user_update", "admin.user_ban", "admin.user_unban",
        "admin.user_warning_read_all",
        "moderation.user_warn", "moderation.user_warn_revoke", "moderation.user_warn_read",
        "page.admin.users",
    ];

    private static readonly string[] PkgAnalyticsCodes =
    [
        "admin.system_access", "admin.dashboard_view",
        "admin.metrics_capability_view", "admin.metrics_workflow_view",
        "admin.metrics_user_view", "admin.metrics_system_health_view",
        "page.admin.overview", "page.admin.metrics",
    ];

    private static readonly string[] PkgAnnouncementsCodes =
    [
        "admin.system_access",
        "admin.announcement_read", "admin.announcement_create", "admin.announcement_delete",
        "moderation.bulk_notification_send",
        "workflow.action.create_announcement", "workflow.action.send_bulk_notification",
        "page.admin.announcements",
    ];

    private static readonly string[] PkgChatSupportCodes =
    [
        "admin.system_access",
        "chat.use", "chat.support_request", "chat.handle_support",
        "page.admin.chat",
    ];

    private static readonly string[] PkgTopicMgmtCodes =
    [
        "admin.system_access",
        "moderation.topic_create", "moderation.topic_update", "moderation.topic_delete",
        "page.admin.topics",
    ];

    // ── Template tanımları: (Name, Slug, Description, Kind, Codes[]) ─────────

    private static readonly (string Name, string Slug, string Description, int Kind, string[] Codes)[] Templates =
    [
        // ── ROL ŞABLONLARI ──────────────────────────────────────────
        ("Süper Admin",
         "super-admin",
         "Platformdaki tüm yetkiler — kill switch emergency dahil. Yalnızca en üst düzey yönetici için. Global kapsam önerilir.",
         0, SuperAdminCodes),

        ("Admin",
         "admin",
         "Tam platform yönetimi — kill switch hard'a kadar. Kill switch emergency ve C# çalıştırma yetkisi yoktur. Global kapsam önerilir.",
         0, AdminCodes),

        ("Kurum Admini",
         "institution-admin",
         "Tek kurumu baştan sona yönetebilme: kullanıcı, içerik, moderasyon, duyuru, metrik. Başka kuruma erişim yoktur.",
         0, KurumAdminiCodes),

        ("Moderatör",
         "moderator",
         "Kurum içi içerik moderasyonu — sorun/çözüm/yorum/konu yönetimi, kullanıcı uyarısı. Kullanıcı yasaklama yetkisi yoktur (bu Kurum Admini'nin işi).",
         0, ModeratörCodes),

        ("Uzman",
         "expert",
         "Alan uzmanı: çözümleri onaylar, kalite skoru verir ve sorunları çözüldü olarak işaretler. Moderasyon veya admin yetkisi yoktur; sadece uzman sayfalarına erişir.",
         0, UzmanCodes),

        ("Resmi Görevli",
         "official-representative",
         "Kurumun resmi temsilcisi: sorunlara kurumsal yanıt yazar ve yanıt durumunu günceller. İçerik denetleme yetkisi yoktur.",
         0, ResmiGörevliCodes),

        ("Standart Kullanıcı",
         "standard-user",
         "Yeni kayıt olan her kullanıcıya otomatik uygulanan temel yetkiler: içerik oluşturma, oy verme, profil yönetimi ve temel sohbet erişimi.",
         0, StandartKullaniciCodes),

        ("Workflow Operatörü",
         "workflow-operator",
         "Otomasyon kurallarını ve tüm workflow action'larını yönetme yetkisi. C# node çalıştırma dahil. Admin panel erişimi sadece workflow sayfalarına kısıtlıdır.",
         0, WorkflowOperatörCodes),

        // ── YETKİ PAKETLERİ ─────────────────────────────────────────
        ("Sorun Moderasyonu Paketi",
         "pkg-problem-mod",
         "Sadece sorun moderasyonu: düzenleme, silme, öne çıkarma, çözüldü işaretleme. Mevcut bir rol üzerine eklenir.",
         1, PkgProblemModCodes),

        ("Çözüm Onaylama Paketi",
         "pkg-solution-review",
         "Çözümleri onaylama ve reddetme yetkisi. Uzman rolü vermeden belirli kişilere çözüm inceleme yetkisi için kullanılır.",
         1, PkgSolutionReviewCodes),

        ("Kullanıcı Yönetimi Paketi",
         "pkg-user-mgmt",
         "Kullanıcı okuma, güncelleme, ban/unban ve uyarı yönetimi. Moderatöre ek kullanıcı yönetim yetkisi vermek için idealdir.",
         1, PkgUserMgmtCodes),

        ("Analitik Görüntüleme Paketi",
         "pkg-analytics",
         "Metrik ve dashboard sayfalarına salt okunur erişim. Paydaşlara veya danışmanlara platform istatistiklerini göstermek için kullanılır.",
         1, PkgAnalyticsCodes),

        ("Duyuru ve Bildirim Paketi",
         "pkg-announcements",
         "Duyuru oluşturma/silme ve toplu bildirim gönderme yetkisi. İletişim sorumlusuna verilebilir.",
         1, PkgAnnouncementsCodes),

        ("Sohbet Desteği Paketi",
         "pkg-chat-support",
         "Kullanıcıların destek taleplerini karşılama yetkisi. Müşteri hizmetleri veya destek ekibi için kullanılır.",
         1, PkgChatSupportCodes),

        ("Konu Yönetimi Paketi",
         "pkg-topic-mgmt",
         "Konu (topic/kategori) oluşturma, düzenleme ve silme yetkisi. İçerik editörlerine konu yönetimi vermek için kullanılır.",
         1, PkgTopicMgmtCodes),
    ];

    // ── ANA SEED METODU ──────────────────────────────────────────────────────

    public static void Seed(DevelopTurkeyContext context)
    {
        var capabilityMap = context.Capabilities
            .Where(c => c.IsActive)
            .ToDictionary(c => c.Code, c => c.Id, StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;

        // ── 1. Yeni şablonlar ekle ──────────────────────────────────────────
        var existingNames = context.CapabilityTemplates
            .Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, slug, description, kind, codes) in Templates)
        {
            if (existingNames.Contains(name))
                continue;

            var template = new CapabilityTemplate
            {
                Name        = name,
                Slug        = slug,
                Description = description,
                Kind        = kind,
                Status      = 1,
                CreatedAt   = now,
                CreatedBy   = 1,
            };
            context.CapabilityTemplates.Add(template);
            context.SaveChanges();

            var version = new TemplateVersion
            {
                TemplateId  = template.Id,
                Version     = 1,
                IsPublished = true,
                PublishedAt = now,
                ChangeNote  = "Sistem tarafından otomatik oluşturuldu.",
                CreatedAt   = now,
                CreatedBy   = 1,
            };
            context.TemplateVersions.Add(version);
            context.SaveChanges();

            foreach (var code in codes)
            {
                if (!capabilityMap.TryGetValue(code, out var capId))
                    continue;

                context.TemplateItems.Add(new TemplateItem
                {
                    TemplateVersionId = version.Id,
                    CapabilityId      = capId,
                });
            }
            context.SaveChanges();
        }

        // ── 2. Mevcut şablonlar — slug/kind eksikse güncelle ───────────────
        UpdateSlugsAndKinds(context);

        // ── 3. Mevcut şablonlar — capability farkı varsa yeni versiyon yayımla ─
        UpdateCanonicalTemplateVersions(context, now);
    }

    // ── Mevcut şablonlara slug ve kind backfill ──────────────────────────────

    private static void UpdateSlugsAndKinds(DevelopTurkeyContext context)
    {
        var updated = false;
        foreach (var (name, slug, _, kind, _) in Templates)
        {
            var template = context.CapabilityTemplates
                .FirstOrDefault(t => t.Name == name && t.Status == 1);
            if (template == null) continue;

            var changed = false;
            if (template.Slug != slug)     { template.Slug = slug; changed = true; }
            if (template.Kind != kind)     { template.Kind = kind; changed = true; }
            if (changed) updated = true;
        }
        if (updated) context.SaveChanges();
    }

    // ── Yeni capability eklendiyse yeni versiyon yayımla ────────────────────

    private static void UpdateCanonicalTemplateVersions(DevelopTurkeyContext context, DateTime now)
    {
        var freshMap = context.Capabilities
            .Where(c => c.IsActive)
            .ToDictionary(c => c.Code, c => c.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var (name, _, _, _, codes) in Templates)
        {
            var template = context.CapabilityTemplates
                .FirstOrDefault(t => t.Name == name && t.Status == 1);
            if (template == null) continue;

            var latestVersion = context.TemplateVersions
                .Where(v => v.TemplateId == template.Id && v.IsPublished)
                .OrderByDescending(v => v.Version)
                .FirstOrDefault();
            if (latestVersion == null) continue;

            var currentCapIds = context.TemplateItems
                .Where(i => i.TemplateVersionId == latestVersion.Id)
                .Select(i => i.CapabilityId)
                .ToHashSet();

            var requiredCapIds = codes
                .Where(c => freshMap.ContainsKey(c))
                .Select(c => freshMap[c])
                .ToHashSet();

            // Hem eksik hem fazla kod varsa yeni versiyon yayımla
            if (requiredCapIds.SetEquals(currentCapIds)) continue;

            var newVersion = new TemplateVersion
            {
                TemplateId  = template.Id,
                Version     = latestVersion.Version + 1,
                IsPublished = true,
                PublishedAt = now,
                ChangeNote  = "Production öncesi şablon revizyonu — rol/paket güncellemesi.",
                CreatedAt   = now,
                CreatedBy   = 1,
            };
            context.TemplateVersions.Add(newVersion);
            context.SaveChanges();

            foreach (var code in codes)
            {
                if (!freshMap.TryGetValue(code, out var capId)) continue;
                context.TemplateItems.Add(new TemplateItem
                {
                    TemplateVersionId = newVersion.Id,
                    CapabilityId      = capId,
                });
            }
            context.SaveChanges();
        }
    }
}
