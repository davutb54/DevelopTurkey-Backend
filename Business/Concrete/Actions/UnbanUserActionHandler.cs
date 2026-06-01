using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Core.Entities.Concrete;
using Entities.Concrete;

namespace Business.Concrete.Actions;

/// <summary>unban_user — Kullanıcı ban kaldırma action'ı.</summary>
public class UnbanUserActionHandler : IWorkflowActionHandler
{
    private readonly IUserService         _userService;
    private readonly INotificationService _notificationService;

    public string ActionCode => "unban_user";

    public UnbanUserActionHandler(IUserService userService, INotificationService notificationService)
    {
        _userService         = userService;
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
        var userId = WorkflowParameterResolver.ResolveUserId(parameters, context);
        var notify = parameters.GetValueOrDefault("notifyUser") != "false";

        var result = _userService.UnbanUser(userId);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Ban kaldırılamadı: {result.Message}"));

        if (notify)
        {
            _notificationService.Add(new Notification
            {
                UserId    = userId,
                Title     = "Hesabınız Yeniden Aktif",
                Message   = "Hesabınızdaki askıya alma işlemi kaldırılmıştır. Platformumuzu tekrar kullanabilirsiniz.",
                Type      = "success",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { userId },
                $"Kullanıcı yasağı kaldırıldı (ID: {userId})."));
    }
}
