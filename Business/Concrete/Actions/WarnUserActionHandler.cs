using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Core.Entities.Concrete;
using Entities.Concrete;

namespace Business.Concrete.Actions;

/// <summary>warn_user — Kullanıcıya uyarı verme action'ı.</summary>
public class WarnUserActionHandler : IWorkflowActionHandler
{
    private readonly IUserWarningService  _userWarningService;
    private readonly INotificationService _notificationService;

    public string ActionCode => "warn_user";

    public WarnUserActionHandler(
        IUserWarningService userWarningService,
        INotificationService notificationService)
    {
        _userWarningService  = userWarningService;
        _notificationService = notificationService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "userTarget", ActionCode, errors);
        return errors;
    }

    public Task<IDataResult<object?>> ExecuteAsync(Dictionary<string, string> parameters, RuleContext context)
    {
        var userId   = WorkflowParameterResolver.ResolveUserId(parameters, context);
        var title    = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("title"),   context);
        var message  = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("message"), context);
        var severity = parameters.GetValueOrDefault("severity") ?? "medium";

        var warning = new UserWarning
        {
            UserId          = userId,
            IssuedByAdminId = context.SystemUserId,
            Title           = string.IsNullOrWhiteSpace(title)   ? "Uyarı" : title,
            Message         = string.IsNullOrWhiteSpace(message) ? context.TriggerEventName : message,
            Severity        = severity,
            IsActive        = true,
            IssuedAt        = DateTime.UtcNow
        };

        var result = _userWarningService.Issue(warning);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Uyarı verilemedi: {result.Message}"));

        _notificationService.Add(new Notification
        {
            UserId    = userId,
            Title     = $"Uyarı: {warning.Title}",
            Message   = warning.Message,
            Type      = severity == "high" ? "error" : "warning",
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { userId, severity },
                $"Kullanıcıya uyarı verildi (ID: {userId})."));
    }
}
