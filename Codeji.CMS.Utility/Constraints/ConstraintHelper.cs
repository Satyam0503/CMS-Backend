using System;
namespace Codeji.CMS.Utility.Constraints
{
    public static class AppModule
    {
        public const string Dashboard = "Dashboard";
        public const string Employees = "Employees";
        public const string Attendance = "Attendance";
        public const string LeaveManagement = "Leave_Management";
        public const string Calendar = "Calendar";
        public const string NoticeBoard = "Notice_Board";
        public const string Jobs = "Jobs";
        public const string Applications = "Applications";
        public const string ProcessLog = "Process_Log";
        public const string PayRoll = "PayRoll";
        public const string Policy = "Policy";
        public const string PayrollSettings = "Payroll_Settings";
        public const string CareerProfile = "Career_Profile";
        public const string WorkFromHome = "Work_From_Home";
    }

    public static class Permission
    {
        public const string View = "View";
        public const string Create = "Create";
        public const string Edit = "Edit";
        public const string Delete = "Delete";
        public const string ViewOwn = "ViewOwn";
        public const string CreateOwn = "CreateOwn";
        public const string EditOwn = "EditOwn";
        public const string CancelOwn = "CancelOwn";
        public const string ViewTeam = "ViewTeam";
        public const string ApproveTeam = "ApproveTeam";
        public const string ViewAll = "ViewAll";
        public const string CreateForEmployee = "CreateForEmployee";
        public const string Override = "Override";
        public const string Revoke = "Revoke";
        public const string PolicyView = "PolicyView";
        public const string PolicyEdit = "PolicyEdit";
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
