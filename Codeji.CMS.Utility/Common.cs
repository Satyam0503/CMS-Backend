using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Utility
{
    public static class Common
    {
        public static string GetApplicantResumeFullPath(string resumeUrl)
        {
            if (string.IsNullOrEmpty(resumeUrl))
                return string.Empty;
            return ConfigManager.AppSettings.AppUrl + ConfigManager.FileSettings.ViewResumeUrl + resumeUrl;
        }

        public static string GetEmployeeImageUrl(string profileUrl)
        {
            if (string.IsNullOrEmpty(profileUrl))
                return string.Empty;
            return ConfigManager.AppSettings.AppUrl + ConfigManager.FileSettings.Employee_ImageUrl + profileUrl;
        }
        public static string GetCompanyLogoUrl(string logoUrl)
        {
            if (string.IsNullOrEmpty(logoUrl))
                return string.Empty;
            return ConfigManager.AppSettings.AppUrl + ConfigManager.FileSettings.CompanyLogoUrl + logoUrl;
        }
    }
}
