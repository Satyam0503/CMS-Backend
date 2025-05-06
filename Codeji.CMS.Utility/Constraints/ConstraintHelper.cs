using System;
namespace Codeji.CMS.Utility.Constraints
{
    public struct AppModule
    {
        public static string Employees = "Employee";
        public static string Applicants = "Applicant";
        public static string JobPostings = "JobPosting";
    }
    public static class ConstraintHelper
    {
        public static readonly HashSet<string> AllowedSanitizerTags = new() { "p", "strong", "em", "u", "i", "s", "h1", "h2", "h3", "h4", "h5", "h6", "ol", "ul", "li", "span", "br" };
        public static readonly HashSet<string> AllowedSanitizerAttributes = new() { "style", "class" };


    }

    public static class Languages
    {
        public static readonly string English = "en";
        public static readonly string Mandarin = "zh";
        public static readonly string Spanish = "es";
        public static readonly string Japanese = "ja";
        public static readonly string German = "de";
        public static readonly string French = "fr";
    }
}
