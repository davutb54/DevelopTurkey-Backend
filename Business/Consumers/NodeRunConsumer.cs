using Business.Abstract;
using Business.Workflow.Messages;
using DataAccess.Abstract;
using Entities.Concrete;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Business.Consumers;

/// <summary>
/// workflow.noderun kuyruğundan gelen NodeRunMessage'ları işler.
/// NodeRun'ı Running → Succeeded/Failed durumuna geçirir.
/// Sprint 1 POC: temel durum geçişi + loglama.
/// Kill Switch: Hard veya Emergency mod aktifse mesaj atlanır (skipped).
/// </summary>
public class NodeRunConsumer : IConsumer<NodeRunMessage>
{
    private readonly INodeRunDal _nodeRunDal;
    private readonly IKillSwitchService _killSwitch;
    private readonly ILogger<NodeRunConsumer> _logger;

    public NodeRunConsumer(
        INodeRunDal nodeRunDal,
        IKillSwitchService killSwitch,
        ILogger<NodeRunConsumer> logger)
    {
        _nodeRunDal = nodeRunDal;
        _killSwitch = killSwitch;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<NodeRunMessage> context)
    {
        var msg = context.Message;

        // Kill switch — Hard veya Emergency modda işleme durur
        if (_killSwitch.IsHard())
        {
            _logger.LogWarning(
                "[NodeRunConsumer] Kill switch HARD/EMERGENCY — skipping NodeRun {NodeRunId}",
                msg.NodeRunId);

            var skipped = _nodeRunDal.Get(n => n.Id == msg.NodeRunId);
            if (skipped is not null)
            {
                skipped.Status = NodeRunStatus.Skipped;
                skipped.EndedAt = DateTime.UtcNow;
                skipped.ErrorMessage = "Kill switch aktif — Hard/Emergency mod.";
                _nodeRunDal.Update(skipped);
            }
            return;
        }

        _logger.LogInformation(
            "[NodeRunConsumer] Processing NodeRun {NodeRunId} (type={NodeType}, run={RunId})",
            msg.NodeRunId, msg.NodeType, msg.RunId);

        var nodeRun = _nodeRunDal.Get(n => n.Id == msg.NodeRunId);
        if (nodeRun is null)
        {
            _logger.LogWarning("[NodeRunConsumer] NodeRun {NodeRunId} not found — skipping", msg.NodeRunId);
            return;
        }

        // Durum: Running
        nodeRun.Status = NodeRunStatus.Running;
        _nodeRunDal.Update(nodeRun);

        try
        {
            // Sprint 1 POC: sadece log, gerçek node execution Faz 3'te
            _logger.LogInformation(
                "[NodeRunConsumer] NodeRun {NodeRunId} ({NodeType}) executing — isDryRun={IsDryRun}",
                msg.NodeRunId, msg.NodeType, msg.IsDryRun);

            // TODO (Faz 3): execution engine çağrısı
            // await _executionEngine.ProcessNodeAsync(msg.NodeRunId, context.CancellationToken);

            // POC: başarılı tamamlandı
            nodeRun.Status = NodeRunStatus.Succeeded;
            nodeRun.EndedAt = DateTime.UtcNow;
            _nodeRunDal.Update(nodeRun);

            _logger.LogInformation("[NodeRunConsumer] NodeRun {NodeRunId} completed successfully", msg.NodeRunId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NodeRunConsumer] NodeRun {NodeRunId} failed: {Error}", msg.NodeRunId, ex.Message);
            nodeRun.Status = NodeRunStatus.Failed;
            nodeRun.EndedAt = DateTime.UtcNow;
            nodeRun.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            _nodeRunDal.Update(nodeRun);
            throw; // MassTransit retry mekanizması devreye girer
        }

        await Task.CompletedTask;
    }
}
