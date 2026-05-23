using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Business.Abstract;
using Business.Models;
using Core.Entities.Concrete;
using Core.Utilities.Authorization;
using Core.Utilities.Helpers.Email;
using Core.Utilities.Results;
using Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace Business.Concrete;

public partial class WorkflowActionDispatcher : IWorkflowActionDispatcher
{
    private readonly IEmailHelper _emailHelper;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;
    private readonly IUserWarningService _userWarningService;
    private readonly IProblemService _problemService;
    private readonly ISolutionService _solutionService;
    private readonly ICommentService _commentService;
    private readonly ILogService _logService;
    private readonly IWebhookClient _webhookClient;
    private readonly ICapabilityResolver _capabilityResolver;
    private readonly ILogger<WorkflowActionDispatcher> _logger;

    [GeneratedRegex(@"\{([^}]+)\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderPattern();

    public WorkflowActionDispatcher(
        IEmailHelper emailHelper,
        IEmailTemplateService emailTemplateService,
        INotificationService notificationService,
        IUserService userService,
        IUserWarningService userWarningService,
        IProblemService problemService,
        ISolutionService solutionService,
        ICommentService commentService,
        ILogService logService,
        IWebhookClient webhookClient,
        ICapabilityResolver capabilityResolver,
        ILogger<WorkflowActionDispatcher> logger)
    {
        _emailHelper = emailHelper;
        _emailTemplateService = emailTemplateService;
        _notificationService = notificationService;
        _userService = userService;
        _userWarningService = userWarningService;
        _problemService = problemService;
        _solutionService = solutionService;
        _commentService = commentService;
        _logService = logService;
        _webhookClient = webhookClient;
        _capabilityResolver = capabilityResolver;
        _logger = logger;
    }

    public async Task<IDataResult<object?>> DispatchAsync(
        string actionCode,
        Dictionary<string, string> parameters,
        RuleContext context)
    {
        _logger.LogInformation(
            "[WorkflowActionDispatcher] Action={ActionCode}, UserId={UserId}, Trigger={Trigger}",
            actionCode, context.SystemUserId, context.TriggerEventName);

        var normalizedCode = actionCode.ToLowerInvariant().Trim();
        var capabilityCode = $"workflow.action.{normalizedCode}";
        if (!_capabilityResolver.Allows(context.SystemUserId, capabilityCode))
        {
            _logger.LogWarning(
                "[WorkflowActionDispatcher] capability_denied: {CapabilityCode} for UserId={UserId}",
                capabilityCode, context.SystemUserId);
            return new ErrorDataResult<object?>(null, $"Yetkisiz action: '{capabilityCode}'");
        }

        try
        {
            return normalizedCode switch
            {
                // İletişim
                "send_email"             => await HandleSendEmailAsync(parameters, context),
                "send_notification"      => await HandleSendNotificationAsync(parameters, context),
                "send_bulk_notification" => await HandleSendBulkNotificationAsync(parameters, context),

                // Kullanıcı Yönetimi
                "ban_user"               => await HandleBanUserAsync(parameters, context),
                "unban_user"             => await HandleUnbanUserAsync(parameters, context),
                "warn_user"              => await HandleWarnUserAsync(parameters, context),
                "change_user_role"       => await HandleChangeUserRoleAsync(parameters, context),

                // Problem Yönetimi
                "resolve_problem"        => await HandleResolveProblemAsync(parameters, context),
                "highlight_problem"      => await HandleHighlightProblemAsync(parameters, context),
                "delete_problem"         => await HandleDeleteProblemAsync(parameters, context),
                "report_problem"         => await HandleReportProblemAsync(parameters, context),

                // Çözüm Yönetimi
                "approve_solution"       => await HandleApproveSolutionAsync(parameters, context),
                "reject_solution"        => await HandleRejectSolutionAsync(parameters, context),
                "highlight_solution"     => await HandleHighlightSolutionAsync(parameters, context),
                "delete_solution"        => await HandleDeleteSolutionAsync(parameters, context),

                // Moderasyon
                "delete_comment"         => await HandleDeleteCommentAsync(parameters, context),

                // Sistem
                "log_event"              => await HandleLogEventAsync(parameters, context),
                "webhook"                => await HandleWebhookAsync(parameters, context),

                _ => new ErrorDataResult<object?>(null, $"Bilinmeyen aksiyon kodu: '{normalizedCode}'")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[WorkflowActionDispatcher] Action={ActionCode} çalıştırılırken hata oluştu.", actionCode);
            return new ErrorDataResult<object?>(null, $"Aksiyon hatası ({actionCode}): {ex.Message}");
        }
    }

    // ── İLETİŞİM ─────────────────────────────────────────────────────────────────

    // send_email
    // Params: recipient (context_user|target_user|custom), customTo, templateKey,
    //         subject, body, cc
    private Task<IDataResult<object?>> HandleSendEmailAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var to = ResolveEmailAddress(parameters, context);
        if (string.IsNullOrWhiteSpace(to))
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "send_email: alıcı e-posta adresi çözülemedi."));

        var (subject, body) = ResolveEmailContent(parameters, context);

        var result = _emailHelper.Send(to, subject, body);

        // Opsiyonel CC adresleri
        var cc = Resolve(parameters.GetValueOrDefault("cc"), context);
        if (result.Success && !string.IsNullOrWhiteSpace(cc))
        {
            foreach (var addr in cc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                _emailHelper.Send(addr, subject, body);
        }

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { to, subject }, "E-posta gönderildi.")
            : new ErrorDataResult<object?>(null, $"E-posta gönderilemedi: {result.Message}"));
    }

    // send_notification
    // Params: recipientType (context_user|target_user|custom), customUserId,
    //         title, message, type (info|success|warning|error), referenceLink
    private Task<IDataResult<object?>> HandleSendNotificationAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var userId = ResolveUserId(parameters, context, "recipientType", "customUserId");

        var notification = new Notification
        {
            UserId        = userId,
            Title         = Resolve(parameters.GetValueOrDefault("title"),   context),
            Message       = Resolve(parameters.GetValueOrDefault("message"), context),
            Type          = parameters.GetValueOrDefault("type") ?? "info",
            ReferenceLink = parameters.TryGetValue("referenceLink", out var link) ? Resolve(link, context) : null,
            IsRead        = false,
            CreatedAt     = DateTime.UtcNow
        };

        var result = _notificationService.Add(notification);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { userId, notification.Title }, "Bildirim oluşturuldu.")
            : new ErrorDataResult<object?>(null, $"Bildirim eklenemedi: {result.Message}"));
    }

    // send_bulk_notification
    // Params: targetGroup (institution|role), role (User|Admin|Expert|Official),
    //         title, message, type
    private async Task<IDataResult<object?>> HandleSendBulkNotificationAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var targetGroup = parameters.GetValueOrDefault("targetGroup") ?? "institution";
        var roleFilter  = parameters.GetValueOrDefault("role");
        var title       = Resolve(parameters.GetValueOrDefault("title"),   context);
        var message     = Resolve(parameters.GetValueOrDefault("message"), context);
        var type        = parameters.GetValueOrDefault("type") ?? "info";

        var allUsersResult = _userService.GetAll();
        if (!allUsersResult.Success || allUsersResult.Data == null)
            return new ErrorDataResult<object?>(null, "Kullanıcı listesi alınamadı.");

        var users = allUsersResult.Data
            .Where(u => !u.IsBanned && !u.IsDeleted)
            .Where(u => context.InstitutionId == null || u.InstitutionId == context.InstitutionId);

        // Rol bazlı filtreleme kaldırıldı — capability sistemi kullanılır.
        // targetGroup == "role" için tüm aktif kullanıcılar hedef alınır.

        var userList = users.ToList();
        int sent = 0;

        foreach (var user in userList)
        {
            var n = new Notification
            {
                UserId        = user.Id,
                Title         = title,
                Message       = message,
                Type          = type,
                IsRead        = false,
                CreatedAt     = DateTime.UtcNow
            };
            if (_notificationService.Add(n).Success) sent++;
            
            // Thread starving'i engellemek icin her 50 kayitta threadi serbest birak
            if (sent % 50 == 0) await Task.Yield();
        }

        return new SuccessDataResult<object?>(
            new { sent, total = userList.Count },
            $"Toplu bildirim gönderildi: {sent}/{userList.Count}");
    }

    // ── KULLANICI YÖNETİMİ ────────────────────────────────────────────────────────

    // ban_user
    // Params: userTarget (context_user|target_user|custom), customUserId,
    //         durationDays (0=kalıcı), reason, notifyUser (true|false)
    private Task<IDataResult<object?>> HandleBanUserAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var userId   = ResolveUserId(parameters, context);
        var reason   = Resolve(parameters.GetValueOrDefault("reason"), context);
        var duration = parameters.TryGetValue("durationDays", out var d) && int.TryParse(d, out var days) ? days : 0;
        var notify   = parameters.GetValueOrDefault("notifyUser") != "false";

        var result = _userService.BanUser(userId);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Kullanıcı banlanamadı: {result.Message}"));

        if (notify)
        {
            var banMessage = duration > 0
                ? $"Hesabınız {duration} gün boyunca askıya alınmıştır. Sebep: {reason}"
                : $"Hesabınız kalıcı olarak askıya alınmıştır. Sebep: {reason}";

            _notificationService.Add(new Notification
            {
                UserId    = userId,
                Title     = "Hesabınız Askıya Alındı",
                Message   = banMessage,
                Type      = "error",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { userId, reason, duration }, $"Kullanıcı banlandı (ID: {userId})."));
    }

    // unban_user
    // Params: userTarget (context_user|target_user|custom), customUserId, notifyUser
    private Task<IDataResult<object?>> HandleUnbanUserAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var userId = ResolveUserId(parameters, context);
        var notify = parameters.GetValueOrDefault("notifyUser") != "false";

        var result = _userService.UnbanUser(userId);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Ban kaldırılamadı: {result.Message}"));

        if (notify)
        {
            _notificationService.Add(new Notification
            {
                UserId    = userId,
                Title     = "Hesabınız Yeniden Aktif",
                Message   = "Hesabınızdaki askıya alma işlemi kaldırılmıştır. Platformumuzu tekrar kullanabilirsiniz.",
                Type      = "success",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { userId }, $"Kullanıcı yasağı kaldırıldı (ID: {userId})."));
    }

    // warn_user
    // Params: userTarget, customUserId, title, message, severity (low|medium|high)
    private Task<IDataResult<object?>> HandleWarnUserAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var userId   = ResolveUserId(parameters, context);
        var title    = Resolve(parameters.GetValueOrDefault("title"),    context);
        var message  = Resolve(parameters.GetValueOrDefault("message"),  context);
        var severity = parameters.GetValueOrDefault("severity") ?? "medium";

        var warning = new UserWarning
        {
            UserId           = userId,
            IssuedByAdminId  = context.SystemUserId,
            Title            = string.IsNullOrWhiteSpace(title)   ? "Uyarı" : title,
            Message          = string.IsNullOrWhiteSpace(message) ? context.TriggerEventName : message,
            Severity         = severity,
            IsActive         = true,
            IssuedAt         = DateTime.UtcNow
        };

        var result = _userWarningService.Issue(warning);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Uyarı verilemedi: {result.Message}"));

        _notificationService.Add(new Notification
        {
            UserId    = userId,
            Title     = $"Uyarı: {warning.Title}",
            Message   = warning.Message,
            Type      = severity == "high" ? "error" : "warning",
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { userId, severity }, $"Kullanıcıya uyarı verildi (ID: {userId})."));
    }

    // change_user_role — rol bayrakları kaldırıldı, capability sistemi kullanılır.
    // Workflow üzerinden rol değişikliği artık desteklenmiyor;
    // /api/users/{id}/capabilities/grant veya /revoke kullanın.
    private Task<IDataResult<object?>> HandleChangeUserRoleAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        _logger.LogWarning("[Dispatcher] change_user_role action kaldırıldı. Capability sistemi kullanın.");
        return Task.FromResult<IDataResult<object?>>(
            new ErrorDataResult<object?>(null, "change_user_role kaldırıldı. Capability grant/revoke API kullanın."));
    }

    // ── PROBLEM YÖNETİMİ ──────────────────────────────────────────────────────────

    // resolve_problem
    // Params: problemTarget (context_problem|custom), customProblemId, notifyOwner (true|false)
    private Task<IDataResult<object?>> HandleResolveProblemAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var problemId = ResolveProblemId(parameters, context);
        if (problemId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "resolve_problem: problem ID çözülemedi."));

        var result = _problemService.ResolveProblem(problemId.Value);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Problem çözülemedi: {result.Message}"));

        var notify = parameters.GetValueOrDefault("notifyOwner") != "false";
        if (notify)
        {
            var problem = _problemService.GetById(problemId.Value);
            if (problem.Success && problem.Data != null)
            {
                _notificationService.Add(new Notification
                {
                    UserId    = problem.Data.SenderId,
                    Title     = "Probleminiz Çözüldü",
                    Message   = $"'{problem.Data.Title}' başlıklı probleminiz çözüldü olarak işaretlendi.",
                    Type      = "success",
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { problemId }, $"Problem çözüldü (ID: {problemId})."));
    }

    // highlight_problem
    // Params: problemTarget, customProblemId
    private Task<IDataResult<object?>> HandleHighlightProblemAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var problemId = ResolveProblemId(parameters, context);
        if (problemId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "highlight_problem: problem ID çözülemedi."));

        var result = _problemService.ToggleHighlight(problemId.Value);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { problemId }, "Problem öne çıkarma durumu değiştirildi.")
            : new ErrorDataResult<object?>(null, $"İşlem başarısız: {result.Message}"));
    }

    // delete_problem
    // Params: problemTarget, customProblemId, reason, notifyOwner
    private Task<IDataResult<object?>> HandleDeleteProblemAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var problemId = ResolveProblemId(parameters, context);
        if (problemId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "delete_problem: problem ID çözülemedi."));

        var reason = Resolve(parameters.GetValueOrDefault("reason"), context);
        var notify = parameters.GetValueOrDefault("notifyOwner") != "false";

        int? ownerId = null;
        string? problemTitle = null;

        if (notify)
        {
            var problem = _problemService.GetById(problemId.Value);
            if (problem.Success && problem.Data != null)
            {
                ownerId      = problem.Data.SenderId;
                problemTitle = problem.Data.Title;
            }
        }

        var result = _problemService.Delete(problemId.Value);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Problem silinemedi: {result.Message}"));

        if (notify && ownerId.HasValue)
        {
            _notificationService.Add(new Notification
            {
                UserId    = ownerId.Value,
                Title     = "Probleminiz Silindi",
                Message   = string.IsNullOrWhiteSpace(reason)
                    ? $"'{problemTitle}' başlıklı probleminiz kaldırıldı."
                    : $"'{problemTitle}' başlıklı probleminiz kaldırıldı. Sebep: {reason}",
                Type      = "warning",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { problemId, reason }, "Problem silindi."));
    }

    // report_problem
    // Params: problemTarget, customProblemId
    private Task<IDataResult<object?>> HandleReportProblemAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var problemId = ResolveProblemId(parameters, context);
        if (problemId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "report_problem: problem ID çözülemedi."));

        var result = _problemService.ReportProblem(problemId.Value);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { problemId }, "Problem raporlandı.")
            : new ErrorDataResult<object?>(null, $"Raporlama başarısız: {result.Message}"));
    }

    // ── ÇÖZÜM YÖNETİMİ ───────────────────────────────────────────────────────────

    // approve_solution
    // Params: solutionTarget (context_solution|custom), customSolutionId, notifyAuthor
    private Task<IDataResult<object?>> HandleApproveSolutionAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var solutionId = ResolveSolutionId(parameters, context);
        if (solutionId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "approve_solution: çözüm ID çözülemedi."));

        var result = _solutionService.ApproveSolution(solutionId.Value);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Çözüm onaylanamadı: {result.Message}"));

        var notify = parameters.GetValueOrDefault("notifyAuthor") != "false";
        if (notify)
        {
            var solution = _solutionService.GetById(solutionId.Value);
            if (solution.Success && solution.Data != null)
            {
                _notificationService.Add(new Notification
                {
                    UserId    = solution.Data.SenderId,
                    Title     = "Çözümünüz Onaylandı",
                    Message   = "Paylaştığınız çözüm uzman ekibimiz tarafından onaylandı.",
                    Type      = "success",
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { solutionId }, "Çözüm onaylandı."));
    }

    // reject_solution
    // Params: solutionTarget, customSolutionId, reason, notifyAuthor
    private Task<IDataResult<object?>> HandleRejectSolutionAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var solutionId = ResolveSolutionId(parameters, context);
        if (solutionId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "reject_solution: çözüm ID çözülemedi."));

        var reason = Resolve(parameters.GetValueOrDefault("reason"), context);
        var result = _solutionService.RejectSolution(solutionId.Value);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Çözüm reddedilemedi: {result.Message}"));

        var notify = parameters.GetValueOrDefault("notifyAuthor") != "false";
        if (notify)
        {
            var solution = _solutionService.GetById(solutionId.Value);
            if (solution.Success && solution.Data != null)
            {
                _notificationService.Add(new Notification
                {
                    UserId    = solution.Data.SenderId,
                    Title     = "Çözümünüz Reddedildi",
                    Message   = string.IsNullOrWhiteSpace(reason)
                        ? "Paylaştığınız çözüm uzman değerlendirmesinden geçemedi."
                        : $"Paylaştığınız çözüm reddedildi. Sebep: {reason}",
                    Type      = "warning",
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { solutionId, reason }, "Çözüm reddedildi."));
    }

    // highlight_solution
    // Params: solutionTarget, customSolutionId
    private Task<IDataResult<object?>> HandleHighlightSolutionAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var solutionId = ResolveSolutionId(parameters, context);
        if (solutionId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "highlight_solution: çözüm ID çözülemedi."));

        var result = _solutionService.ToggleHighlight(solutionId.Value);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { solutionId }, "Çözüm öne çıkarma durumu değiştirildi.")
            : new ErrorDataResult<object?>(null, $"İşlem başarısız: {result.Message}"));
    }

    // delete_solution
    // Params: solutionTarget, customSolutionId, reason, notifyAuthor
    private Task<IDataResult<object?>> HandleDeleteSolutionAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var solutionId = ResolveSolutionId(parameters, context);
        if (solutionId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "delete_solution: çözüm ID çözülemedi."));

        var reason = Resolve(parameters.GetValueOrDefault("reason"), context);
        var notify = parameters.GetValueOrDefault("notifyAuthor") != "false";

        int? authorId = null;
        if (notify)
        {
            var s = _solutionService.GetById(solutionId.Value);
            if (s.Success && s.Data != null) authorId = s.Data.SenderId;
        }

        var result = _solutionService.Delete(solutionId.Value);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Çözüm silinemedi: {result.Message}"));

        if (notify && authorId.HasValue)
        {
            _notificationService.Add(new Notification
            {
                UserId    = authorId.Value,
                Title     = "Çözümünüz Silindi",
                Message   = string.IsNullOrWhiteSpace(reason)
                    ? "Paylaştığınız bir çözüm kaldırıldı."
                    : $"Paylaştığınız çözüm kaldırıldı. Sebep: {reason}",
                Type      = "warning",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { solutionId, reason }, "Çözüm silindi."));
    }

    // ── MODERASYON ────────────────────────────────────────────────────────────────

    // delete_comment
    // Params: commentTarget (context_comment|custom), customCommentId, reason
    private Task<IDataResult<object?>> HandleDeleteCommentAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var commentTarget = parameters.GetValueOrDefault("commentTarget") ?? "context_comment";
        int? commentId = commentTarget == "custom"
            && int.TryParse(Resolve(parameters.GetValueOrDefault("customCommentId"), context), out var cid)
            ? cid
            : context.CommentId;

        if (commentId == null)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, "delete_comment: yorum ID çözülemedi."));

        var reason = Resolve(parameters.GetValueOrDefault("reason"), context);
        var result = _commentService.Delete(commentId.Value);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { commentId, reason }, "Yorum silindi.")
            : new ErrorDataResult<object?>(null, $"Yorum silinemedi: {result.Message}"));
    }

    // ── SİSTEM ────────────────────────────────────────────────────────────────────

    // log_event
    // Params: category, action, message, details, severity (Info|Warning|Error|Critical)
    private Task<IDataResult<object?>> HandleLogEventAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var category = Resolve(parameters.GetValueOrDefault("category"), context);
        var action   = Resolve(parameters.GetValueOrDefault("action"),   context);
        var message  = Resolve(parameters.GetValueOrDefault("message"),  context);
        var details  = parameters.TryGetValue("details",  out var d)   ? Resolve(d, context)   : null;
        var severity = parameters.TryGetValue("severity", out var sev) ? Resolve(sev, context) : "Info";

        var effectiveCategory = string.IsNullOrWhiteSpace(category) ? "Workflow" : category;
        var effectiveAction   = string.IsNullOrWhiteSpace(action)   ? context.TriggerEventName : action;

        switch ((severity ?? "Info").Trim().ToLowerInvariant())
        {
            case "warning":
                _logService.LogWarning(effectiveCategory, effectiveAction, message, details, context.InstitutionId);
                break;
            case "error":
                _logService.LogError(effectiveCategory, effectiveAction, message, details, context.InstitutionId);
                break;
            case "critical":
                _logService.LogCritical(effectiveCategory, effectiveAction, message, details, context.InstitutionId);
                break;
            default:
                _logService.LogInfo(effectiveCategory, effectiveAction, message, details, context.InstitutionId);
                break;
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { category, action, message, severity }, "Olay loglandı."));
    }

    // webhook
    // Params: url, method (POST|GET|PUT|PATCH), payload, authHeader
    // B9: Geçici hatalarda (5xx / 429 / network=StatusCode 0) exponential backoff
    // ile yeniden dener. 4xx (kalıcı istemci hatası) için retry yapılmaz.
    private const int WebhookMaxAttempts = 3;

    private async Task<IDataResult<object?>> HandleWebhookAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var url        = Resolve(parameters.GetValueOrDefault("url"),        context);
        var method     = (parameters.GetValueOrDefault("method") ?? "POST").ToUpperInvariant();
        var payload    = Resolve(parameters.GetValueOrDefault("payload"),    context);
        var authHeader = Resolve(parameters.GetValueOrDefault("authHeader"), context);

        if (string.IsNullOrWhiteSpace(url))
            return new ErrorDataResult<object?>(null, "webhook: 'url' parametresi boş.");

        try
        {
            // Otomatik context verisi payload'a ekle
            if (string.IsNullOrWhiteSpace(payload))
            {
                payload = JsonSerializer.Serialize(new
                {
                    trigger    = context.TriggerEventName,
                    userId     = context.SystemUserId,
                    targetUserId = context.TargetUserId,
                    problemId  = context.ProblemId,
                    solutionId = context.SolutionId,
                    timestamp  = context.ExecutedAt
                });
            }

            WebhookSendResult? result = null;
            for (var attempt = 1; attempt <= WebhookMaxAttempts; attempt++)
            {
                result = await _webhookClient.SendAsync(url, method, payload, authHeader);

                if (result.Success)
                {
                    return new SuccessDataResult<object?>(
                        new { url, method, status = result.StatusCode, attempts = attempt },
                        attempt > 1
                            ? $"Webhook {attempt}. denemede tetiklendi."
                            : "Webhook başarıyla tetiklendi.");
                }

                // Kalıcı hata (4xx) → retry anlamsız, hemen çık.
                if (!IsTransientWebhookFailure(result.StatusCode))
                    break;

                // Son deneme değilse exponential backoff ile bekle (200ms, 600ms).
                if (attempt < WebhookMaxAttempts)
                {
                    var delayMs = 200 * (int)Math.Pow(3, attempt - 1);
                    await Task.Delay(delayMs);
                }
            }

            return new ErrorDataResult<object?>(
                null,
                $"Webhook başarısız ({WebhookMaxAttempts} deneme): {result?.ErrorMessage ?? "HTTP " + result?.StatusCode}");
        }
        catch (Exception ex)
        {
            return new ErrorDataResult<object?>(null, $"Webhook hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Webhook hatası geçici mi (retry edilmeli mi)?
    /// Geçici: 5xx sunucu hatası, 429 rate-limit, 0 (network/timeout).
    /// Kalıcı: 4xx (400/401/403/404 ...) — istemci hatası, retry boşa gider.
    /// </summary>
    private static bool IsTransientWebhookFailure(int statusCode)
    {
        if (statusCode == 0) return true;          // network / timeout / DNS
        if (statusCode == 429) return true;        // too many requests
        if (statusCode >= 500 && statusCode <= 599) return true; // server error
        return false;                              // 4xx ve diğerleri → kalıcı
    }

    // ── YARDIMCI METODLAR ─────────────────────────────────────────────────────────

    private int ResolveUserId(
        Dictionary<string, string> parameters,
        RuleContext context,
        string targetKey   = "userTarget",
        string customIdKey = "customUserId")
    {
        var userTarget = parameters.GetValueOrDefault(targetKey) ?? "context_user";
        return userTarget switch
        {
            "target_user" => context.TargetUserId ?? context.SystemUserId,
            "custom"      => int.TryParse(Resolve(parameters.GetValueOrDefault(customIdKey), context), out var cid)
                                 ? cid
                                 : context.SystemUserId,
            _             => context.SystemUserId
        };
    }

    private int? ResolveProblemId(Dictionary<string, string> parameters, RuleContext context)
    {
        var target = parameters.GetValueOrDefault("problemTarget") ?? "context_problem";
        if (target == "custom")
        {
            if (int.TryParse(Resolve(parameters.GetValueOrDefault("customProblemId"), context), out var cid))
                return cid;
            return null; // Fallback engellendi
        }
        return context.ProblemId;
    }

    private int? ResolveSolutionId(Dictionary<string, string> parameters, RuleContext context)
    {
        var target = parameters.GetValueOrDefault("solutionTarget") ?? "context_solution";
        if (target == "custom")
        {
            if (int.TryParse(Resolve(parameters.GetValueOrDefault("customSolutionId"), context), out var cid))
                return cid;
            return null; // Fallback engellendi
        }
        return context.SolutionId;
    }

    private string? ResolveEmailAddress(Dictionary<string, string> parameters, RuleContext context)
    {
        var recipientType = parameters.GetValueOrDefault("recipient") ?? "context_user";
        return recipientType switch
        {
            "target_user" when context.TargetUserId.HasValue
                => _userService.GetById(context.TargetUserId.Value).Data?.Email,
            "target_user"
                => context.UserSnapshot?.Email,
            "custom"
                => Resolve(parameters.GetValueOrDefault("customTo"), context),
            _ => context.UserSnapshot?.Email
        };
    }

    private (string subject, string body) ResolveEmailContent(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var templateKey = Resolve(parameters.GetValueOrDefault("templateKey"), context);

        if (!string.IsNullOrWhiteSpace(templateKey))
        {
            var tmplResult = _emailTemplateService.GetByKey(templateKey);
            if (tmplResult.Success && tmplResult.Data != null)
            {
                var placeholders = BuildContextPlaceholders(context);
                var subject = _emailTemplateService.RenderTemplate(tmplResult.Data.Subject, placeholders).Data
                              ?? tmplResult.Data.Subject;
                var body    = _emailTemplateService.RenderTemplate(tmplResult.Data.Body, placeholders).Data
                              ?? tmplResult.Data.Body;
                return (subject, body);
            }
        }

        return (
            Resolve(parameters.GetValueOrDefault("subject"), context),
            Resolve(parameters.GetValueOrDefault("body"),    context)
        );
    }

    private Dictionary<string, string> BuildContextPlaceholders(RuleContext context)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (context.UserSnapshot != null)
        {
            d["Name"]     = context.UserSnapshot.Name;
            d["Surname"]  = context.UserSnapshot.Surname;
            d["Email"]    = context.UserSnapshot.Email;
            d["UserName"] = context.UserSnapshot.UserName;
        }

        if (context.ProblemSnapshot  != null) d["ProblemTitle"]    = context.ProblemSnapshot.Title;
        if (context.SolutionSnapshot != null) d["SolutionId"]      = context.SolutionSnapshot.Id.ToString();

        d["TriggerEvent"]  = context.TriggerEventName;
        d["SystemUserId"]  = context.SystemUserId.ToString();
        d["InstitutionId"] = context.InstitutionId?.ToString() ?? "";

        return d;
    }

    // Placeholder çözümleme — {User.Email}, {Problem.Title} vb.
    private string Resolve(string? template, RuleContext context)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        return PlaceholderPattern().Replace(template, match =>
        {
            var path = match.Groups[1].Value;
            return ResolvePath(path, context) ?? match.Value;
        });
    }

    private static string? ResolvePath(string path, RuleContext context)
    {
        var dotIndex = path.IndexOf('.');
        if (dotIndex < 0)
            return GetPropertyValue(context, path)?.ToString();

        var prefix   = path[..dotIndex].ToLowerInvariant();
        var property = path[(dotIndex + 1)..];

        return prefix switch
        {
            "user"     => GetPropertyValue(context.UserSnapshot,     property)?.ToString(),
            "problem"  => GetPropertyValue(context.ProblemSnapshot,  property)?.ToString(),
            "solution" => GetPropertyValue(context.SolutionSnapshot, property)?.ToString(),
            "context"  => GetPropertyValue(context, property)?.ToString(),
            _          => null
        };
    }

    private static object? GetPropertyValue(object? obj, string propertyName)
    {
        if (obj is null) return null;
        return obj.GetType()
            .GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
            ?.GetValue(obj);
    }
}
