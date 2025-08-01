using Codeji.CMS.DTO.ResponseModel;
namespace Codeji.CMS.GenericRepository.Interfaces;

public interface INotificationService
{
    Task SendNotificationToUser(string userId, NotificationViewModel notification);
}