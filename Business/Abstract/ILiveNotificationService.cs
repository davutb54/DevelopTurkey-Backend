using Core.Entities.Concrete;
using Core.Utilities.Results;

namespace Business.Abstract;

public interface ILiveNotificationService
{
    Task SendNotificationAsync(Notification notification);
}
