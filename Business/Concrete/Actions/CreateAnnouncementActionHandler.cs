using Business.Abstract;
using Business.Concrete.Actions.Helpers;
using Business.Models;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Entities.DTOs;

namespace Business.Concrete.Actions;

/// <summary>create_announcement — Platform duyurusu oluşturur; opsiyonel toplu bildirim gönderir.</summary>
public class CreateAnnouncementActionHandler : IWorkflowActionHandler
{
    private readonly IAnnouncementService _announcementService;
    private readonly IUserService         _userService;
    private readonly INotificationService _notificationService;

    public string ActionCode => "create_announcement";

    public CreateAnnouncementActionHandler(
        IAnnouncementService announcementService,
        IUserService         userService,
        INotificationService notificationService)
    {
        _announcementService = announcementService;
        _userService         = userService;
        _notificationService = notificationService;
    }

    public IReadOnlyList<string> ValidateParameters(Dictionary<string, string> parameters)
    {
        var errors = new List<string>();
        WorkflowParameterResolver.RequireParam(parameters, "title",   ActionCode, errors);
        WorkflowParameterResolver.RequireParam(parameters, "content", ActionCode, errors);
        return errors;
    }

    public async Task<IDataResult<object?>> ExecuteAsync(
        Dictionary<string, string> parameters, RuleContext context)
    {
        var title       = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("title"),   context);
        var content     = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("content"), context);
        var targetGroup = parameters.GetValueOrDefault("targetGroup", "all");
        var link        = WorkflowParameterResolver.Resolve(parameters.GetValueOrDefault("link"),    context);
        var type        = parameters.GetValueOrDefault("type", "info");

        var institutionIdRaw = WorkflowParameterResolver.Resolve(
            parameters.GetValueOrDefault("institutionId"), context);
        int? institutionId = int.TryParse(institutionIdRaw, out var iid) ? iid : null;

        var expiresAtRaw = parameters.GetValueOrDefault("expiresAt");
        DateTime? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(expiresAtRaw) &&
            DateTime.TryParse(expiresAtRaw, out var parsed))
            expiresAt = parsed.ToUniversalTime();

        var createResult = _announcementService.Create(new CreateAnnouncementDto
        {
            Title         = title,
            Content       = content,
            TargetGroup   = targetGroup,
            InstitutionId = institutionId,
            Link          = string.IsNullOrWhiteSpace(link) ? null : link,
            ExpiresAt     = expiresAt,
        }, context.SystemUserId);

        if (!createResult.Success)
            return new ErrorDataResult<object?>(null,
                $"create_announcement başarısız: {createResult.Message}");

        var sendNotif = parameters.GetValueOrDefault("sendNotification", "false") != "false";
        if (sendNotif)
        {
            var allUsersResult = _userService.GetAll();
            if (allUsersResult.Success && allUsersResult.Data != null)
            {
                var users = allUsersResult.Data.Where(u => !u.IsBanned && !u.IsDeleted).ToList();
                if (institutionId.HasValue)
                    users = users.Where(u => u.InstitutionId == institutionId.Value).ToList();

                int sent = 0;
                foreach (var user in users)
                {
                    _notificationService.Add(new Notification
                    {
                        UserId    = user.Id,
                        Title     = title,
                        Message   = content,
                        Type      = type,
                        IsRead    = false,
                        CreatedAt = DateTime.UtcNow,
                    });
                    if (++sent % 50 == 0) await Task.Yield();
                }
            }
        }

        return new SuccessDataResult<object?>(new { title, targetGroup },
            $"Duyuru oluşturuldu: '{title}'.");
    }
}
