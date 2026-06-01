using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Abstract;
using Business.Models;
using Business.Workflow.Messages;
using DataAccess.Abstract;
using Entities.Concrete;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Business.Consumers;

/// <summary>
/// workflow.noderun kuyruğundan gelen NodeRunMessage'ları işler.
///
/// Desteklenen node tipleri:
///   triggerNode   → doğrudan Succeeded
///   conditionNode → doğrudan Succeeded (değerlendirme WorkflowInterpreterManager'da)
///   actionNode    → ActionRun oluştur + ActionRunMessage publish et
///   csharpNode    → (Faz sonraki adım) şimdilik Succeeded
///
/// Kill Switch: Hard veya Emergency mod aktifse mesaj atlanır (skipped).
/// </summary>
public class NodeRunConsumer : IConsumer<NodeRunMessage>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly INodeRunDal _nodeRunDal;
    private readonly IWorkflowRunDal _workflowRunDal;
    private readonly IWorkflowVersionDal _workflowVersionDal;
    private readonly IActionRunDal _actionRunDal;
    private readonly IRuleContextSnapshotDal _snapshotDal;
    private readonly IKillSwitchService _killSwitch;
    private readonly IBus _bus;
    private readonly ILogger<NodeRunConsumer> _logger;

    public NodeRunConsumer(
        INodeRunDal nodeRunDal,
        IWorkflowRunDal workflowRunDal,
        IWorkflowVersionDal workflowVersionDal,
        IActionRunDal actionRunDal,
        IRuleContextSnapshotDal snapshotDal,
        IKillSwitchService killSwitch,
        IBus bus,
        ILogger<NodeRunConsumer> logger)
    {
        _nodeRunDal = nodeRunDal;
        _workflowRunDal = workflowRunDal;
        _workflowVersionDal = workflowVersionDal;
        _actionRunDal = actionRunDal;
        _snapshotDal = snapshotDal;
        _killSwitch = killSwitch;
        _bus = bus;
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

        // Idempotency: zaten terminal durumda ise yeniden işleme
        if (nodeRun.Status is NodeRunStatus.Succeeded or NodeRunStatus.Skipped or NodeRunStatus.Failed)
        {
            _logger.LogInformation(
                "[NodeRunConsumer] NodeRun {NodeRunId} already in terminal status {Status} — skipping (idempotent)",
                msg.NodeRunId, nodeRun.Status);
            return;
        }

        nodeRun.Status = NodeRunStatus.Running;
        nodeRun.EndedAt = null;
        nodeRun.ErrorMessage = null;
        _nodeRunDal.Update(nodeRun);

        try
        {
            var normalizedType = msg.NodeType.Replace("Node", "", StringComparison.OrdinalIgnoreCase)
                                             .ToLowerInvariant();

            switch (normalizedType)
            {
                case "action":
                    await ProcessActionNodeAsync(msg, nodeRun, context.CancellationToken);
                    break;

                case "trigger":
                case "condition":
                case "csharp":
                default:
                    // triggerNode, conditionNode: sadece durum geçişi (gerçek mantık
                    // WorkflowInterpreterManager'ın senkron yolunda çalışır).
                    // csharpNode: Faz sonrası ekleme.
                    _logger.LogInformation(
                        "[NodeRunConsumer] NodeRun {NodeRunId} ({NodeType}) — no async work, marking Succeeded",
                        msg.NodeRunId, msg.NodeType);
                    break;
            }

            nodeRun.Status = NodeRunStatus.Succeeded;
            nodeRun.EndedAt = DateTime.UtcNow;
            _nodeRunDal.Update(nodeRun);

            _logger.LogInformation("[NodeRunConsumer] NodeRun {NodeRunId} completed successfully", msg.NodeRunId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NodeRunConsumer] NodeRun {NodeRunId} failed (will retry): {Error}",
                msg.NodeRunId, ex.Message);

            nodeRun.Status = NodeRunStatus.Pending;
            nodeRun.EndedAt = null;
            nodeRun.ErrorMessage = (ex.Message.Length > 500 ? ex.Message[..500] : ex.Message) + " [retry pending]";
            _nodeRunDal.Update(nodeRun);

            throw;
        }
    }

    /// <summary>
    /// actionNode için: FlowJson'dan aksiyon kodunu ve parametreleri çıkarır,
    /// ActionRun kaydı oluşturur ve ActionRunMessage kuyruğa gönderir.
    /// </summary>
    private async Task ProcessActionNodeAsync(
        NodeRunMessage msg,
        NodeRun nodeRun,
        CancellationToken ct)
    {
        // WorkflowRun → VersionId
        var workflowRun = _workflowRunDal.Get(r => r.Id == msg.RunId);
        if (workflowRun is null)
        {
            _logger.LogWarning("[NodeRunConsumer] WorkflowRun {RunId} not found for NodeRun {NodeRunId}",
                msg.RunId, msg.NodeRunId);
            return;
        }

        // WorkflowVersion → FlowJson
        var version = _workflowVersionDal.Get(v => v.Id == workflowRun.VersionId);
        if (version is null)
        {
            _logger.LogWarning("[NodeRunConsumer] WorkflowVersion {VersionId} not found", workflowRun.VersionId);
            return;
        }

        // FlowJson içinden ilgili node'u bul
        var (actionCode, payloadJson) = ParseActionNode(version.FlowJson, msg.NodeId);
        if (string.IsNullOrWhiteSpace(actionCode))
        {
            _logger.LogWarning(
                "[NodeRunConsumer] ActionCode bulunamadı — NodeId={NodeId}, RunId={RunId}",
                msg.NodeId, msg.RunId);
            return;
        }

        // RuleContextSnapshot → bağlam JSON'u
        var snapshot = _snapshotDal.GetByRun(msg.RunId);
        var ruleContextJson = snapshot?.ContextJson ?? string.Empty;

        // ActionRun oluştur
        var actionRun = new ActionRun
        {
            Id = Guid.NewGuid(),
            NodeRunId = nodeRun.Id,
            ActionCode = actionCode,
            Status = ActionRunStatus.Pending,
            PayloadJson = payloadJson,
            StartedAt = DateTime.UtcNow,
        };
        _actionRunDal.Add(actionRun);

        // ActionRunMessage kuyruğa gönder
        await _bus.Publish(new ActionRunMessage
        {
            ActionRunId = actionRun.Id,
            NodeRunId = nodeRun.Id,
            RunId = msg.RunId,
            ActionCode = actionCode,
            PayloadJson = payloadJson,
            InstitutionId = msg.InstitutionId,
            TriggeredByUserId = workflowRun.TriggeredByUserId,
            IsDryRun = msg.IsDryRun,
            AttemptNumber = 0,
            TriggerEvent = workflowRun.TriggerEvent,
            RuleContextJson = ruleContextJson,
            EnqueuedAt = DateTime.UtcNow,
        }, ct);

        _logger.LogInformation(
            "[NodeRunConsumer] ActionRun {ActionRunId} (code={ActionCode}) queued for NodeRun {NodeRunId}",
            actionRun.Id, actionCode, msg.NodeRunId);
    }

    /// <summary>
    /// FlowJson içinden belirtilen nodeId'ye ait aksiyon kodu ve parametreleri çıkarır.
    /// React Flow formatı: { nodes: [{id, type, data: {action, params}}], edges: [] }
    /// </summary>
    private static (string actionCode, string payloadJson) ParseActionNode(string flowJson, string nodeId)
    {
        if (string.IsNullOrWhiteSpace(flowJson) || string.IsNullOrWhiteSpace(nodeId))
            return (string.Empty, "{}");

        try
        {
            using var doc = JsonDocument.Parse(flowJson);
            if (!doc.RootElement.TryGetProperty("nodes", out var nodes))
                return (string.Empty, "{}");

            foreach (var node in nodes.EnumerateArray())
            {
                if (!node.TryGetProperty("id", out var idEl) ||
                    !string.Equals(idEl.GetString(), nodeId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!node.TryGetProperty("data", out var data))
                    break;

                var actionCode = string.Empty;
                if (data.TryGetProperty("action", out var actionEl))
                    actionCode = actionEl.GetString() ?? string.Empty;

                var payloadJson = "{}";
                if (data.TryGetProperty("params", out var paramsEl) &&
                    paramsEl.ValueKind == JsonValueKind.Object)
                {
                    payloadJson = paramsEl.GetRawText();
                }

                return (actionCode, payloadJson);
            }
        }
        catch (JsonException ex)
        {
            // FlowJson geçersizse boş döndür; çağıran uyarı loglar
            _ = ex;
        }

        return (string.Empty, "{}");
    }
}
