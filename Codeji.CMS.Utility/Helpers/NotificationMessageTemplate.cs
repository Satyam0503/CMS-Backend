using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Utility.Helpers;

public static class NotificationMessageTemplate
{
    public static string Create(NotificationTypes type, string title)
    {
        return type switch
        {
            NotificationTypes.Notice =>
                $"New notice posted: {title}",
            _ => "You have new notification",
        };
    }
}
