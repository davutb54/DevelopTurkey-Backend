using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;

namespace Business.Concrete.Actions;

/// <summary>log_event — Sistem log kaydı oluşturma action'ı.</summary>
public class LogEventActionHandler : IWorkflowActionHandler
{
    private readonly ILogService _logService;

    public string ActionCode => "log_event";

    public LogEventActionHandler(ILogService logService)
    {
        _logService = logService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "message", ActionCode, errors);
        return errors;
    }

    public Task<IDataResult<object?>> ExecuteAsync(Dictionary<string, string> parameters, RuleContext context)
    {
        var category = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("category"), context);
        var action   = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("action"),   context);
        var message  = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("message"),  context);
        var details  = parameters.TryGetValue("details",  out var d)   ? WorkflowParameterResolver.Resolve(d, context)   : null;
        var severity = parameters.TryGetValue("severity", out var sev) ? WorkflowParameterResolver.Resolve(sev, context) : "Info";

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
            new SuccessDataResult<object?>(
                new { category = effectiveCategory, action = effectiveAction, message, severity },
                "Olay loglandı."));
    }
}
