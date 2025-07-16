using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Utility.Helpers;

public static class NotificationMessageTemplate
{
    public static string Create(NotificationTypes type)
    {
        return type switch
        {
            NotificationTypes.Notice =>
                $"New notice posted",
            NotificationTypes.LeaveRequest =>
                "New leave request",
            NotificationTypes.LeaveRequestApproved =>
                "Leave request approved",
            NotificationTypes.LeaveRequestReject =>
                "Leave request rejected",
            _ => "You have new notification",
        };
    }
}
