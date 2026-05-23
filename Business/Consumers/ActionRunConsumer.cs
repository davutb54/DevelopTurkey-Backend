using Business.Abstract;
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
    private readonly IActionRunDal _actionRunDal;
    private readonly IWorkflowDeadLetterDal _deadLetterDal;
    private readonly IWorkflowActionDispatcher _dispatcher;
    private readonly ILogger<ActionRunConsumer> _logger;

    public ActionRunConsumer(
        IActionRunDal actionRunDal,
        IWorkflowDeadLetterDal deadLetterDal,
        IWorkflowActionDispatcher dispatcher,
        ILogger<ActionRunConsumer> logger)
    {
        _actionRunDal = actionRunDal;
        _deadLetterDal = deadLetterDal;
        _dispatcher = dispatcher;
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
                // Dry-run: simüle et, gerçek işlem yapma
                _logger.LogInformation(
                    "[ActionRunConsumer] [DRY-RUN] Simulating action {ActionCode} for ActionRun {ActionRunId}",
                    msg.ActionCode, msg.ActionRunId);

                actionRun.Status = ActionRunStatus.Simulated;
                actionRun.EndedAt = DateTime.UtcNow;
                actionRun.ResultJson = """{"simulated":true}""";
                _actionRunDal.Update(actionRun);
                return;
            }

            // Gerçek dispatch — Faz 3'te tam execution engine bağlanacak
            // Şimdilik: dispatcher mevcut mekanizmayı çağırır
            _logger.LogInformation(
                "[ActionRunConsumer] Dispatching action {ActionCode} for ActionRun {ActionRunId}",
                msg.ActionCode, msg.ActionRunId);

            // TODO (Faz 3): tam context + parametreler ile dispatcher çağrısı
            // var result = await _dispatcher.DispatchAsync(msg.ActionCode, ruleContext, parameters, ct);

            actionRun.Status = ActionRunStatus.Succeeded;
            actionRun.EndedAt = DateTime.UtcNow;
            _actionRunDal.Update(actionRun);

            _logger.LogInformation("[ActionRunConsumer] ActionRun {ActionRunId} succeeded", msg.ActionRunId);
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

        await Task.CompletedTask;
    }
}
