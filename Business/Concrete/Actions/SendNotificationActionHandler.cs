using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Core.Entities.Concrete;
using Entities.Concrete;

namespace Business.Concrete.Actions;

/// <summary>send_notification — Tekil bildirim gönderme action'ı.</summary>
public class SendNotificationActionHandler : IWorkflowActionHandler
{
    private readonly INotificationService _notificationService;

    public string ActionCode => "send_notification";

    public SendNotificationActionHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "title",   ActionCode, errors);
        WorkflowParameterResolver.RequireParam(parameters, "message", ActionCode, errors);
        return errors;
    }

    public Task<IDataResult<object?>> ExecuteAsync(Dictionary<string, string> parameters, RuleContext context)
    {
        var userId = WorkflowParameterResolver.ResolveUserId(parameters, context, "recipientType", "customUserId");

        var notification = new Notification
        {
            UserId        = userId,
            Title         = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("title"),   context),
            Message       = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("message"), context),
            Type          = parameters.GetValueOrDefault("type") ?? "info",
            ReferenceLink = parameters.TryGetValue("referenceLink", out var link)
                            ? WorkflowParameterResolver.Resolve(link, context) : null,
            IsRead        = false,
            CreatedAt     = DateTime.UtcNow
        };

        var result = _notificationService.Add(notification);

        return Task.FromResult<IDataResult<object?>>(result.Success
            ? new SuccessDataResult<object?>(new { userId, notification.Title }, "Bildirim oluşturuldu.")
            : new ErrorDataResult<object?>(null, $"Bildirim eklenemedi: {result.Message}"));
    }
}
