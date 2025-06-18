using Codeji.CMS.DTO.ResponseModel;
namespace Codeji.CMS.GenericRepository.Interfaces;

public interface INotificationService
{
    Task SendNoticeNotificationToUser(string userId, NotificationViewModel notification);

}