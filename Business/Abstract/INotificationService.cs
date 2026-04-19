using Core.Entities.Concrete;
using Core.Utilities.Results;
using System.Collections.Generic;

namespace Business.Abstract;

public interface INotificationService
{
    IDataResult<List<Notification>> GetUnreadByUserId(int userId);
    IDataResult<List<Notification>> GetAllByUserId(int userId, int page, int pageSize);
    IResult Add(Notification notification);
    IResult MarkAsRead(int notificationId, int userId);
    IResult MarkAllAsRead(int userId);
    int GetUnreadCount(int userId);
}
