using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.Notification;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> hubContext;

    public NotificationService(IHubContext<NotificationHub> _hubContext)
    {
        hubContext = _hubContext;
    }

    /// <summary>
    /// sent gallery update notification 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="Notify"></param>
    /// <returns></returns>

    public Task AddGalleryNotifyProgress(string userId, UserNotificationModel Notify)
    {
        return hubContext.Clients.User(userId).SendAsync("galleryNotify", Notify);
    }


}
public interface INotificationService
{
    Task AddGalleryNotifyProgress(string userId, UserNotificationModel Notify);

}