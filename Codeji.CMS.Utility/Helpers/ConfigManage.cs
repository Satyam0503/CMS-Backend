namespace Codeji.CMS.Utility.Helpers;
public class ConfigManager
{
    public static AppConfiguration Settings { get; private set; } = new();

    public static void Initialize(AppConfiguration settings)
    {
        Settings = settings;
        AppSettings = settings.AppSettings;
        FileSettings = settings.FileSettings;
        EmailSettings = settings.EmailSettings;
        ReCaptcha = settings.ReCaptcha;
    }
    public static AppSettings AppSettings;
    public static FileSettings FileSettings;
    public static EmailSettings EmailSettings;
    public static ReCaptchaSettings ReCaptcha;

}
public class AppConfiguration
{
    public AppSettings AppSettings { get; set; }
    public FileSettings FileSettings { get; set; }
    public EmailSettings EmailSettings { get; set; }
    public ReCaptchaSettings ReCaptcha { get; set; }
}


public class AppSettings
{
    public string IsForDebug { get; set; }
    public string AppVersion { get; set; }
    public string APIUrl { get; set; }
    public string AppUrl { get; set; }

}

public class FileSettings
{
    public string UploadUrl { get; set; }
    public string Employee_ImageUrl { get; set; }
    public string ViewResumeUrl { get; set; }
    public string Default_User_Image { get; set; }
}

public class EmailSettings
{
    public string FromName { get; set; }
    public string FromEmail { get; set; }
    public string BccEmail { get; set; }
    public string SupportEmail { get; set; }
    public string SENDGRID_API_KEY { get; set; }
}

public class ReCaptchaSettings
{
    public string SecretKey { get; set; }
}



