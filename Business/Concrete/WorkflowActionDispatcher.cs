using System.Diagnostics;
using System.Text.Json;
using Business.Abstract;
using Business.Models;
using Core.Utilities.Authorization;
using Core.Utilities.Results;
using Microsoft.Extensions.Logging;

namespace Business.Concrete;

/// <summary>
/// Workflow action registry dispatcher.
///
/// Sorumluluklar (tek sorumluluk):
///   1. Capability kontrolü
///   2. Handler lookup (ActionCode → IWorkflowActionHandler)
///   3. Parametre şema doğrulaması
///   4. Handler'a delegate
///   5. Sonucu kalıcı audit log'a yaz (success/failure/denied)
///
/// İş mantığı yok — her action kendi handler sınıfında yaşar (Business/Concrete/Actions/).
/// Yeni action eklemek için sadece IWorkflowActionHandler implement edilip DI'a kaydedilmesi yeterli.
/// </summary>
public class WorkflowActionDispatcher : IWorkflowActionDispatcher
{
    private const string AuditCategory = "WorkflowAction";

    private readonly IReadOnlyDictionary<string, IWorkflowActionHandler> _handlers;
    private readonly ICapabilityResolver _capabilityResolver;
    private readonly ILogService _auditLog;
    private readonly ILogger<WorkflowActionDispatcher> _logger;

    public WorkflowActionDispatcher(
        IEnumerable<IWorkflowActionHandler> handlers,
        ICapabilityResolver capabilityResolver,
        ILogService auditLog,
        ILogger<WorkflowActionDispatcher> logger)
    {
        _handlers           = handlers.ToDictionary(h => h.ActionCode, StringComparer.OrdinalIgnoreCase);
        _capabilityResolver = capabilityResolver;
        _auditLog           = auditLog;
        _logger             = logger;
    }

    public async Task<IDataResult<object?>> DispatchAsync(
        string actionCode,
        Dictionary<string, string> parameters,
        RuleContext context)
    {
        var normalizedCode = actionCode.ToLowerInvariant().Trim();

        // WorkflowCreatorId > 0 ise capability kontrolü workflow'u yaratan admin üzerinden yapılır.
        // Böylece "auth.registered" gibi event'lerde tetikleyen kullanıcı yeni/yetkisiz olsa bile
        // admin'in capability'leri geçerli olur.
        var authUserId = context.WorkflowCreatorId > 0 ? context.WorkflowCreatorId : context.SystemUserId;

        _logger.LogInformation(
            "[WorkflowActionDispatcher] Action={ActionCode}, AuthUserId={AuthUserId} (SystemUserId={SystemUserId}), Trigger={Trigger}",
            normalizedCode, authUserId, context.SystemUserId, context.TriggerEventName);

        // ── 1. Capability kontrolü ─────────────────────────────────────────────
        var capabilityCode = $"workflow.action.{normalizedCode}";
        if (!_capabilityResolver.Allows(authUserId, capabilityCode))
        {
            _logger.LogWarning(
                "[WorkflowActionDispatcher] capability_denied: {CapabilityCode} for AuthUserId={AuthUserId}",
                capabilityCode, authUserId);
            var deniedMsg = $"Yetkisiz action: '{capabilityCode}'";
            WriteAudit(normalizedCode, "denied", deniedMsg, parameters, context, durationMs: 0);
            return new ErrorDataResult<object?>(null, deniedMsg);
        }

        // ── 2. Handler lookup ──────────────────────────────────────────────────
        if (!_handlers.TryGetValue(normalizedCode, out var handler))
        {
            _logger.LogWarning(
                "[WorkflowActionDispatcher] Bilinmeyen action kodu: {ActionCode}", normalizedCode);
            var unknownMsg = $"Bilinmeyen aksiyon kodu: '{normalizedCode}'";
            WriteAudit(normalizedCode, "unknown", unknownMsg, parameters, context, durationMs: 0);
            return new ErrorDataResult<object?>(null, unknownMsg);
        }

        // ── 3. Parametre şema doğrulaması ──────────────────────────────────────
        var validationErrors = handler.ValidateParameters(parameters);
        if (validationErrors.Count > 0)
        {
            var errorMsg = string.Join("; ", validationErrors);
            _logger.LogWarning(
                "[WorkflowActionDispatcher] Parametre doğrulama hatası [{ActionCode}]: {Errors}",
                normalizedCode, errorMsg);
            var validationMsg = $"Parametre hatası: {errorMsg}";
            WriteAudit(normalizedCode, "invalid", validationMsg, parameters, context, durationMs: 0);
            return new ErrorDataResult<object?>(null, validationMsg);
        }

        // ── 4. Execute ─────────────────────────────────────────────────────────
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await handler.ExecuteAsync(parameters, context);
            sw.Stop();
            WriteAudit(
                normalizedCode,
                result.Success ? "success" : "failure",
                result.Message ?? string.Empty,
                parameters,
                context,
                sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[WorkflowActionDispatcher] Action={ActionCode} çalıştırılırken hata oluştu.",
                normalizedCode);
            var exMsg = $"Aksiyon hatası ({normalizedCode}): {ex.Message}";
            WriteAudit(normalizedCode, "error", exMsg, parameters, context, sw.ElapsedMilliseconds);
            return new ErrorDataResult<object?>(null, exMsg);
        }
    }

    /// <summary>
    /// Action sonucunu Log tablosuna yazar. Audit/compliance amaçlı.
    /// </summary>
    private void WriteAudit(
        string actionCode,
        string outcome,
        string message,
        Dictionary<string, string> parameters,
        RuleContext context,
        long durationMs)
    {
        try
        {
            var details = JsonSerializer.Serialize(new
            {
                outcome,
                durationMs,
                trigger      = context.TriggerEventName,
                systemUserId = context.SystemUserId,
                targetUserId = context.TargetUserId,
                problemId    = context.ProblemId,
                solutionId   = context.SolutionId,
                commentId    = context.CommentId,
                parameters,
                message,
            });

            var auditMessage = $"[{outcome}] {actionCode}: {message}";

            switch (outcome)
            {
                case "success":
                    _auditLog.LogInfo(AuditCategory, actionCode, auditMessage, details, context.InstitutionId);
                    break;
                case "denied":
                case "invalid":
                case "unknown":
                    _auditLog.LogWarning(AuditCategory, actionCode, auditMessage, details, context.InstitutionId);
                    break;
                default: // failure, error
                    _auditLog.LogError(AuditCategory, actionCode, auditMessage, details, context.InstitutionId);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Audit log yazımı başarısız olursa dispatch akışını bozma; sadece logger'a yaz.
            _logger.LogError(ex,
                "[WorkflowActionDispatcher] Audit log yazılamadı. Action={ActionCode}",
                actionCode);
        }
    }
}
