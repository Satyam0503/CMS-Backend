using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.Notification
{
    [Authorize]
    public class NotificationHub : Hub
    {

    }

    public class UserNotificationModel
    {
        public string UserNotificationId { get; set; }
        public string SentFrom { get; set; }
        public string SentTo { get; set; }
        public int NotificationType { get; set; }
        public DateTime? SentDate { get; set; }
        public string TaskId { get; set; }
        public bool IsRead { get; set; }
        public string IsNotifiedByEmail { get; set; }
    }

}