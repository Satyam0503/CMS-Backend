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
            _ => "You have new notification",
        };
    }
}
