using System.Text.Json;
using Business.Abstract;
using Business.Models;
using Business.Workflow.Messages;
using DataAccess.Abstract;
using Entities.Concrete;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Business.Consumers;

/// <summary>
/// workflow.actionrun kuyruğundan gelen ActionRunMessage'ları işler.
/// Retry politikası: 3 kez, exponential backoff (500ms, 1s, 2s).
/// Max retry aşılınca MassTransit dead-letter kuyruğuna gönderir.
/// </summary>
public class ActionRunConsumer : IConsumer<ActionRunMessage>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IActionRunDal _actionRunDal;
    private readonly IWorkflowDeadLetterDal _deadLetterDal;
    private readonly IWorkflowActionDispatcher _dispatcher;
    private readonly IRuleContextSnapshotDal _snapshotDal;
    private readonly ILogger<ActionRunConsumer> _logger;

    public ActionRunConsumer(
        IActionRunDal actionRunDal,
        IWorkflowDeadLetterDal deadLetterDal,
        IWorkflowActionDispatcher dispatcher,
        IRuleContextSnapshotDal snapshotDal,
        ILogger<ActionRunConsumer> logger)
    {
        _actionRunDal = actionRunDal;
        _deadLetterDal = deadLetterDal;
        _dispatcher = dispatcher;
        _snapshotDal = snapshotDal;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ActionRunMessage> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[ActionRunConsumer] Processing ActionRun {ActionRunId} (code={ActionCode}, attempt={Attempt})",
            msg.ActionRunId, msg.ActionCode, msg.AttemptNumber);

        var actionRun = _actionRunDal.Get(a => a.Id == msg.ActionRunId);
        if (actionRun is null)
        {
            _logger.LogWarning("[ActionRunConsumer] ActionRun {ActionRunId} not found — skipping", msg.ActionRunId);
            return;
        }

        actionRun.Status = ActionRunStatus.Running;
        actionRun.RetryCount = msg.AttemptNumber;
        _actionRunDal.Update(actionRun);

        try
        {
            if (msg.IsDryRun)
            {
                _logger.LogInformation(
                    "[ActionRunConsumer] [DRY-RUN] Simulating action {ActionCode} for ActionRun {ActionRunId}",
                    msg.ActionCode, msg.ActionRunId);

                actionRun.Status = ActionRunStatus.Simulated;
                actionRun.EndedAt = DateTime.UtcNow;
                actionRun.ResultJson = """{"simulated":true}""";
                _actionRunDal.Update(actionRun);
                return;
            }

            var parameters = DeserializeParameters(msg.PayloadJson);
            var ruleContext = ResolveRuleContext(msg);

            _logger.LogInformation(
                "[ActionRunConsumer] Dispatching action {ActionCode} for ActionRun {ActionRunId}",
                msg.ActionCode, msg.ActionRunId);

            var result = await _dispatcher.DispatchAsync(msg.ActionCode, parameters, ruleContext);

            actionRun.Status = result.Success ? ActionRunStatus.Succeeded : ActionRunStatus.Failed;
            actionRun.EndedAt = DateTime.UtcNow;
            actionRun.ResultJson = JsonSerializer.Serialize(new
            {
                result.Success,
                result.Message,
                Data = result.Data,
            });
            _actionRunDal.Update(actionRun);

            if (result.Success)
                _logger.LogInformation("[ActionRunConsumer] ActionRun {ActionRunId} succeeded", msg.ActionRunId);
            else
                _logger.LogWarning("[ActionRunConsumer] ActionRun {ActionRunId} failed (non-exception): {Msg}",
                    msg.ActionRunId, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ActionRunConsumer] ActionRun {ActionRunId} failed (attempt {Attempt}): {Error}",
                msg.ActionRunId, msg.AttemptNumber, ex.Message);

            actionRun.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            actionRun.RetryCount = msg.AttemptNumber;
            _actionRunDal.Update(actionRun);

            throw; // MassTransit retry mekanizması yönetir
        }
    }

    /// <summary>
    /// ActionRunMessage'dan RuleContext üretir.
    /// Önce mesajdaki RuleContextJson'ı dener; yoksa DB snapshot'ına bakar;
    /// yoksa msg alanlarından minimal context inşa eder.
    /// </summary>
    private RuleContext ResolveRuleContext(ActionRunMessage msg)
    {
        // 1. Mesajdaki pre-serialized context (NodeRunConsumer tarafından doldurulur)
        if (!string.IsNullOrWhiteSpace(msg.RuleContextJson))
        {
            try
            {
                var ctx = JsonSerializer.Deserialize<RuleContext>(msg.RuleContextJson, JsonOptions);
                if (ctx is not null)
                    return ctx;
            }
            catch (JsonException)
            {
                _logger.LogWarning(
                    "[ActionRunConsumer] RuleContextJson parse hatası — snapshot'a fallback. RunId={RunId}",
                    msg.RunId);
            }
        }

        // 2. DB snapshot (WorkflowOrchestrator tarafından kaydedilir)
        var snapshot = _snapshotDal.GetByRun(msg.RunId);
        if (snapshot is not null && !string.IsNullOrWhiteSpace(snapshot.ContextJson))
        {
            try
            {
                var ctx = JsonSerializer.Deserialize<RuleContext>(snapshot.ContextJson, JsonOptions);
                if (ctx is not null)
                    return ctx;
            }
            catch (JsonException)
            {
                _logger.LogWarning(
                    "[ActionRunConsumer] Snapshot ContextJson parse hatası — minimal context kullanılıyor. RunId={RunId}",
                    msg.RunId);
            }
        }

        // 3. Minimal fallback — msg'daki skaler alanlardan inşa et
        _logger.LogWarning(
            "[ActionRunConsumer] RuleContext bulunamadı — minimal context. RunId={RunId}", msg.RunId);

        return new RuleContext
        {
            SystemUserId = msg.TriggeredByUserId,
            InstitutionId = msg.InstitutionId,
            TriggerEventName = msg.TriggerEvent,
        };
    }

    private static Dictionary<string, string> DeserializeParameters(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson) || payloadJson == "{}")
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(payloadJson, JsonOptions)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
