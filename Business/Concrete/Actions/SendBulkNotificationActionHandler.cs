using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Core.Entities.Concrete;
using Entities.Concrete;

namespace Business.Concrete.Actions;

/// <summary>send_bulk_notification — Kurum üyelerine toplu bildirim gönderme action'ı.</summary>
public class SendBulkNotificationActionHandler : IWorkflowActionHandler
{
    private readonly IUserService         _userService;
    private readonly INotificationService _notificationService;

    public string ActionCode => "send_bulk_notification";

    public SendBulkNotificationActionHandler(
        IUserService userService,
        INotificationService notificationService)
    {
        _userService         = userService;
        _notificationService = notificationService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "title",   ActionCode, errors);
        WorkflowParameterResolver.RequireParam(parameters, "message", ActionCode, errors);
        return errors;
    }

    public async Task<IDataResult<object?>> ExecuteAsync(Dictionary<string, string> parameters, RuleContext context)
    {
        var title   = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("title"),   context);
        var message = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("message"), context);
        var type    = parameters.GetValueOrDefault("type") ?? "info";

        var allUsersResult = _userService.GetAll();
        if (!allUsersResult.Success || allUsersResult.Data == null)
            return new ErrorDataResult<object?>(null, "Kullanıcı listesi alınamadı.");

        var users = allUsersResult.Data
            .Where(u => !u.IsBanned && !u.IsDeleted)
            .Where(u => context.InstitutionId == null || u.InstitutionId == context.InstitutionId)
            .ToList();

        int sent = 0;
        foreach (var user in users)
        {
            var n = new Notification
            {
                UserId    = user.Id,
                Title     = title,
                Message   = message,
                Type      = type,
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            };
            if (_notificationService.Add(n).Success) sent++;

            if (sent % 50 == 0) await Task.Yield();
        }

        return new SuccessDataResult<object?>(
            new { sent, total = users.Count },
            $"Toplu bildirim gönderildi: {sent}/{users.Count}");
    }
}
