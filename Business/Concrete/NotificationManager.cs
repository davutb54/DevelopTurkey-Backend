using Business.Abstract;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using System.Collections.Generic;
using System.Linq;

namespace Business.Concrete;

public class NotificationManager : INotificationService
{
    private readonly INotificationDal _notificationDal;
    private readonly ILogService _logService;

    public NotificationManager(INotificationDal notificationDal, ILogService logService)
    {
        _notificationDal = notificationDal;
        _logService = logService;
    }

    public IResult Add(Notification notification)
    {
        _notificationDal.Add(notification);
        _logService.LogInfo("System", "SendNotification", $"Kullanıcıya (ID: {notification.UserId}) yeni bildirim: {notification.Title}");
        return new SuccessResult("Bildirim eklendi.");
    }

    public IDataResult<List<Notification>> GetAllByUserId(int userId, int page, int pageSize)
    {
        var notifications = _notificationDal.GetAll(n => n.UserId == userId)
                                            .OrderByDescending(n => n.CreatedAt)
                                            .Skip((page - 1) * pageSize)
                                            .Take(pageSize)
                                            .ToList();

        return new SuccessDataResult<List<Notification>>(notifications, "Bildirimler listelendi.");
    }

    public IDataResult<List<Notification>> GetUnreadByUserId(int userId)
    {
        var notifications = _notificationDal.GetAll(n => n.UserId == userId && !n.IsRead)
                                            .OrderByDescending(n => n.CreatedAt)
                                            .ToList();

        return new SuccessDataResult<List<Notification>>(notifications, "Okunmamış bildirimler listelendi.");
    }

    public int GetUnreadCount(int userId)
    {
        return _notificationDal.Count(n => n.UserId == userId && !n.IsRead);
    }

    public IResult MarkAllAsRead(int userId)
    {
        var notifications = _notificationDal.GetAll(n => n.UserId == userId && !n.IsRead);
        
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            _notificationDal.Update(notification);
        }

        if (notifications.Any())
        {
            _logService.LogInfo("System", "MarkAllAsRead", $"Kullanıcı (ID: {userId}) tüm bildirimleri okudu.");
        }

        return new SuccessResult("Tüm bildirimler okundu olarak işaretlendi.");
    }

    public IResult MarkAsRead(int notificationId, int userId)
    {
        var notification = _notificationDal.Get(n => n.Id == notificationId && n.UserId == userId);
        
        if (notification == null)
        {
            _logService.LogWarning("System", "MarkAsReadError", $"Geçersiz bildirim okuma denemesi. Bildirim ID: {notificationId}, Kullanıcı ID: {userId}");
            return new ErrorResult("Bildirim bulunamadı veya yetkiniz yok.");
        }

        notification.IsRead = true;
        _notificationDal.Update(notification);

        _logService.LogInfo("System", "MarkAsRead", $"Kullanıcı (ID: {userId}), {notificationId} ID'li bildirimi okudu.");

        return new SuccessResult("Bildirim okundu olarak işaretlendi.");
    }
}
