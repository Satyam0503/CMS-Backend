using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Codeji.CMS.API.Notification
{
    /// <summary>
    /// Sent notification to live users
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        /// <summary>
        /// sent gallery update notification 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="Notify"></param>
        /// <returns></returns>

        public Task AddGalleryNotifyProgress(string userId, UserNotificationModel Notify)
        {
            return Clients.User(userId).SendAsync("galleryNotify", Notify);
        }
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