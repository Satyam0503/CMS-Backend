namespace Codeji.CMS.Utility.Helpers;
public class ConfigManager
{

    public static string FromEmail => ConfigurationHelper.config.GetSection("AppSettings:FromEmail")?.Value ?? "";
    public static string FromName => ConfigurationHelper.config.GetSection("AppSettings:FromName")?.Value ?? "";
    public static string BCCEmail => ConfigurationHelper.config.GetSection("AppSettings:BccEmail")?.Value ?? "";
    public static string BaseUrl => ConfigurationHelper.config.GetSection("AppSettings:BaseUrl")?.Value ?? "";

    public static string APIUrl => ConfigurationHelper.config.GetSection("AppSettings:APIUrl")?.Value ?? "";

    public static string Employee_ImageUrl => ConfigurationHelper.config.GetSection("AppSettings:Employee_ImageUrl")?.Value ?? "";
    public static string Is_For_Debug => ConfigurationHelper.config.GetSection("AppSettings:isForDebug")?.Value ?? "";
    public static string App_Version => ConfigurationHelper.config.GetSection("AppSettings:AppVersion")?.Value ?? "";
    public static string viewResumeUrl => ConfigurationHelper.config.GetSection("AppSettings:viewResumeUrl")?.Value ?? "";

    public static string SENDGRID_API_KEY => ConfigurationHelper.config.GetSection("AppSettings:SENDGRID_API_KEY")?.Value ?? "";
}

