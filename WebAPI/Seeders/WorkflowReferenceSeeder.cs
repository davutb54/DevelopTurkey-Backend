using DataAccess.Concrete.EntityFramework;
using Entities.Concrete;

namespace WebAPI.Seeders;

public static class WorkflowReferenceSeeder
{
    public static void Seed(DevelopTurkeyContext context)
    {
        if (!context.WorkflowTriggers.Any())
        {
            var triggers = new List<WorkflowTrigger>
            {
                // ── Auth ──────────────────────────────────────────────────────────────
                new() { Name = "User Registered",              CodeName = "auth.registered",               Description = "Yeni kullanici kaydi olusturuldu.",                TargetEntity = "User",            IsActive = true },
                new() { Name = "Login Success",                CodeName = "auth.login_success",            Description = "Kullanici basarili giris yapti.",                  TargetEntity = "User",            IsActive = true },
                new() { Name = "Login Failed",                 CodeName = "auth.login_failed",             Description = "Basarisiz giris denemesi yapildi.",                TargetEntity = "User",            IsActive = true },
                new() { Name = "Logout",                       CodeName = "auth.logout",                   Description = "Kullanici cikis yapti.",                           TargetEntity = "User",            IsActive = true },
                new() { Name = "Google Login",                 CodeName = "auth.google_login",             Description = "Google hesabiyla giris yapildi.",                  TargetEntity = "User",            IsActive = true },
                new() { Name = "Account Locked Out",           CodeName = "auth.locked_out",               Description = "Cok fazla basarisiz deneme nedeniyle hesap kilitlendi.", TargetEntity = "User",       IsActive = true },
                new() { Name = "Email Verified",               CodeName = "auth.email_verified",           Description = "E-posta adresi basariyla dogrulandi.",             TargetEntity = "User",            IsActive = true },
                new() { Name = "Verification Code Sent",       CodeName = "auth.verification_code_sent",   Description = "E-posta dogrulama kodu gonderildi.",               TargetEntity = "User",            IsActive = true },
                new() { Name = "Verification Code Resent",     CodeName = "auth.verification_resent",      Description = "Dogrulama kodu yeniden gonderildi.",               TargetEntity = "User",            IsActive = true },
                new() { Name = "Password Changed",             CodeName = "auth.password_changed",         Description = "Kullanici sifresi degistirildi.",                  TargetEntity = "User",            IsActive = true },
                new() { Name = "Password Reset Requested",     CodeName = "auth.password_reset_requested", Description = "Sifre sifirlama kodu gonderildi.",                 TargetEntity = "User",            IsActive = true },
                new() { Name = "Password Reset Verified",      CodeName = "auth.password_reset_verified",  Description = "Sifre sifirlama kodu dogrulandi.",                 TargetEntity = "User",            IsActive = true },
                new() { Name = "Password Reset Completed",     CodeName = "auth.password_reset_completed", Description = "Sifre basariyla sifirlandi.",                      TargetEntity = "User",            IsActive = true },
                new() { Name = "Admin Impersonated User",      CodeName = "auth.impersonated",             Description = "Admin baska bir kullanici hesabina gecis yapti.",   TargetEntity = "User",            IsActive = true },
                new() { Name = "Impersonation Reverted",       CodeName = "auth.impersonation_reverted",   Description = "Admin kendi hesabina geri dondu.",                 TargetEntity = "User",            IsActive = true },

                // ── User ─────────────────────────────────────────────────────────────
                new() { Name = "User Updated",                 CodeName = "user.updated",                  Description = "Kullanici profili guncellendi.",                   TargetEntity = "User",            IsActive = true },
                new() { Name = "User Deleted",                 CodeName = "user.deleted",                  Description = "Kullanici hesabi silindi.",                        TargetEntity = "User",            IsActive = true },
                new() { Name = "User Banned",                  CodeName = "user.banned",                   Description = "Kullanici yasaklandi.",                            TargetEntity = "User",            IsActive = true },
                new() { Name = "User Unbanned",                CodeName = "user.unbanned",                 Description = "Kullanici yasagi kaldirildi.",                     TargetEntity = "User",            IsActive = true },
                new() { Name = "User Reported",                CodeName = "user.reported",                 Description = "Kullanici raporlandi.",                            TargetEntity = "User",            IsActive = true },
                new() { Name = "User Unreported",              CodeName = "user.unreported",               Description = "Kullanici raporu kaldirildi.",                     TargetEntity = "User",            IsActive = true },
                new() { Name = "User Role Changed",            CodeName = "user.role_changed",             Description = "Kullanici rolu degistirildi.",                     TargetEntity = "User",            IsActive = true },
                new() { Name = "User Institution Changed",     CodeName = "user.institution_changed",      Description = "Kullanici kurumu degistirildi.",                   TargetEntity = "User",            IsActive = true },
                new() { Name = "Username Changed",             CodeName = "user.username_changed",         Description = "Kullanici adi degistirildi.",                      TargetEntity = "User",            IsActive = true },
                new() { Name = "Warning Issued",               CodeName = "user.warning_issued",           Description = "Kullaniciya uyari verildi.",                       TargetEntity = "User",            IsActive = true },
                new() { Name = "Warning Revoked",              CodeName = "user.warning_revoked",          Description = "Kullanici uyarisi geri alindi.",                   TargetEntity = "User",            IsActive = true },

                // ── Problem ──────────────────────────────────────────────────────────
                new() { Name = "Problem Created",              CodeName = "problem.created",               Description = "Yeni problem olusturuldu.",                        TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Updated",              CodeName = "problem.updated",               Description = "Problem guncellendi.",                             TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Deleted",              CodeName = "problem.deleted",               Description = "Problem silindi.",                                 TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Resolved",             CodeName = "problem.resolved",              Description = "Problem cozuldu olarak isaretlendi.",              TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Resolved Toggled",     CodeName = "problem.resolved_toggled",      Description = "Problem cozum durumu degistirildi.",               TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Reported",             CodeName = "problem.reported",              Description = "Problem raporlandi.",                              TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Unreported",           CodeName = "problem.unreported",            Description = "Problem raporu kaldirildi.",                       TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Highlight Toggled",    CodeName = "problem.highlight_toggled",     Description = "Problem one cikma durumu degistirildi.",           TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem View Incremented",     CodeName = "problem.view_incremented",      Description = "Problem goruntuleme sayisi artti.",                TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Topic Removed",        CodeName = "problem.topic_removed",         Description = "Problemden konu kaldirildi.",                      TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Upvoted",              CodeName = "problem.upvoted",               Description = "Probleme destek verildi.",                         TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Unvoted",              CodeName = "problem.unvoted",               Description = "Problem destegi geri cekildi.",                    TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Followed",             CodeName = "problem.followed",              Description = "Problem takip edildi.",                            TargetEntity = "Problem",         IsActive = true },
                new() { Name = "Problem Unfollowed",           CodeName = "problem.unfollowed",            Description = "Problem takipten cikiildi.",                       TargetEntity = "Problem",         IsActive = true },

                // ── Solution ─────────────────────────────────────────────────────────
                new() { Name = "Solution Created",             CodeName = "solution.created",              Description = "Yeni cozum paylasildi.",                           TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Updated",             CodeName = "solution.updated",              Description = "Cozum guncellendi.",                               TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Deleted",             CodeName = "solution.deleted",              Description = "Cozum silindi.",                                   TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Reported",            CodeName = "solution.reported",             Description = "Cozum raporlandi.",                                TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Unreported",          CodeName = "solution.unreported",           Description = "Cozum raporu kaldirildi.",                         TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Approved",            CodeName = "solution.approved",             Description = "Cozum onaylandi.",                                 TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Rejected",            CodeName = "solution.rejected",             Description = "Cozum reddedildi.",                                TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Highlight Toggled",   CodeName = "solution.highlight_toggled",    Description = "Cozum one cikma durumu degistirildi.",             TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Upvoted",             CodeName = "solution.upvoted",              Description = "Cozume olumlu oy verildi.",                        TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Downvoted",           CodeName = "solution.downvoted",            Description = "Cozume olumsuz oy verildi.",                       TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Vote Retracted",      CodeName = "solution.vote_retracted",       Description = "Cozum oyu geri alindi.",                           TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Vote Changed",        CodeName = "solution.vote_changed",         Description = "Cozum oy yonu degistirildi.",                      TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Saved",               CodeName = "solution.saved",                Description = "Cozum kaydedildi.",                                TargetEntity = "Solution",        IsActive = true },
                new() { Name = "Solution Unsaved",             CodeName = "solution.unsaved",              Description = "Cozum kaydedilenlerden cikarildi.",                TargetEntity = "Solution",        IsActive = true },

                // ── Comment ──────────────────────────────────────────────────────────
                new() { Name = "Comment Created",              CodeName = "comment.created",               Description = "Yeni yorum eklendi.",                              TargetEntity = "Comment",         IsActive = true },
                new() { Name = "Comment Updated",              CodeName = "comment.updated",               Description = "Yorum guncellendi.",                               TargetEntity = "Comment",         IsActive = true },
                new() { Name = "Comment Deleted",              CodeName = "comment.deleted",               Description = "Yorum silindi.",                                   TargetEntity = "Comment",         IsActive = true },

                // ── Topic ─────────────────────────────────────────────────────────────
                new() { Name = "Topic Created",                CodeName = "topic.created",                 Description = "Yeni konu olusturuldu.",                           TargetEntity = "Topic",           IsActive = true },
                new() { Name = "Topic Updated",                CodeName = "topic.updated",                 Description = "Konu guncellendi.",                                TargetEntity = "Topic",           IsActive = true },
                new() { Name = "Topic Deleted",                CodeName = "topic.deleted",                 Description = "Konu silindi.",                                    TargetEntity = "Topic",           IsActive = true },
                new() { Name = "Topic Followed",               CodeName = "topic.followed",                Description = "Konu takip edildi.",                               TargetEntity = "Topic",           IsActive = true },
                new() { Name = "Topic Unfollowed",             CodeName = "topic.unfollowed",              Description = "Konu takipten cikiildi.",                          TargetEntity = "Topic",           IsActive = true },

                // ── Report ────────────────────────────────────────────────────────────
                new() { Name = "Report Created",               CodeName = "report.created",                Description = "Yeni rapor olusturuldu.",                          TargetEntity = "Report",          IsActive = true },
                new() { Name = "Report Resolved",              CodeName = "report.resolved",               Description = "Rapor cozuldu.",                                   TargetEntity = "Report",          IsActive = true },

                // ── Feedback ──────────────────────────────────────────────────────────
                new() { Name = "Feedback Received",            CodeName = "feedback.received",             Description = "Yeni geri bildirim alindi.",                       TargetEntity = "Feedback",        IsActive = true },

                // ── Legal ─────────────────────────────────────────────────────────────
                new() { Name = "Agreement Created",            CodeName = "legal.agreement_created",       Description = "Yeni sozlesme olusturuldu.",                       TargetEntity = "LegalAgreement",  IsActive = true },
                new() { Name = "Agreement Published",          CodeName = "legal.agreement_published",     Description = "Sozlesme yayinlandi.",                             TargetEntity = "LegalAgreement",  IsActive = true },

                // ── Institution ───────────────────────────────────────────────────────
                new() { Name = "Institution Created",          CodeName = "institution.created",           Description = "Yeni kurum olusturuldu.",                          TargetEntity = "Institution",     IsActive = true },
                new() { Name = "Institution Updated",          CodeName = "institution.updated",           Description = "Kurum guncellendi.",                               TargetEntity = "Institution",     IsActive = true },
                new() { Name = "Institution Deactivated",      CodeName = "institution.deactivated",       Description = "Kurum pasife alindi.",                             TargetEntity = "Institution",     IsActive = true },
                new() { Name = "Institution Feature Changed",  CodeName = "institution.feature_changed",   Description = "Kurum ozelligi degistirildi.",                     TargetEntity = "Institution",     IsActive = true },

                // ── System ────────────────────────────────────────────────────────────
                new() { Name = "System Settings Changed",      CodeName = "system.settings_changed",       Description = "Sistem ayarlari guncellendi.",                     TargetEntity = "System",          IsActive = true },
            };

            context.WorkflowTriggers.AddRange(triggers);
            context.SaveChanges();
        }

        if (!context.WorkflowFields.Any())
        {
            var fields = new List<WorkflowField>
            {
                // ── RuleContext direct properties ─────────────────────────────────────
                new() { Name = "System User Id",       FieldPath = "SystemUserId",      DataType = "int",    IsActive = true },
                new() { Name = "Institution Id",       FieldPath = "InstitutionId",     DataType = "int",    IsActive = true },
                new() { Name = "Target User Id",       FieldPath = "TargetUserId",      DataType = "int",    IsActive = true },
                new() { Name = "Old Value",            FieldPath = "OldValue",          DataType = "string", IsActive = true },
                new() { Name = "New Value",            FieldPath = "NewValue",          DataType = "string", IsActive = true },
                new() { Name = "Trigger Event Name",   FieldPath = "TriggerEventName",  DataType = "string", IsActive = true },

                // ── Metadata keys ─────────────────────────────────────────────────────
                new() { Name = "Problem Id",           FieldPath = "ProblemId",         DataType = "int",    IsActive = true },
                new() { Name = "Solution Id",          FieldPath = "SolutionId",        DataType = "int",    IsActive = true },
                new() { Name = "Comment Id",           FieldPath = "CommentId",         DataType = "int",    IsActive = true },
                new() { Name = "Topic Id",             FieldPath = "TopicId",           DataType = "int",    IsActive = true },
                new() { Name = "Agreement Id",         FieldPath = "AgreementId",       DataType = "int",    IsActive = true },
                new() { Name = "Warning Id",           FieldPath = "WarningId",         DataType = "int",    IsActive = true },
                new() { Name = "Target Id",            FieldPath = "TargetId",          DataType = "int",    IsActive = true },
                new() { Name = "Admin Id",             FieldPath = "AdminId",           DataType = "int",    IsActive = true },
                new() { Name = "Lockout Minutes",      FieldPath = "LockoutMinutes",    DataType = "int",    IsActive = true },
                new() { Name = "Lockout Count",        FieldPath = "LockoutCount",      DataType = "int",    IsActive = true },
                new() { Name = "Role Name",            FieldPath = "RoleName",          DataType = "string", IsActive = true },
                new() { Name = "User Role",            FieldPath = "UserRole",          DataType = "string", IsActive = true },
                new() { Name = "Target Type",          FieldPath = "TargetType",        DataType = "string", IsActive = true },
                new() { Name = "Feature Key",          FieldPath = "FeatureKey",        DataType = "string", IsActive = true },
                new() { Name = "Severity",             FieldPath = "Severity",          DataType = "string", IsActive = true },
                new() { Name = "Ip Address",           FieldPath = "IpAddress",         DataType = "string", IsActive = true },
                new() { Name = "Problem Status",       FieldPath = "ProblemStatus",     DataType = "string", IsActive = true },
                new() { Name = "Problem Difficulty",   FieldPath = "ProblemDifficulty", DataType = "string", IsActive = true },
                new() { Name = "Is Upvote",            FieldPath = "IsUpvote",          DataType = "bool",   IsActive = true },
                new() { Name = "User Score",           FieldPath = "UserScore",         DataType = "int",    IsActive = true },
            };

            context.WorkflowFields.AddRange(fields);
            context.SaveChanges();
        }

        // UserProblemCount mevcut değilse ekle (mevcut kurulumlar için patch)
        if (!context.WorkflowFields.Any(f => f.FieldPath == "UserProblemCount"))
        {
            context.WorkflowFields.Add(new() { Name = "User Problem Count", FieldPath = "UserProblemCount", DataType = "int", IsActive = true });
            context.SaveChanges();
        }

        // ── WorkflowActions: tam idempotent upsert ───────────────────────────────
        // Her startup'ta çalışır. ActionCode'a göre: varsa güncelle, yoksa ekle.
        // change_user_role kaldırıldı — handler silindi, DB kaydı da temizlenir.
        {
            var stale = context.WorkflowActions.FirstOrDefault(a => a.ActionCode == "change_user_role");
            if (stale != null) { context.WorkflowActions.Remove(stale); context.SaveChanges(); }
        }

        var actionDefs = new[]
        {
            // Code, Name, Category, Icon, Description, ParametersSchemaJson
            // ── İletişim ─────────────────────────────────────────────────────────
            ("send_email", "E-posta Gönder", "İletişim", "📧",
             "Sisteme kayıtlı şablonu kullanarak veya özel içerikle e-posta gönderir.",
             """[{"key":"recipient","label":"Alıcı Tipi","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"context_user"},{"key":"customTo","label":"Özel E-posta (recipient=custom)","type":"text","required":false,"defaultValue":""},{"key":"templateKey","label":"E-posta Şablonu Anahtarı","type":"text","required":false,"defaultValue":""},{"key":"subject","label":"Konu (şablon seçilmemişse)","type":"text","required":false,"defaultValue":""},{"key":"body","label":"İçerik (şablon seçilmemişse)","type":"text","required":false,"defaultValue":""},{"key":"cc","label":"CC Adresleri (virgülle ayır)","type":"text","required":false,"defaultValue":""}]"""),
            ("send_notification", "Bildirim Gönder", "İletişim", "🔔",
             "Kullanıcıya uygulama içi bildirim gönderir; isteğe bağlı yönlendirme bağlantısı eklenebilir.",
             """[{"key":"recipientType","label":"Alıcı Tipi","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"context_user"},{"key":"customUserId","label":"Kullanıcı ID (recipientType=custom)","type":"text","required":false,"defaultValue":""},{"key":"title","label":"Başlık","type":"text","required":true,"defaultValue":""},{"key":"message","label":"Mesaj","type":"text","required":true,"defaultValue":""},{"key":"type","label":"Tür","type":"select","options":["info","success","warning","error"],"required":true,"defaultValue":"info"},{"key":"referenceLink","label":"Yönlendirme Bağlantısı (opsiyonel)","type":"text","required":false,"defaultValue":""}]"""),
            ("send_bulk_notification", "Toplu Bildirim Gönder", "İletişim", "📣",
             "Kurumdaki tüm kullanıcılara veya belirli bir role sahip kullanıcılara toplu bildirim gönderir.",
             """[{"key":"targetGroup","label":"Hedef Grup","type":"select","options":["institution","role"],"required":true,"defaultValue":"institution"},{"key":"role","label":"Rol (targetGroup=role ise)","type":"select","options":["User","Admin","Expert","Official"],"required":false,"defaultValue":"User"},{"key":"title","label":"Başlık","type":"text","required":true,"defaultValue":""},{"key":"message","label":"Mesaj","type":"text","required":true,"defaultValue":""},{"key":"type","label":"Tür","type":"select","options":["info","success","warning","error"],"required":true,"defaultValue":"info"}]"""),

            // ── Kullanıcı Yönetimi ────────────────────────────────────────────────
            ("ban_user", "Kullanıcıyı Yasakla", "Kullanıcı Yönetimi", "🚫",
             "Kullanıcı hesabını belirlenen süre boyunca veya kalıcı olarak askıya alır.",
             """[{"key":"userTarget","label":"Hedef Kullanıcı","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"target_user"},{"key":"customUserId","label":"Kullanıcı ID (userTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"durationDays","label":"Süre (gün, 0=kalıcı)","type":"number","required":true,"defaultValue":"7"},{"key":"reason","label":"Sebep","type":"text","required":false,"defaultValue":""},{"key":"notifyUser","label":"Kullanıcıyı Bildir","type":"boolean","required":false,"defaultValue":"true"}]"""),
            ("unban_user", "Yasağı Kaldır", "Kullanıcı Yönetimi", "✅",
             "Askıya alınmış kullanıcının yasağını kaldırır ve isteğe bağlı bildirim gönderir.",
             """[{"key":"userTarget","label":"Hedef Kullanıcı","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"target_user"},{"key":"customUserId","label":"Kullanıcı ID (userTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"notifyUser","label":"Kullanıcıyı Bildir","type":"boolean","required":false,"defaultValue":"true"}]"""),
            ("warn_user", "Kullanıcıyı Uyar", "Kullanıcı Yönetimi", "⚠️",
             "Kullanıcıya resmi uyarı kaydı oluşturur ve bildirim gönderir.",
             """[{"key":"userTarget","label":"Hedef Kullanıcı","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"target_user"},{"key":"customUserId","label":"Kullanıcı ID (userTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"title","label":"Uyarı Başlığı","type":"text","required":true,"defaultValue":"Kural İhlali"},{"key":"message","label":"Uyarı Mesajı","type":"text","required":true,"defaultValue":""},{"key":"severity","label":"Ağırlık","type":"select","options":["low","medium","high"],"required":true,"defaultValue":"medium"}]"""),
            ("grant_capability", "Yetki Ver / Kaldır", "Kullanıcı Yönetimi", "🔑",
             "Kullanıcıya belirli bir yetki (capability) kodu verir veya kaldırır.",
             """[{"key":"userTarget","label":"Hedef Kullanıcı","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"target_user"},{"key":"customUserId","label":"Kullanıcı ID (userTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"capabilityCode","label":"Yetki Kodu","type":"capability-select","required":true,"defaultValue":""},{"key":"action","label":"İşlem","type":"select","options":["grant","revoke"],"required":true,"defaultValue":"grant"},{"key":"reason","label":"Sebep","type":"text","required":false,"defaultValue":""},{"key":"expiresAt","label":"Geçerlilik Bitiş (ISO tarih)","type":"text","required":false,"defaultValue":""}]"""),
            ("apply_capability_template", "Yetki Şablonu Uygula", "Kullanıcı Yönetimi", "📋",
             "Bir yetki şablonunun en son yayınlanan versiyonunu hedef kullanıcıya uygular.",
             """[{"key":"userTarget","label":"Hedef Kullanıcı","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"target_user"},{"key":"customUserId","label":"Kullanıcı ID (userTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"templateId","label":"Yetki Şablonu","type":"template-select","required":true,"defaultValue":""},{"key":"reason","label":"Sebep","type":"text","required":false,"defaultValue":""},{"key":"expiresAt","label":"Geçerlilik Bitiş (ISO tarih)","type":"text","required":false,"defaultValue":""}]"""),

            // ── Problem Yönetimi ─────────────────────────────────────────────────
            ("assign_problem_institution", "Problemi Kuruma Ata", "Problem Yönetimi", "🏛️",
             "Problemi belirtilen kurumun sorumluluğuna atar.",
             """[{"key":"problemTarget","label":"Hedef Problem","type":"select","options":["context_problem","custom"],"required":true,"defaultValue":"context_problem"},{"key":"customProblemId","label":"Problem ID (problemTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"institutionId","label":"Kurum ID","type":"number","required":true,"defaultValue":""}]"""),
            ("change_problem_status", "Problem Durumunu Değiştir", "Problem Yönetimi", "🔄",
             "Problemin çözüm, öne çıkarma veya raporlanma durumunu açar/kapatır.",
             """[{"key":"problemTarget","label":"Hedef Problem","type":"select","options":["context_problem","custom"],"required":true,"defaultValue":"context_problem"},{"key":"customProblemId","label":"Problem ID (problemTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"status","label":"Durum","type":"select","options":["resolved","highlighted","reported"],"required":true,"defaultValue":"resolved"},{"key":"value","label":"Değer","type":"boolean","required":true,"defaultValue":"true"}]"""),
            ("resolve_problem", "Problemi Çöz", "Problem Yönetimi", "✔️",
             "Problemi çözüldü olarak işaretler; isteğe bağlı olarak problem sahibine bildirim gönderilir.",
             """[{"key":"problemTarget","label":"Hedef Problem","type":"select","options":["context_problem","custom"],"required":true,"defaultValue":"context_problem"},{"key":"customProblemId","label":"Problem ID (problemTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"notifyOwner","label":"Sahibini Bildir","type":"boolean","required":false,"defaultValue":"true"}]"""),
            ("highlight_problem", "Problemi Öne Çıkar", "Problem Yönetimi", "⭐",
             "Problemin öne çıkarma durumunu açar/kapatır (toggle).",
             """[{"key":"problemTarget","label":"Hedef Problem","type":"select","options":["context_problem","custom"],"required":true,"defaultValue":"context_problem"},{"key":"customProblemId","label":"Problem ID (problemTarget=custom)","type":"text","required":false,"defaultValue":""}]"""),
            ("delete_problem", "Problemi Sil", "Problem Yönetimi", "🗑️",
             "Problemi sistemden kalıcı olarak kaldırır; isteğe bağlı sebep ve sahip bildirimi eklenebilir.",
             """[{"key":"problemTarget","label":"Hedef Problem","type":"select","options":["context_problem","custom"],"required":true,"defaultValue":"context_problem"},{"key":"customProblemId","label":"Problem ID (problemTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"reason","label":"Silme Sebebi","type":"text","required":false,"defaultValue":""},{"key":"notifyOwner","label":"Sahibini Bildir","type":"boolean","required":false,"defaultValue":"true"}]"""),
            ("report_problem", "Problemi Raporla", "Problem Yönetimi", "🚩",
             "Problemi moderasyon incelemesi için raporlar.",
             """[{"key":"problemTarget","label":"Hedef Problem","type":"select","options":["context_problem","custom"],"required":true,"defaultValue":"context_problem"},{"key":"customProblemId","label":"Problem ID (problemTarget=custom)","type":"text","required":false,"defaultValue":""}]"""),

            // ── Çözüm Yönetimi ───────────────────────────────────────────────────
            ("approve_solution", "Çözümü Onayla", "Çözüm Yönetimi", "✅",
             "Uzman onayı bekleyen çözümü onaylar ve yazara bildirim gönderir.",
             """[{"key":"solutionTarget","label":"Hedef Çözüm","type":"select","options":["context_solution","custom"],"required":true,"defaultValue":"context_solution"},{"key":"customSolutionId","label":"Çözüm ID (solutionTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"notifyAuthor","label":"Yazarı Bildir","type":"boolean","required":false,"defaultValue":"true"}]"""),
            ("reject_solution", "Çözümü Reddet", "Çözüm Yönetimi", "❌",
             "Uzman incelemesinden geçemeyen çözümü reddeder; yazara sebep bildirilir.",
             """[{"key":"solutionTarget","label":"Hedef Çözüm","type":"select","options":["context_solution","custom"],"required":true,"defaultValue":"context_solution"},{"key":"customSolutionId","label":"Çözüm ID (solutionTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"reason","label":"Reddetme Sebebi","type":"text","required":false,"defaultValue":""},{"key":"notifyAuthor","label":"Yazarı Bildir","type":"boolean","required":false,"defaultValue":"true"}]"""),
            ("highlight_solution", "Çözümü Öne Çıkar", "Çözüm Yönetimi", "💡",
             "Çözümün öne çıkarma durumunu açar/kapatır (toggle).",
             """[{"key":"solutionTarget","label":"Hedef Çözüm","type":"select","options":["context_solution","custom"],"required":true,"defaultValue":"context_solution"},{"key":"customSolutionId","label":"Çözüm ID (solutionTarget=custom)","type":"text","required":false,"defaultValue":""}]"""),
            ("delete_solution", "Çözümü Sil", "Çözüm Yönetimi", "🗑️",
             "Çözümü sistemden kaldırır; isteğe bağlı sebep ve yazar bildirimi eklenebilir.",
             """[{"key":"solutionTarget","label":"Hedef Çözüm","type":"select","options":["context_solution","custom"],"required":true,"defaultValue":"context_solution"},{"key":"customSolutionId","label":"Çözüm ID (solutionTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"reason","label":"Silme Sebebi","type":"text","required":false,"defaultValue":""},{"key":"notifyAuthor","label":"Yazarı Bildir","type":"boolean","required":false,"defaultValue":"true"}]"""),

            // ── Moderasyon ───────────────────────────────────────────────────────
            ("delete_comment", "Yorumu Sil", "Moderasyon", "🧹",
             "Belirtilen yorumu moderasyon gerekçesiyle siler.",
             """[{"key":"commentTarget","label":"Hedef Yorum","type":"select","options":["context_comment","custom"],"required":true,"defaultValue":"context_comment"},{"key":"customCommentId","label":"Yorum ID (commentTarget=custom)","type":"text","required":false,"defaultValue":""},{"key":"reason","label":"Silme Sebebi","type":"text","required":false,"defaultValue":""}]"""),

            // ── Sistem ───────────────────────────────────────────────────────────
            ("log_event", "Olay Kaydet", "Sistem", "📝",
             "Audit log tablosuna özelleştirilebilir kategori ve seviyede kayıt ekler.",
             """[{"key":"category","label":"Kategori","type":"text","required":false,"defaultValue":"Workflow"},{"key":"action","label":"Eylem","type":"text","required":false,"defaultValue":""},{"key":"message","label":"Mesaj","type":"text","required":true,"defaultValue":""},{"key":"details","label":"Detaylar","type":"text","required":false,"defaultValue":""},{"key":"severity","label":"Seviye","type":"select","options":["Info","Warning","Error","Critical"],"required":true,"defaultValue":"Info"}]"""),
            ("webhook", "Webhook Tetikle", "Sistem", "🌐",
             "Dış servise HTTP isteği gönderir; payload boş bırakılırsa context otomatik eklenir.",
             """[{"key":"url","label":"Webhook URL","type":"text","required":true,"defaultValue":"https://"},{"key":"method","label":"HTTP Metodu","type":"select","options":["POST","GET","PUT","PATCH"],"required":true,"defaultValue":"POST"},{"key":"payload","label":"Payload JSON (boş=otomatik)","type":"text","required":false,"defaultValue":""},{"key":"authHeader","label":"Authorization Header","type":"text","required":false,"defaultValue":""}]"""),
            ("create_announcement", "Duyuru Oluştur", "Sistem", "📢",
             "Platforma kalıcı duyuru ekler; isteğe bağlı olarak ilgili kullanıcılara bildirim gönderir.",
             """[{"key":"title","label":"Başlık","type":"text","required":true,"defaultValue":""},{"key":"content","label":"İçerik","type":"text","required":true,"defaultValue":""},{"key":"targetGroup","label":"Hedef Grup","type":"select","options":["all","registered","institution"],"required":false,"defaultValue":"all"},{"key":"institutionId","label":"Kurum ID (targetGroup=institution)","type":"text","required":false,"defaultValue":""},{"key":"link","label":"Bağlantı URL (opsiyonel)","type":"text","required":false,"defaultValue":""},{"key":"expiresAt","label":"Geçerlilik Bitiş (ISO tarih)","type":"text","required":false,"defaultValue":""},{"key":"sendNotification","label":"Bildirim de Gönder","type":"boolean","required":false,"defaultValue":"false"}]"""),
            ("trigger_workflow", "Workflow Tetikle", "Sistem", "⚡",
             "Başka bir workflow definition'ı çalıştırır; maksimum 3 zincir derinliği.",
             """[{"key":"workflowDefinitionId","label":"Workflow Definition ID","type":"number","required":true,"defaultValue":""},{"key":"inheritContext","label":"Context'i Devral","type":"boolean","required":false,"defaultValue":"true"}]"""),
        };

        // Upsert: her action'ı ActionCode'a göre bul; varsa güncelle, yoksa ekle
        foreach (var (code, name, cat, icon, desc, schema) in actionDefs)
        {
            var existing = context.WorkflowActions.FirstOrDefault(a => a.ActionCode == code);
            if (existing != null)
            {
                existing.Name                 = name;
                existing.Category             = cat;
                existing.Icon                 = icon;
                existing.Description          = desc;
                existing.ParametersSchemaJson = schema;
                existing.IsActive             = true;
            }
            else
            {
                context.WorkflowActions.Add(new WorkflowAction
                {
                    Name = name, ActionCode = code, Category = cat,
                    Icon = icon, Description = desc,
                    ParametersSchemaJson = schema, IsActive = true,
                });
            }
        }
        context.SaveChanges();

        // Artık kullanılmayan eski seeder bloğu buradan devam eder:
        if (false)
        {
            var actions = new List<WorkflowAction>
            {
                // ── İletişim ─────────────────────────────────────────────────────────
                new()
                {
                    Name = "Send Email",
                    ActionCode = "send_email",
                    ParametersSchemaJson = """
                    [
                      {"key":"recipient","label":"Alıcı Tipi","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"context_user"},
                      {"key":"customTo","label":"Özel E-posta Adresi","type":"text","required":false,"defaultValue":""},
                      {"key":"templateKey","label":"E-posta Şablonu Anahtarı","type":"text","required":false,"defaultValue":""},
                      {"key":"subject","label":"Konu (şablon yoksa)","type":"text","required":false,"defaultValue":""},
                      {"key":"body","label":"İçerik (şablon yoksa)","type":"text","required":false,"defaultValue":""},
                      {"key":"cc","label":"CC Adresleri (virgülle ayrılmış)","type":"text","required":false,"defaultValue":""}
                    ]
                    """,
                    IsActive = true
                },
                new()
                {
                    Name = "Send Notification",
                    ActionCode = "send_notification",
                    ParametersSchemaJson = """
                    [
                      {"key":"recipientType","label":"Alıcı Tipi","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"context_user"},
                      {"key":"customUserId","label":"Kullanıcı ID (custom ise)","type":"text","required":false,"defaultValue":""},
                      {"key":"title","label":"Başlık","type":"text","required":true,"defaultValue":""},
                      {"key":"message","label":"Mesaj","type":"text","required":true,"defaultValue":""},
                      {"key":"type","label":"Tür","type":"select","options":["info","success","warning","error"],"required":true,"defaultValue":"info"},
                      {"key":"referenceLink","label":"Bağlantı (opsiyonel)","type":"text","required":false,"defaultValue":""}
                    ]
                    """,
                    IsActive = true
                },
                new()
                {
                    Name = "Send Bulk Notification",
                    ActionCode = "send_bulk_notification",
                    ParametersSchemaJson = """
                    [
                      {"key":"targetGroup","label":"Hedef Grup","type":"select","options":["institution","role"],"required":true,"defaultValue":"institution"},
                      {"key":"role","label":"Rol (targetGroup=role ise)","type":"select","options":["User","Admin","Expert","Official"],"required":false,"defaultValue":"User"},
                      {"key":"title","label":"Başlık","type":"text","required":true,"defaultValue":""},
                      {"key":"message","label":"Mesaj","type":"text","required":true,"defaultValue":""},
                      {"key":"type","label":"Tür","type":"select","options":["info","success","warning","error"],"required":true,"defaultValue":"info"}
                    ]
                    """,
                    IsActive = true
                },

                // ── Kullanıcı Yönetimi ────────────────────────────────────────────────
                new()
                {
                    Name = "Ban User",
                    ActionCode = "ban_user",
                    ParametersSchemaJson = """
                    [
                      {"key":"userTarget","label":"Hedef Kullanıcı","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"target_user"},
                      {"key":"customUserId","label":"Kullanıcı ID (custom ise)","type":"text","required":false,"defaultValue":""},
                      {"key":"durationDays","label":"Süre (gün, 0=kalıcı)","type":"number","required":true,"defaultValue":"7"},
                      {"key":"reason","label":"Sebep","type":"text","required":false,"defaultValue":""},
                      {"key":"notifyUser","label":"Kullanıcıyı Bildir","type":"boolean","required":false,"defaultValue":"true"}
                    ]
                    """,
                    IsActive = true
                },
                new()
                {
                    Name = "Unban User",
                    ActionCode = "unban_user",
                    ParametersSchemaJson = """
                    [
                      {"key":"userTarget","label":"Hedef Kullanıcı","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"target_user"},
                      {"key":"customUserId","label":"Kullanıcı ID (custom ise)","type":"text","required":false,"defaultValue":""},
                      {"key":"notifyUser","label":"Kullanıcıyı Bildir","type":"boolean","required":false,"defaultValue":"true"}
                    ]
                    """,
                    IsActive = true
                },
                new()
                {
                    Name = "Warn User",
                    ActionCode = "warn_user",
                    ParametersSchemaJson = """
                    [
                      {"key":"userTarget","label":"Hedef Kullanıcı","type":"select","options":["context_user","target_user","custom"],"required":true,"defaultValue":"target_user"},
                      {"key":"customUserId","label":"Kullanıcı ID (custom ise)","type":"text","required":false,"defaultValue":""},
                      {"key":"title","label":"Uyarı Başlığı","type":"text","required":true,"defaultValue":"Kural İhlali"},
                      {"key":"message","label":"Uyarı Mesajı","type":"text","required":true,"defaultValue":""},
                      {"key":"severity","label":"Ağırlık","type":"select","options":["low","medium","high"],"required":true,"defaultValue":"medium"}
                    ]
                    """,
                    IsActive = true
                },

                // ── Problem Yönetimi ─────────────────────────────────────────────────
                new()
                {
                    Name = "Resolve Problem",
                    ActionCode = "resolve_problem",
                    ParametersSchemaJson = """
                    [
                      {"key":"problemTarget","label":"Hedef Problem","type":"select","options":["context_problem","custom"],"required":true,"defaultValue":"context_problem"},
                      {"key":"customProblemId","label":"Problem ID (custom ise)","type":"text","required":false,"defaultValue":""},
                      {"key":"notifyOwner","label":"Sahibini Bildir","type":"boolean","required":false,"defaultValue":"true"}
                    ]
                    """,
                    IsActive = true
                },
                new()
                {
                    Name = "Highlight Problem",
                    ActionCode = "highlight_problem",
                    ParametersSchemaJson = """
                    [
                      {"key":"problemTarget","label":"Hedef Problem","type":"select","options":["context_problem","custom"],"required":true,"defaultValue":"context_problem"},
                      {"key":"customProblemId","label":"Problem ID (custom ise)","type":"text","required":false,"defaultValue":""}
                    ]
                    """,
                    IsActive = true
                },
                new()
                {
                    Name = "Delete Problem",
                    ActionCode = "delete_problem",
                    ParametersSchemaJson = """
                    [
                      {"key":"problemTarget","label":"Hedef Problem","type":"select","options":["context_problem","custom"],"required":true,"defaultValue":"context_problem"},
                      {"key":"customProblemId","label":"Problem ID (custom ise)","type":"text","required":false,"defaultValue":""},
                      {"key":"reason","label":"Silme Sebebi","type":"text","required":false,"defaultValue":""},
                      {"key":"notifyOwner","label":"Sahibini Bildir","type":"boolean","required":false,"defaultValue":"true"}
                    ]
                    """,
                    IsActive = true
                },
                new()
                {
                    Name = "Report Problem",
                    ActionCode = "report_problem",
                    ParametersSchemaJson = """
                    [
                      {"key":"problemTarget","label":"Hedef Problem","type":"select","options":["context_problem","custom"],"required":true,"defaultValue":"context_problem"},
                      {"key":"customProblemId","label":"Problem ID (custom ise)","type":"text","required":false,"defaultValue":""}
                    ]
                    """,
                    IsActive = true
                },

                // ── Çözüm Yönetimi ───────────────────────────────────────────────────
                new()
                {
                    Name = "Approve Solution",
                    ActionCode = "approve_solution",
                    ParametersSchemaJson = """
                    [
                      {"key":"solutionTarget","label":"Hedef Çözüm","type":"select","options":["context_solution","custom"],"required":true,"defaultValue":"context_solution"},
                      {"key":"customSolutionId","label":"Çözüm ID (custom ise)","type":"text","required":false,"defaultValue":""},
                      {"key":"notifyAuthor","label":"Yazarı Bildir","type":"boolean","required":false,"defaultValue":"true"}
                    ]
                    """,
                    IsActive = true
                },
                new()
                {
                    Name = "Reject Solution",
                    ActionCode = "reject_solution",
                    ParametersSchemaJson = """
                    [
                      {"key":"solutionTarget","label":"Hedef Çözüm","type":"select","options":["context_solution","custom"],"required":true,"defaultValue":"context_solution"},
                      {"key":"customSolutionId","label":"Çözüm ID (custom ise)","type":"text","required":false,"defaultValue":""},
                      {"key":"reason","label":"Reddetme Sebebi","type":"text","required":false,"defaultValue":""},
                      {"key":"notifyAuthor","label":"Yazarı Bildir","type":"boolean","required":false,"defaultValue":"true"}
                    ]
                    """,
                    IsActive = true
                },
                new()
                {
                    Name = "Highlight Solution",
                    ActionCode = "highlight_solution",
                    ParametersSchemaJson = """
                    [
                      {"key":"solutionTarget","label":"Hedef Çözüm","type":"select","options":["context_solution","custom"],"required":true,"defaultValue":"context_solution"},
                      {"key":"customSolutionId","label":"Çözüm ID (custom ise)","type":"text","required":false,"defaultValue":""}
                    ]
                    """,
                    IsActive = true
                },
                new()
                {
                    Name = "Delete Solution",
                    ActionCode = "delete_solution",
                    ParametersSchemaJson = """
                    [
                      {"key":"solutionTarget","label":"Hedef Çözüm","type":"select","options":["context_solution","custom"],"required":true,"defaultValue":"context_solution"},
                      {"key":"customSolutionId","label":"Çözüm ID (custom ise)","type":"text","required":false,"defaultValue":""},
                      {"key":"reason","label":"Silme Sebebi","type":"text","required":false,"defaultValue":""},
                      {"key":"notifyAuthor","label":"Yazarı Bildir","type":"boolean","required":false,"defaultValue":"true"}
                    ]
                    """,
                    IsActive = true
                },

                // ── Moderasyon ───────────────────────────────────────────────────────
                new()
                {
                    Name = "Delete Comment",
                    ActionCode = "delete_comment",
                    ParametersSchemaJson = """
                    [
                      {"key":"commentTarget","label":"Hedef Yorum","type":"select","options":["context_comment","custom"],"required":true,"defaultValue":"context_comment"},
                      {"key":"customCommentId","label":"Yorum ID (custom ise)","type":"text","required":false,"defaultValue":""},
                      {"key":"reason","label":"Silme Sebebi","type":"text","required":false,"defaultValue":""}
                    ]
                    """,
                    IsActive = true
                },

                // ── Sistem ───────────────────────────────────────────────────────────
                new()
                {
                    Name = "Log Event",
                    ActionCode = "log_event",
                    ParametersSchemaJson = """
                    [
                      {"key":"category","label":"Kategori","type":"text","required":false,"defaultValue":"Workflow"},
                      {"key":"action","label":"Eylem","type":"text","required":false,"defaultValue":""},
                      {"key":"message","label":"Mesaj","type":"text","required":true,"defaultValue":""},
                      {"key":"details","label":"Detaylar","type":"text","required":false,"defaultValue":""},
                      {"key":"severity","label":"Seviye","type":"select","options":["Info","Warning","Error","Critical"],"required":true,"defaultValue":"Info"}
                    ]
                    """,
                    IsActive = true
                },
                new()
                {
                    Name = "Webhook",
                    ActionCode = "webhook",
                    ParametersSchemaJson = """
                    [
                      {"key":"url","label":"Webhook URL","type":"text","required":true,"defaultValue":"https://"},
                      {"key":"method","label":"HTTP Metodu","type":"select","options":["POST","GET","PUT","PATCH"],"required":true,"defaultValue":"POST"},
                      {"key":"payload","label":"Payload (JSON, boş=otomatik)","type":"text","required":false,"defaultValue":""},
                      {"key":"authHeader","label":"Authorization Header","type":"text","required":false,"defaultValue":""}
                    ]
                    """,
                    IsActive = true
                },
            };
            // (eski blok artık çalışmaz — if (false) koruması var)
        }
    }
}
