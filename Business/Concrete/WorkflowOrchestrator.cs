using Business.Abstract;
using Business.Models;
using Business.Workflow.Messages;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Business.Concrete;

/// <summary>
/// Sprint 1 Orchestrator v1:
/// 1) triggerEvent + institutionId için aktif definition'ları bulur.
/// 2) Her definition için idempotency kontrolü yapar.
/// 3) WorkflowRun + RuleContextSnapshot oluşturur.
/// 4) FlowJson'daki tüm root node'lar için NodeRun planlar ve kuyruğa gönderir.
/// Kill Switch: Soft veya daha yüksek mod aktifse yeni run başlatılmaz.
/// </summary>
public class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly IWorkflowDefinitionDal _definitionDal;
    private readonly IWorkflowVersionDal _versionDal;
    private readonly IWorkflowRunDal _runDal;
    private readonly INodeRunDal _nodeRunDal;
    private readonly IRuleContextSnapshotDal _snapshotDal;
    private readonly IBus _bus;
    private readonly IKillSwitchService _killSwitch;
    private readonly ILogger<WorkflowOrchestrator> _logger;

    public WorkflowOrchestrator(
        IWorkflowDefinitionDal definitionDal,
        IWorkflowVersionDal versionDal,
        IWorkflowRunDal runDal,
        INodeRunDal nodeRunDal,
        IRuleContextSnapshotDal snapshotDal,
        IBus bus,
        IKillSwitchService killSwitch,
        ILogger<WorkflowOrchestrator> logger)
    {
        _definitionDal = definitionDal;
        _versionDal = versionDal;
        _runDal = runDal;
        _nodeRunDal = nodeRunDal;
        _snapshotDal = snapshotDal;
        _bus = bus;
        _killSwitch = killSwitch;
        _logger = logger;
    }

    public async Task<IDataResult<List<WorkflowRun>>> StartAsync(
        string triggerEvent,
        int institutionId,
        string contextJson,
        Guid eventId,
        int triggeredByUserId,
        bool isDryRun = false)
    {
        // Kill switch kontrolü — Soft veya daha yüksek mod yeni run'ları engeller
        if (_killSwitch.IsSoft())
        {
            _logger.LogWarning(
                "[Orchestrator] Kill switch active — rejecting new run for trigger={TriggerEvent} institution={InstitutionId}",
                triggerEvent, institutionId);
            return new ErrorDataResult<List<WorkflowRun>>(new List<WorkflowRun>(), "Kill switch aktif: yeni workflow run'ları geçici olarak engellendi.");
        }

        var definitions = _definitionDal.GetActiveByTrigger(triggerEvent, institutionId);
        if (definitions.Count == 0)
        {
            _logger.LogDebug(
                "[Orchestrator] No active definitions for trigger={TriggerEvent} institution={InstitutionId}",
                triggerEvent, institutionId);
            return new SuccessDataResult<List<WorkflowRun>>(new List<WorkflowRun>());
        }

        var runs = new List<WorkflowRun>();

        foreach (var definition in definitions)
        {
            if (definition.CurrentVersionId is null)
            {
                _logger.LogWarning("[Orchestrator] Definition {DefinitionId} has no active version — skipping",
                    definition.Id);
                continue;
            }

            var version = _versionDal.Get(v => v.Id == definition.CurrentVersionId.Value);
            if (version is null || version.Status != 1)
            {
                _logger.LogWarning("[Orchestrator] Version {VersionId} not found or inactive — skipping",
                    definition.CurrentVersionId);
                continue;
            }

            var enrichedContextJson = InjectWorkflowCreatorId(contextJson, definition.CreatedByUserId);

            var result = await StartSingleAsync(
                definition.Id, version.Id, version.FlowJson,
                triggerEvent, institutionId, enrichedContextJson,
                eventId, triggeredByUserId, isDryRun);

            if (result.Success)
                runs.Add(result.Data);
        }

        return new SuccessDataResult<List<WorkflowRun>>(runs,
            $"{runs.Count} workflow run(s) started.");
    }

    public async Task<IDataResult<WorkflowRun>> StartSingleAsync(
        int definitionId,
        int versionId,
        string flowJson,
        string triggerEvent,
        int institutionId,
        string contextJson,
        Guid eventId,
        int triggeredByUserId,
        bool isDryRun = false)
    {
        // 1) Idempotency — aynı (eventId, definitionId, versionId) üçlüsü var mı?
        var existing = _runDal.FindExisting(eventId, definitionId, versionId);
        if (existing is not null)
        {
            _logger.LogInformation(
                "[Orchestrator] Idempotent run found: {RunId} for event={EventId} def={DefinitionId}",
                existing.Id, eventId, definitionId);
            return new SuccessDataResult<WorkflowRun>(existing, "Mevcut run döndürüldü (idempotent).");
        }

        // 2) WorkflowRun oluştur
        var run = new WorkflowRun
        {
            Id = Guid.NewGuid(),
            DefinitionId = definitionId,
            VersionId = versionId,
            TriggerEvent = triggerEvent,
            InstitutionId = institutionId,
            EventId = eventId,
            TriggeredByUserId = triggeredByUserId,
            Status = WorkflowRunStatus.Pending,
            IsDryRun = isDryRun,
            StartedAt = DateTime.UtcNow,
        };
        _runDal.Add(run);

        _logger.LogInformation(
            "[Orchestrator] Created WorkflowRun {RunId} for definition={DefinitionId} event={TriggerEvent}",
            run.Id, definitionId, triggerEvent);

        // 3) Context snapshot kaydet
        _snapshotDal.Add(new RuleContextSnapshot
        {
            Id = Guid.NewGuid(),
            RunId = run.Id,
            ContextJson = contextJson,
            CreatedAt = DateTime.UtcNow,
        });

        // 4) Root node'ları bul ve NodeRun planla
        var rootNodes = ParseRootNodes(flowJson);
        _logger.LogInformation("[Orchestrator] Run {RunId} has {Count} root node(s)", run.Id, rootNodes.Count);

        run.Status = WorkflowRunStatus.Running;
        _runDal.Update(run);

        foreach (var (nodeId, nodeType) in rootNodes)
        {
            var nodeRun = new NodeRun
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                NodeId = nodeId,
                NodeType = nodeType,
                Status = NodeRunStatus.Pending,
                StartedAt = DateTime.UtcNow,
            };
            _nodeRunDal.Add(nodeRun);

            // 5) Queue'ya gönder
            await _bus.Publish(new NodeRunMessage
            {
                NodeRunId = nodeRun.Id,
                RunId = run.Id,
                DefinitionId = definitionId,
                NodeId = nodeId,
                NodeType = nodeType,
                InstitutionId = institutionId,
                IsDryRun = isDryRun,
                EnqueuedAt = DateTime.UtcNow,
            });

            _logger.LogInformation(
                "[Orchestrator] NodeRun {NodeRunId} (type={NodeType}) published to queue",
                nodeRun.Id, nodeType);
        }

        return new SuccessDataResult<WorkflowRun>(run, "Workflow run başlatıldı.");
    }

    /// <summary>
    /// contextJson'a WorkflowCreatorId'yi inject eder.
    /// Her definition için farklı yaratıcı olabileceğinden StartAsync döngüsünde çağrılır.
    /// </summary>
    private static string InjectWorkflowCreatorId(string contextJson, int creatorId)
    {
        if (string.IsNullOrWhiteSpace(contextJson) || creatorId <= 0) return contextJson;
        try
        {
            var ctx = JsonSerializer.Deserialize<RuleContext>(contextJson);
            if (ctx is null) return contextJson;
            ctx.WorkflowCreatorId = creatorId;
            return JsonSerializer.Serialize(ctx);
        }
        catch
        {
            return contextJson;
        }
    }

    /// <summary>
    /// FlowJson içinden root node'ları çıkarır.
    /// Root node = başka bir node'dan edge ile gelmemiş (hedef olmayan) node.
    /// Sprint 1 POC: sadece triggerNode tipindeki node'ları root kabul eder.
    /// Faz 3'te gerçek DAG root hesaplaması yapılacak.
    /// </summary>
    private static List<(string nodeId, string nodeType)> ParseRootNodes(string flowJson)
    {
        var result = new List<(string, string)>();

        if (string.IsNullOrWhiteSpace(flowJson))
            return result;

        try
        {
            using var doc = JsonDocument.Parse(flowJson);
            var root = doc.RootElement;

            // React Flow formatı: { nodes: [{id, type, data}, ...], edges: [...] }
            if (!root.TryGetProperty("nodes", out var nodesEl))
                return result;

            // Target nodeId'lerini topla (edge hedefleri root değildir)
            var targets = new HashSet<string>();
            if (root.TryGetProperty("edges", out var edgesEl))
            {
                foreach (var edge in edgesEl.EnumerateArray())
                {
                    if (edge.TryGetProperty("target", out var t))
                        targets.Add(t.GetString() ?? "");
                }
            }

            foreach (var node in nodesEl.EnumerateArray())
            {
                var id = node.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                var type = node.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? "" : "";

                // Root: hiçbir edge'in hedefi değil
                if (!string.IsNullOrEmpty(id) && !targets.Contains(id))
                {
                    result.Add((id, type));
                }
            }
        }
        catch (JsonException ex)
        {
            // Geçersiz FlowJson — boş döndür; Faz 3'te validation eklenecek
            System.Diagnostics.Debug.WriteLine($"[Orchestrator] FlowJson parse error: {ex.Message}");
        }

        return result;
    }
}
