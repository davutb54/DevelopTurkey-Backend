using Business.Workflow.Messages;
using DataAccess.Abstract;
using Entities.Concrete;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Business.Consumers;

/// <summary>
/// NodeRunConsumer tüm retry'larını tükettiğinde MassTransit tarafından
/// Fault&lt;NodeRunMessage&gt; mesajı fırlatılır.
/// Bu consumer kalıcı hata durumunu DB'ye yazar.
/// </summary>
public class NodeRunFaultConsumer : IConsumer<Fault<NodeRunMessage>>
{
    private readonly INodeRunDal _nodeRunDal;
    private readonly IWorkflowRunDal _workflowRunDal;
    private readonly ILogger<NodeRunFaultConsumer> _logger;

    public NodeRunFaultConsumer(
        INodeRunDal nodeRunDal,
        IWorkflowRunDal workflowRunDal,
        ILogger<NodeRunFaultConsumer> logger)
    {
        _nodeRunDal = nodeRunDal;
        _workflowRunDal = workflowRunDal;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Fault<NodeRunMessage>> context)
    {
        var msg = context.Message.Message;
        var exceptions = context.Message.Exceptions;
        var lastError = exceptions.LastOrDefault()?.Message ?? "Bilinmeyen hata";

        _logger.LogError(
            "[NodeRunFaultConsumer] NodeRun {NodeRunId} kalıcı olarak başarısız oldu (tüm retry'lar tükendi). Error: {Error}",
            msg.NodeRunId, lastError);

        // NodeRun → Failed
        var nodeRun = _nodeRunDal.Get(n => n.Id == msg.NodeRunId);
        if (nodeRun is not null)
        {
            nodeRun.Status = NodeRunStatus.Failed;
            nodeRun.EndedAt = DateTime.UtcNow;
            nodeRun.ErrorMessage = (lastError.Length > 2000 ? lastError[..2000] : lastError)
                + $" [fault after {exceptions.Length} attempts]";
            _nodeRunDal.Update(nodeRun);

            // WorkflowRun'ı da Failed yap (kritik node başarısız oldu)
            var workflowRun = _workflowRunDal.Get(r => r.Id == msg.RunId);
            if (workflowRun is not null && workflowRun.Status != WorkflowRunStatus.Succeeded)
            {
                workflowRun.Status = WorkflowRunStatus.Failed;
                workflowRun.EndedAt = DateTime.UtcNow;
                workflowRun.ErrorMessage = $"NodeRun {msg.NodeRunId} kalıcı hata: {lastError[..Math.Min(500, lastError.Length)]}";
                _workflowRunDal.Update(workflowRun);
            }
        }

        await Task.CompletedTask;
    }
}
