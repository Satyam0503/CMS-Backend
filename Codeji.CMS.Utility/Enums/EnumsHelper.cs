namespace Codeji.CMS.Utility.Enums
{
    public static class EnumsHelper
    {
        public enum ClaimTypesEnum
        {
            CompanyId,
            role_id,
            UserPermissionRole,
            user_id,
            company_id,
            admin_id,
            admin_company_id,
            role_type
        }
        public enum ActivityType
        {
            New = 0,
            InProgress = 1,
            OnHold = 2,
            Shortlisted = 3,
            Selected = 4,
            Rejected = 5,
            ReApply = 6
        }
        public enum ActivityStatus
        {
            Active = 1,
            InActive = 0,
        }
        public enum Roles
        {
            Administrator = 1,
            HR = 2,
            Employee = 3
        }
        public enum MailType
        {
            EmployeeWelcomeMail = 0,
            SelectedMail = 1,
            RejectedMail = 2,
            ApplyNowMailToHR = 3,
            ApplyNowMailToApplicant = 4,
            ContactUsMail = 5,
            ContactUsMailToHR = 6,
            LeaveMailToHR = 7,
            LeaveReplyMail = 8,
            ResignationMail = 9,
            ResetPassword = 10,
        }
        public enum NoticeType
        {
            General = 1,
            Important = 2,
            Urgent = 3
        }
        public enum NotificationTypes
        {
            Notice = 1,
            LeaveRequest = 2,
            LeaveRequestApproved = 3,
            LeaveRequestReject = 4,
            BirthDay = 5,
            WorkAnniversary = 6
        }
        public enum NotificationPreferenceType
        {
            BirthdayNotification,
            WorkAnniversaries,
            LeaveStatusUpdate,
            Notice,
            HolidayReminder,
            MessageNotification,
        }

        public enum CalendarItem
        {
            Holiday = 1,
            Event = 2,
        }
        public enum CalendarResponseItem
        {
            Holiday = 1,
            Event = 2,
            Birthday = 3,
            WorkAnniversary = 4
        }

        public enum LeaveRequestStatus
        {
            Pending = 1,
            Accepted = 2,
            Rejected = 3,
            WithDrawn = 4
        }
        public enum LeaveTypes
        {
            Sick = 1,
            Casual = 2,
            Earned = 3,
            Maternity = 4,
            Paternity = 5,
        }
        public enum LeaveAccrualPeriod
        {
            Monthly = 1,
            Yearly,
            None,
        }
    }
}

