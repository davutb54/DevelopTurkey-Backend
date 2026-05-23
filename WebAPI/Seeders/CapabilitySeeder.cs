using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;

namespace WebAPI.Seeders;

public static class CapabilitySeeder
{
    public static void Seed(DevelopTurkeyContext context)
    {
        var definitions = new (string Code, string Description, string Category)[]
        {
            // ══════════════════════════════════════════════════════════════════
            // ADMIN — Sistem yönetimi (global scope, sadece SUPER_ADMIN/ADMIN)
            // ══════════════════════════════════════════════════════════════════

            ("admin.system_access",          "Yönetim paneline genel erişim. Tüm admin endpointlerinin ön koşulu.", "admin"),
            ("admin.system_monitor",         "Sistem sağlık metrikleri, uptime, CPU/RAM dashboard görüntüleme.",      "admin"),
            ("admin.system_settings_read",   "SystemSettings kayıtlarını okuma.",                                     "admin"),
            ("admin.system_settings_write",  "SystemSettings güncelleme (site adı, varsayılan dil, vb.).",            "admin"),

            ("admin.user_read",              "Tüm kullanıcıları (kurum-bağımsız) görüntüleme.",                       "admin"),
            ("admin.user_create",            "Manuel olarak yeni kullanıcı kaydı oluşturma.",                         "admin"),
            ("admin.user_update",            "Kullanıcı profilini güncelleme (ad, email, kurum).",                    "admin"),
            ("admin.user_delete",            "Kullanıcı hesabını silme.",                                             "admin"),
            ("admin.user_ban",               "Kullanıcıyı süreli veya kalıcı banlama.",                               "admin"),
            ("admin.user_unban",             "Kullanıcı banını kaldırma.",                                            "admin"),
            ("admin.user_role_change",       "IsAdmin/IsExpert/IsOfficial bayraklarını değiştirme (legacy köprü).",   "admin"),
            ("admin.user_impersonate",       "Başka bir kullanıcı kimliğine geçiş (sudo).",                           "admin"),
            ("admin.user_warning_read_all",  "Tüm kullanıcı uyarılarını (UserWarning) okuma.",                        "admin"),

            ("admin.institution_read",          "Tüm kurumları görüntüleme.",                                          "admin"),
            ("admin.institution_create",        "Yeni kurum oluşturma.",                                               "admin"),
            ("admin.institution_update",        "Kurum bilgilerini güncelleme.",                                       "admin"),
            ("admin.institution_deactivate",    "Kurumu pasifleştirme.",                                               "admin"),
            ("admin.institution_feature_write", "Kuruma özel feature değerlerini ayarlama.",                           "admin"),

            ("admin.audit_read",        "Sistem AuditLog kayıtlarını görüntüleme.",                                    "admin"),
            ("admin.audit_export",      "Audit log dışa aktarımı (CSV/JSON).",                                         "admin"),
            ("admin.capability_audit_read", "CapabilityAuditLog kayıtlarını okuma.",                                   "admin"),

            ("admin.feature_group_manage",       "FeatureGroup CRUD (global tanımlar).",                               "admin"),
            ("admin.feature_definition_manage",  "FeatureDefinition CRUD (global tanımlar).",                          "admin"),
            ("admin.legal_agreement_manage",     "LegalAgreement (KVKK, gizlilik, kullanım) CRUD ve yayımlama.",       "admin"),
            ("admin.email_template_manage",      "EmailTemplate CRUD (sistem maillerinin şablonları).",                "admin"),
            ("admin.about_page_manage",          "AboutPageSection CRUD.",                                             "admin"),
            ("admin.feedback_read",              "Kullanıcı geri bildirimlerini (Feedback) okuma.",                    "admin"),

            ("admin.rule_read",             "DynamicRule kayıtlarını görüntüleme.",                                    "admin"),
            ("admin.rule_create",           "Yeni DynamicRule oluşturma.",                                             "admin"),
            ("admin.rule_update",           "Mevcut DynamicRule düzenleme.",                                           "admin"),
            ("admin.rule_delete",           "DynamicRule silme.",                                                      "admin"),
            ("admin.rule_activate",         "DynamicRule aktif/pasif durumunu değiştirme.",                            "admin"),
            ("admin.rule_test_run",         "Workflow dry-run / test-run çalıştırma.",                                 "admin"),
            ("admin.workflow_log_read",     "WorkflowLog kayıtlarını görüntüleme.",                                    "admin"),
            ("admin.workflow_reference_manage", "WorkflowTrigger/Field/Action katalog yönetimi.",                      "admin"),

            ("admin.capability_catalog_read",     "Capability listesini okuma.",                                       "admin"),
            ("admin.capability_catalog_write",    "Yeni custom (non-system) capability oluşturma ve düzenleme.",       "admin"),
            ("admin.capability_grant",            "UserCapability oluşturma (grant).",                                 "admin"),
            ("admin.capability_revoke",           "UserCapability iptal etme (revoke).",                               "admin"),
            ("admin.capability_template_create",  "CapabilityTemplate oluşturma.",                                     "admin"),
            ("admin.capability_template_publish", "Template'in yeni versiyonunu yayımlama.",                           "admin"),
            ("admin.capability_template_apply",   "Template'i kullanıcı(lara) uygulama (toplu grant).",                "admin"),

            ("admin.killswitch_read",       "Kill switch durumunu okuma.",                                              "admin"),
            ("admin.killswitch_soft",       "Soft kill (yeni run kabul etmeme) tetikleme.",                             "admin"),
            ("admin.killswitch_hard",       "Hard kill (worker durdurma) tetikleme.",                                   "admin"),
            ("admin.killswitch_emergency",  "Emergency lock (tüm workflow altyapısı). Sadece SUPER_ADMIN.",             "admin"),

            ("admin.dashboard_view",          "Admin paneli ana dashboarda erişim ve özet kartları görme.",           "admin"),
            ("admin.metrics_capability_view", "Capability sistemi metriklerini görme (snapshot doluluğu, grant/revoke trendi, top capabilityler).", "admin"),
            ("admin.metrics_workflow_view",   "Workflow run istatistiklerini görme (success/fail oranı, top actionlar, ortalama süre).", "admin"),
            ("admin.metrics_user_view",       "Kullanıcı aktivite metriklerini görme (DAU/WAU/MAU, yeni kayıt trendi, top contributors).", "admin"),
            ("admin.metrics_system_health_view", "Sistem sağlık göstergeleri (RAM, uptime, snapshot durumu, hata sayısı, DB stats).", "admin"),
            ("admin.metrics_export",          "Metrik verilerini CSV/JSON olarak dışa aktarma.",                       "admin"),

            // ══════════════════════════════════════════════════════════════════
            // MODERATION — Kurum modaratörü (institution scope)
            // ══════════════════════════════════════════════════════════════════

            ("moderation.content_review",      "Bekleyen veya raporlanan içeriği inceleme.",                            "moderation"),
            ("moderation.problem_moderate",    "Problem moderasyonu — düzenleme, taşıma, etiketleme.",                  "moderation"),
            ("moderation.problem_delete",      "Problem silme (institution kapsamında).",                               "moderation"),
            ("moderation.problem_highlight",   "Problem öne çıkarma (highlight) işareti.",                              "moderation"),
            ("moderation.problem_resolve",     "Problem çözüldü olarak işaretleme.",                                    "moderation"),
            ("moderation.solution_moderate",   "Çözüm moderasyonu.",                                                    "moderation"),
            ("moderation.solution_delete",     "Çözüm silme.",                                                          "moderation"),
            ("moderation.solution_highlight",  "Çözüm vurgulama.",                                                      "moderation"),
            ("moderation.comment_moderate",    "Yorum moderasyonu.",                                                    "moderation"),
            ("moderation.comment_delete",      "Yorum silme.",                                                          "moderation"),
            ("moderation.user_warn",           "Kullanıcıya uyarı (UserWarning) oluşturma.",                            "moderation"),
            ("moderation.user_warn_revoke",    "Kullanıcı uyarısını geri alma.",                                        "moderation"),
            ("moderation.user_warn_read",      "Kurum üyelerinin uyarı geçmişini okuma.",                               "moderation"),
            ("moderation.report_review",       "Raporlanan içeriği inceleme.",                                          "moderation"),
            ("moderation.report_resolve",      "Raporu karara bağlama (resolved/dismissed).",                           "moderation"),
            ("moderation.topic_create",        "Yeni topic oluşturma (institution kapsamında).",                        "moderation"),
            ("moderation.topic_update",        "Topic düzenleme.",                                                      "moderation"),
            ("moderation.topic_delete",        "Topic silme.",                                                          "moderation"),
            ("moderation.bulk_notification_send", "Toplu bildirim gönderme (kurum üyelerine).",                         "moderation"),
            ("moderation.audit_view_institution", "Sadece kurumun audit logunu görme.",                                 "moderation"),

            // ══════════════════════════════════════════════════════════════════
            // EXPERT — Uzman (genelde global, IsExpert muadili)
            // ══════════════════════════════════════════════════════════════════

            ("expert.solution_approve",          "Çözümü onaylama.",                                                    "expert"),
            ("expert.solution_reject",           "Çözümü reddetme.",                                                    "expert"),
            ("expert.solution_quality_score",    "Çözüme kalite skoru atama.",                                          "expert"),
            ("expert.problem_quality_score",     "Probleme kalite skoru atama.",                                        "expert"),
            ("expert.problem_difficulty_set",    "Problem zorluk seviyesini belirleme.",                                "expert"),
            ("expert.content_highlight",         "Uzman vurgusu (badge) ekleme.",                                       "expert"),
            ("expert.badge_assign",              "Kullanıcılara rozet atama.",                                          "expert"),
            ("expert.badge_revoke",              "Rozet geri alma.",                                                    "expert"),
            ("expert.workflow_test_run",         "Workflow dry-run çalıştırma yetkisi (admin değil).",                  "expert"),
            ("expert.csharp_execute",            "Workflow C# (Roslyn) node'u çalıştırma yetkisi.",                     "expert"),

            // ══════════════════════════════════════════════════════════════════
            // USER — Standart kullanıcı (self scope, default herkese verilir)
            // ══════════════════════════════════════════════════════════════════

            ("user.problem_create",       "Yeni problem oluşturma.",                                                    "user"),
            ("user.problem_update_own",   "Kendi oluşturduğu problemi güncelleme.",                                     "user"),
            ("user.problem_delete_own",   "Kendi oluşturduğu problemi silme.",                                          "user"),
            ("user.solution_create",      "Yeni çözüm paylaşma.",                                                       "user"),
            ("user.solution_update_own",  "Kendi çözümünü güncelleme.",                                                 "user"),
            ("user.solution_delete_own",  "Kendi çözümünü silme.",                                                      "user"),
            ("user.comment_create",       "Yorum yazma.",                                                               "user"),
            ("user.comment_update_own",   "Kendi yorumunu güncelleme.",                                                 "user"),
            ("user.comment_delete_own",   "Kendi yorumunu silme.",                                                      "user"),

            ("user.problem_upvote",       "Probleme 'Ben de yaşıyorum' desteği verme.",                                 "user"),
            ("user.problem_unvote",       "Problem desteğini geri çekme.",                                              "user"),
            ("user.solution_upvote",      "Çözüme olumlu oy verme.",                                                    "user"),
            ("user.solution_downvote",    "Çözüme olumsuz oy verme.",                                                   "user"),
            ("user.vote_retract",         "Verilen oyu geri alma.",                                                     "user"),

            ("user.problem_follow",       "Problem takip etme.",                                                        "user"),
            ("user.topic_follow",         "Topic takip etme.",                                                          "user"),
            ("user.solution_save",        "Çözümü kaydedilenlere ekleme.",                                              "user"),
            ("user.content_share",        "İçeriği harici platformlarda paylaşma (WhatsApp/X/Instagram).",              "user"),
            ("user.mention_user",         "Yorumda başka kullanıcıyı etiketleme (@mention).",                           "user"),

            ("user.content_report",       "Problem/solution/comment raporlama.",                                        "user"),
            ("user.user_report",          "Başka kullanıcıyı raporlama.",                                               "user"),

            ("user.profile_update",            "Profil bilgilerini güncelleme.",                                        "user"),
            ("user.profile_avatar_change",     "Avatar değiştirme.",                                                    "user"),
            ("user.profile_email_change",      "E-posta adresini değiştirme (doğrulama gerekir).",                      "user"),
            ("user.profile_password_change",   "Şifre değiştirme.",                                                     "user"),
            ("user.profile_username_change",   "Kullanıcı adı (username) değiştirme.",                                  "user"),

            ("user.content_export",       "Kendi içeriği ve hesap verisini dışa aktarma (veri taşınabilirlik).",        "user"),
            ("user.account_delete",       "Kendi hesabını silme (right to be forgotten).",                              "user"),
            ("user.notifications_manage", "Kendi bildirim tercihlerini yönetme.",                                       "user"),
            ("user.feedback_send",        "Sisteme geri bildirim gönderme.",                                            "user"),

            // ══════════════════════════════════════════════════════════════════
            // WORKFLOW ACTION GATING — her workflow action için ayrı kapı
            // ══════════════════════════════════════════════════════════════════

            ("workflow.action.send_email",            "Workflow 'send_email' actionını kullanma.",            "workflow.action"),
            ("workflow.action.send_notification",     "Workflow 'send_notification' actionını kullanma.",     "workflow.action"),
            ("workflow.action.send_bulk_notification","Workflow 'send_bulk_notification' actionını kullanma.","workflow.action"),
            ("workflow.action.ban_user",              "Workflow 'ban_user' actionını kullanma.",              "workflow.action"),
            ("workflow.action.unban_user",            "Workflow 'unban_user' actionını kullanma.",            "workflow.action"),
            ("workflow.action.warn_user",             "Workflow 'warn_user' actionını kullanma.",             "workflow.action"),
            ("workflow.action.change_user_role",      "Workflow 'change_user_role' actionını kullanma.",      "workflow.action"),
            ("workflow.action.resolve_problem",       "Workflow 'resolve_problem' actionını kullanma.",       "workflow.action"),
            ("workflow.action.highlight_problem",     "Workflow 'highlight_problem' actionını kullanma.",     "workflow.action"),
            ("workflow.action.delete_problem",        "Workflow 'delete_problem' actionını kullanma.",        "workflow.action"),
            ("workflow.action.report_problem",        "Workflow 'report_problem' actionını kullanma.",        "workflow.action"),
            ("workflow.action.approve_solution",      "Workflow 'approve_solution' actionını kullanma.",      "workflow.action"),
            ("workflow.action.reject_solution",       "Workflow 'reject_solution' actionını kullanma.",       "workflow.action"),
            ("workflow.action.highlight_solution",    "Workflow 'highlight_solution' actionını kullanma.",    "workflow.action"),
            ("workflow.action.delete_solution",       "Workflow 'delete_solution' actionını kullanma.",       "workflow.action"),
            ("workflow.action.delete_comment",        "Workflow 'delete_comment' actionını kullanma.",        "workflow.action"),
            ("workflow.action.log_event",             "Workflow 'log_event' actionını kullanma.",             "workflow.action"),
            ("workflow.action.webhook",               "Workflow 'webhook' actionını kullanma (dış sisteme HTTP isteği).", "workflow.action"),
        };

        var existingCodes = context.Capabilities
            .Select(c => c.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addedAny = false;
        foreach (var (code, description, category) in definitions)
        {
            if (existingCodes.Contains(code))
                continue;

            context.Capabilities.Add(new Capability
            {
                Code = code,
                Description = description,
                Category = category,
                IsSystem = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
            addedAny = true;
        }

        if (addedAny)
            context.SaveChanges();
    }
}
