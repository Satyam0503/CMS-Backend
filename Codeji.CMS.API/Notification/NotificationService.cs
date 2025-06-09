using Codeji.CMS.DTO.NoticeBoard;
using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.Notification;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> hubContext;

    public NotificationService(IHubContext<NotificationHub> _hubContext)
    {
        hubContext = _hubContext;
    }

    public Task SendNoticeNotification(AddNoticeRequestModel notice)
    {
        return hubContext.Clients.All.SendAsync("noticeNofity", notice);
    }
}
public interface INotificationService
{
    Task SendNoticeNotification(AddNoticeRequestModel notice);
}