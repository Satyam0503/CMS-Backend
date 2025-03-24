namespace Codeji.CMS.Utility.Helpers;
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

public class ConfigManager
{

    public static string FromEmail => ConfigurationHelper.config.GetSection("AppSettings:FromEmail")?.Value ?? "";
    public static string FromName => ConfigurationHelper.config.GetSection("AppSettings:FromName")?.Value ?? "";
    public static string BCCEmail => ConfigurationHelper.config.GetSection("AppSettings:BccEmail")?.Value ?? "";
    public static string AppUrl => ConfigurationHelper.config.GetSection("AppSettings:BaseUrl")?.Value ?? "";

    public static string APIUrl => ConfigurationHelper.config.GetSection("AppSettings:APIUrl")?.Value ?? "";

    public static string Employee_ImageUrl => ConfigurationHelper.config.GetSection("AppSettings:Employee_ImageUrl")?.Value ?? "";
    public static string Is_For_Debug => ConfigurationHelper.config.GetSection("AppSettings:isForDebug")?.Value ?? "";
    public static string App_Version => ConfigurationHelper.config.GetSection("AppSettings:AppVersion")?.Value ?? "";
    public static string viewResumeUrl => ConfigurationHelper.config.GetSection("AppSettings:viewResumeUrl")?.Value ?? "";
    public static string SENDGRID_API_KEY => ConfigurationHelper.config.GetSection("AppSettings:SENDGRID_API_KEY")?.Value ?? "";
    public static string LocalAuthUrl => ConfigurationHelper.config.GetSection("AppSettings:LocalAuthUrl")?.Value ?? "";
    public static string RecaptchSecretKey => ConfigurationHelper.config.GetSection("reCaptcha:SecretKey")?.Value ?? "";




}

