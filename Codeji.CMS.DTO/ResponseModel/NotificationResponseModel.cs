
namespace Codeji.CMS.DTO.ResponseModel
{
    public class NotificationResponseModel
    {
        public List<NotificationViewModel> NotificationList { get; set; }
        public int All { get; set; }
        public int Unread { get; set; }
    }
}