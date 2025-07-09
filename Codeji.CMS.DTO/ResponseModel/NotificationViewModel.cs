using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.ResponseModel
{
    public class NotificationViewModel
    {
        public string UserNotificationId { get; set; }
        public bool IsRead { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime SentDateTime { get; set; }
        public string SentBy { get; set; }
        public EnumsHelper.NotificationTypes NotificationTypes { get; set; }
    }
}