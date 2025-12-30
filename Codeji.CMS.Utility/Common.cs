using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Utility
{
    public static class Common
    {
        public static string? GetApplicantResumeFullPath(string? resumeUrl)
        {
            if (string.IsNullOrEmpty(resumeUrl))
                return null;
            return ConfigManager.AppSettings.APIUrl + ConfigManager.FileSettings.ViewResumeUrl + resumeUrl;
        }

        public static string? GetEmployeeImageUrl(string? profileUrl)
        {
            if (string.IsNullOrEmpty(profileUrl))
                return null;
            return ConfigManager.AppSettings.APIUrl + ConfigManager.FileSettings.Employee_ImageUrl + profileUrl;
        }
        public static string? GetCompanyLogoUrl(string? logoUrl)
        {
            if (string.IsNullOrEmpty(logoUrl))
                return null;
            return ConfigManager.AppSettings.APIUrl + ConfigManager.FileSettings.CompanyLogoUrl + logoUrl;
        }
        public static string GetCalendarItemCoverImagePath(string imageUrl)
        {
            return ConfigManager.AppSettings.APIUrl + ConfigManager.FileSettings.CalendarItemCoverImage + imageUrl;
        }
        public static string GetPolicyDocumentPath(string docName)
        {
            return ConfigManager.AppSettings.APIUrl + ConfigManager.FileSettings.PolicyDocument + docName;
        }
    }
}
