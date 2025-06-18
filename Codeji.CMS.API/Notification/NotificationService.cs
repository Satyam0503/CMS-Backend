using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.Notification;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> hubContext;

    public NotificationService(IHubContext<NotificationHub> _hubContext)
    {
        hubContext = _hubContext;
    }

    // hub methods are called by connected clients
    public Task SendNoticeNotificationToUser(string userId, NotificationViewModel model)
    {
        return hubContext.Clients.User(userId).SendAsync("noticeNofity", model);
    }
}
