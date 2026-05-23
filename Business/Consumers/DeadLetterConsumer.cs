using Business.Workflow.Messages;
using DataAccess.Abstract;
using Entities.Concrete;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Business.Consumers;

/// <summary>
/// workflow.deadletter kuyruğuna düşen mesajları DB'ye kaydeder.
/// Operasyon ekibi bu kayıtları AdminPanel üzerinden inceleyip
/// manuel olarak yeniden kuyruğa alabilir.
/// </summary>
public class DeadLetterConsumer : IConsumer<DeadLetterMessage>
{
    private readonly IWorkflowDeadLetterDal _deadLetterDal;
    private readonly ILogger<DeadLetterConsumer> _logger;

    public DeadLetterConsumer(
        IWorkflowDeadLetterDal deadLetterDal,
        ILogger<DeadLetterConsumer> logger)
    {
        _deadLetterDal = deadLetterDal;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeadLetterMessage> context)
    {
        var msg = context.Message;

        _logger.LogError(
            "[DeadLetterConsumer] Dead-letter received — reason={Reason}, runId={RunId}, actionRunId={ActionRunId}",
            msg.Reason, msg.RunId, msg.ActionRunId);

        _deadLetterDal.Add(new WorkflowDeadLetter
        {
            RunId        = msg.RunId,
            NodeRunId    = msg.NodeRunId,
            ActionRunId  = msg.ActionRunId,
            Reason       = msg.Reason,
            ErrorDetail  = msg.ErrorDetail,
            PayloadJson  = msg.OriginalPayloadJson,
            CreatedAt    = DateTime.UtcNow,
        });

        await Task.CompletedTask;
    }
}
