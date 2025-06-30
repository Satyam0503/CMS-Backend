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
            CreateNewPasswordMail = 0,
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
        }

        public enum HolidayTypes
        {
            Cultural = 1,
            Health = 2,
            Religious = 3,
            National = 4,
        }

        public enum LeaveTypes
        {
            Paid = 1,
            UnPaid = 2,
        }

        public enum LeaveDuration
        {
            FullDay = 1,
            HalfDay = 2,
        }
    }
}

