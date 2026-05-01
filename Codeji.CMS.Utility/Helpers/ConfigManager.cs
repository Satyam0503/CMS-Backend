using Microsoft.Extensions.Configuration;

namespace Codeji.CMS.Utility.Helpers;

public class ConfigManager
{
    public static void Initialize(IConfiguration configuration)
    {
        AppSettings = configuration.GetSection("AppSettings").Get<AppSettings>()
            ?? throw new InvalidOperationException("AppSettings section is missing in appsettings.json");

        FileSettings = configuration.GetSection("FileSettings").Get<FileSettings>()
            ?? throw new InvalidOperationException("FileSettings section is missing in appsettings.json");

        EmailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>()
            ?? throw new InvalidOperationException("EmailSettings section is missing in appsettings.json");

        ReCaptcha = configuration.GetSection("reCaptcha").Get<ReCaptchaSettings>()
            ?? throw new InvalidOperationException("reCaptcha section is missing in appsettings.json");

        Jwt = configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt section is missing in appsettings.json");
    }

    public static AppSettings AppSettings;
    public static FileSettings FileSettings;
    public static EmailSettings EmailSettings;
    public static ReCaptchaSettings ReCaptcha;
    public static JwtSettings Jwt;
}


public class AppSettings
{
    public bool IsForDebug { get; set; }
    public string AppVersion { get; set; }
    public string APIUrl { get; set; }
    public string AppUrl { get; set; }
}

public class FileSettings
{
    public string UploadUrl { get; set; }
    public string Employee_ImageUrl { get; set; }
    public string ViewResumeUrl { get; set; }
    public string CompanyLogoUrl { get; set; }
    public string CalendarItemCoverImage { get; set; }
    public string PolicyDocument { get; set; }
}

public class EmailSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string FromName { get; set; }
    public string FromEmail { get; set; }
    public string BccEmail { get; set; }
    public string SupportEmail { get; set; }
    public string SecretKey { get; set; }
}

public class ReCaptchaSettings
{
    public string SecretKey { get; set; }
}

public class JwtSettings
{
    public string SecretKey { get; set; }
    public int Expiry { get; set; }
}
