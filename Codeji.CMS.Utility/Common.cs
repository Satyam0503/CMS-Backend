using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Utility
{
    public class Common
    {
        public static string GetApplicantResumeFullPath(string resumeUrl)
        {
            if (string.IsNullOrEmpty(resumeUrl))
                return string.Empty;
            return ConfigManager.APIUrl + ConfigManager.viewResumeUrl + resumeUrl;
        }

        public static string GetEmployeeImageUrl(string profileUrl)
        {
            if (string.IsNullOrEmpty(profileUrl))
                return string.Empty;
            return ConfigManager.APIUrl + ConfigManager.Employee_ImageUrl + profileUrl;
        }
    }
}
