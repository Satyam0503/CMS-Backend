using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.Company;

public sealed class CompanyNotificationPreferencesDto
{
    public Dictionary<EnumsHelper.NotificationPreferenceType, bool> Preferences { get; set; } = new();
    public CompanyNotificationConfigurationDto Configuration { get; set; } = new();
}
public sealed class CompanyNotificationConfigurationDto
{
    public string Position { get; set; } = "top-right";
    public int AutoClose { get; set; } = 3500;
    public int Limit { get; set; } = 2;
    public bool NewestOnTop { get; set; } = true;
    public bool PauseOnHover { get; set; } = true;
    public bool ShowCloseButton { get; set; } = true;
    public bool CloseOnClick { get; set; }
    public string Transition { get; set; } = "slide";
    public string Theme { get; set; } = "system";
}
