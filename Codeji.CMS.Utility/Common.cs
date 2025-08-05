using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Utility
{
    public static class Common
    {
        public static string GetApplicantResumeFullPath(string resumeUrl)
        {
            if (string.IsNullOrEmpty(resumeUrl))
                return string.Empty;
            return ConfigManager.AppSettings.APIUrl + ConfigManager.FileSettings.ViewResumeUrl + resumeUrl;
        }

        public static string GetEmployeeImageUrl(string? profileUrl)
        {
            if (string.IsNullOrEmpty(profileUrl))
                return ConfigManager.AppSettings.APIUrl + ConfigManager.FileSettings.Default_User_Image;
            return ConfigManager.AppSettings.APIUrl + ConfigManager.FileSettings.Employee_ImageUrl + profileUrl;
        }
        public static string GetCompanyLogoUrl(string? logoUrl)
        {
            if (string.IsNullOrEmpty(logoUrl))
                return ConfigManager.AppSettings.APIUrl + ConfigManager.FileSettings.Default_Company_logo;
            return ConfigManager.AppSettings.APIUrl + ConfigManager.FileSettings.CompanyLogoUrl + logoUrl;
        }
        public static string GetHolidayCoverImagePath(string imageUrl)
        {
            return ConfigManager.AppSettings.APIUrl + ConfigManager.FileSettings.HolidayCoverImage + imageUrl;
        }
    }
}
