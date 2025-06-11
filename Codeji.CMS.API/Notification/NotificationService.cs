using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.Notification;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly IMongoDbRepository<UserNotifications> _userNotification;
    private readonly IMongoDbRepository<Notifications> _notifications;

    public NotificationService(IHubContext<NotificationHub> _hubContext, IMongoDbRepository<UserNotifications> userNotifications, IMongoDbRepository<Notifications> notifications)
    {
        hubContext = _hubContext;
        _userNotification = userNotifications;
        _notifications = notifications;
    }

    public Task SendNoticeNotification(string groupName, NotificationViewModel model)
    {
        return hubContext.Clients.Group(groupName).SendAsync("noticeNofity", model);
    }
    public async Task<Result<NotificationViewModel>> GetAllNotifications(string userId)
    {
        IEnumerable<UserNotifications> userNotifications = await _userNotification.GetAll(x => x.UserId == userId);
        if (!userNotifications.Any())
        {
            return new Result<NotificationViewModel>()
            {
                MethodResults = [],
                Success = true,
                TotalRecords = 0
            };
        }
        string[] notificationsId = userNotifications.Select(x => x.NotificationId).ToArray();
        IEnumerable<Notifications> notifications = await _notifications.GetAll(x => notificationsId.Contains(x.NotificationId));
        var data = (from usrNft in userNotifications
                    join ntf in notifications on usrNft.NotificationId equals ntf.NotificationId
                    select new NotificationViewModel
                    {
                        UserNotificationId = usrNft.UserNotificationId,
                        IsRead = usrNft.IsRead,
                        Title = ntf.Title,
                        SentDateTime = ntf.CreatedDateTime,
                        SentBy = ntf.CreatedBy,
                        NotificationTypes = ntf.NotificationType
                    }).OrderByDescending(x => x.SentDateTime).ToList();
        return new Result<NotificationViewModel>()
        {
            MethodResults = data,
            Success = true,
            TotalRecords = data.Count
        };
    }
}

public interface INotificationService
{
    Task SendNoticeNotification(string groupName, NotificationViewModel notification);
    Task<Result<NotificationViewModel>> GetAllNotifications(string userId);
}