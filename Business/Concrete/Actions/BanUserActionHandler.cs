using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Utilities.Results;
using Core.Entities.Concrete;
using Entities.Concrete;

namespace Business.Concrete.Actions;

/// <summary>ban_user — Kullanıcı banlama action'ı.</summary>
public class BanUserActionHandler : IWorkflowActionHandler
{
    private readonly IUserService         _userService;
    private readonly INotificationService _notificationService;

    public string ActionCode => "ban_user";

    public BanUserActionHandler(IUserService userService, INotificationService notificationService)
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
        var userId   = WorkflowParameterResolver.ResolveUserId(parameters, context);
        var reason   = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("reason"), context);
        var duration = parameters.TryGetValue("durationDays", out var d)
                       && int.TryParse(d, out var days) ? days : 0;
        var notify   = parameters.GetValueOrDefault("notifyUser") != "false";

        var result = _userService.BanUser(userId);
        if (!result.Success)
            return Task.FromResult<IDataResult<object?>>(
                new ErrorDataResult<object?>(null, $"Kullanıcı banlanamadı: {result.Message}"));

        if (notify)
        {
            var banMessage = duration > 0
                ? $"Hesabınız {duration} gün boyunca askıya alınmıştır. Sebep: {reason}"
                : $"Hesabınız kalıcı olarak askıya alınmıştır. Sebep: {reason}";

            _notificationService.Add(new Notification
            {
                UserId    = userId,
                Title     = "Hesabınız Askıya Alındı",
                Message   = banMessage,
                Type      = "error",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        return Task.FromResult<IDataResult<object?>>(
            new SuccessDataResult<object?>(new { userId, reason, duration },
                $"Kullanıcı banlandı (ID: {userId})."));
    }
}
