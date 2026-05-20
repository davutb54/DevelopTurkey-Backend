using Business.Abstract;
using Core.Entities.Concrete;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;

namespace WebAPI.SignalR;

public class SignalRLiveNotificationManager : ILiveNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRLiveNotificationManager(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(Notification notification)
    {
        await _hubContext.Clients.User(notification.UserId.ToString()).SendAsync("ReceiveNotification", notification);
    }
}
