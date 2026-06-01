using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using DataAccess.Abstract;
using System.Text.Json;

namespace Business.Concrete.Actions;

/// <summary>
/// trigger_workflow — Başka bir workflow definition'ını tetikler.
/// Maksimum zincir derinliği: MAX_CHAIN_DEPTH (3).
/// </summary>
public class TriggerWorkflowActionHandler : IWorkflowActionHandler
{
    private const int MAX_CHAIN_DEPTH = 3;

    private readonly IWorkflowOrchestrator    _orchestrator;
    private readonly IWorkflowDefinitionDal   _definitionDal;
    private readonly IWorkflowVersionDal      _versionDal;

    public string ActionCode => "trigger_workflow";

    public TriggerWorkflowActionHandler(
        IWorkflowOrchestrator  orchestrator,
        IWorkflowDefinitionDal definitionDal,
        IWorkflowVersionDal    versionDal)
    {
        _orchestrator  = orchestrator;
        _definitionDal = definitionDal;
        _versionDal    = versionDal;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "workflowDefinitionId", ActionCode, errors);
        return errors;
    }

    public async Task<IDataResult<object?>> ExecuteAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        // 1) Derinlik koruması
        if (context.ChainDepth >= MAX_CHAIN_DEPTH)
            return new ErrorDataResult<object?>(null,
                $"trigger_workflow: maksimum zincir derinliğine ulaşıldı ({MAX_CHAIN_DEPTH}). Sonsuz döngü koruması devreye girdi.");

        // 2) Definition ID
        if (!int.TryParse(parameters.GetValueOrDefault("workflowDefinitionId"), out var definitionId) || definitionId <= 0)
            return new ErrorDataResult<object?>(null, "trigger_workflow: geçerli workflowDefinitionId gerekli.");

        // 3) Kendini tetikleme koruması
        if (context.WorkflowRunId.HasValue)
        {
            // Definition ID ile mevcut run'ın definition ID'sini doğrudan karşılaştıramayız (run ID ayrı),
            // ancak aynı definition'ın başlatılmasını izin veriyoruz — döngü derinlik limitiyle korunuyor.
        }

        // 4) Definition + Version lookup
        var definition = _definitionDal.Get(d => d.Id == definitionId && d.IsActive);
        if (definition is null)
            return new ErrorDataResult<object?>(null,
                $"trigger_workflow: aktif definition bulunamadı (ID: {definitionId}).");

        if (definition.CurrentVersionId is null)
            return new ErrorDataResult<object?>(null,
                $"trigger_workflow: definition'ın aktif versiyonu yok (ID: {definitionId}).");

        var version = _versionDal.Get(v => v.Id == definition.CurrentVersionId.Value);
        if (version is null || version.Status != 1)
            return new ErrorDataResult<object?>(null,
                $"trigger_workflow: aktif version bulunamadı (VersionId: {definition.CurrentVersionId}).");

        // 5) Context klonu — ChainDepth artırılır
        var inheritContext = parameters.GetValueOrDefault("inheritContext", "true") != "false";
        RuleContext childContext;
        if (inheritContext)
        {
            // RuleContext bir class — JSON round-trip ile derin kopya yapılır
            var json = JsonSerializer.Serialize(context);
            childContext = JsonSerializer.Deserialize<RuleContext>(json) ?? new RuleContext();
            childContext.ChainDepth    = context.ChainDepth + 1;
            childContext.WorkflowRunId = null;
            childContext.ExecutedAt    = DateTime.UtcNow;
        }
        else
        {
            childContext = new RuleContext
            {
                ChainDepth = context.ChainDepth + 1,
                ExecutedAt = DateTime.UtcNow,
            };
        }

        var childContextJson = JsonSerializer.Serialize(childContext);

        // 6) Yeni run başlat
        var result = await _orchestrator.StartSingleAsync(
            definitionId:    definitionId,
            versionId:       version.Id,
            flowJson:        version.FlowJson ?? string.Empty,
            triggerEvent:    $"workflow.chained.{context.TriggerEventName}",
            institutionId:   context.InstitutionId ?? 0,
            contextJson:     childContextJson,
            eventId:         Guid.NewGuid(),
            triggeredByUserId: context.SystemUserId,
            isDryRun:        false);

        return result.Success
            ? new SuccessDataResult<object?>(
                new { definitionId, runId = result.Data?.Id, chainDepth = childContext.ChainDepth },
                $"Workflow tetiklendi: definition {definitionId} (zincir derinliği: {childContext.ChainDepth}).")
            : new ErrorDataResult<object?>(null,
                $"trigger_workflow başarısız: {result.Message}");
    }
}
