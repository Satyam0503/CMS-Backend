namespace Codeji.CMS.Utility.Enums
{
    public static class EnumsHelper
    {
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
            SelectedMail = 1,
            RejectedMail = 2,
            ApplyNowMailToHR = 3,
            ApplyNowMailToApplicant = 4,
            ContactUsMail = 5,
            ContactUsMailToHR = 6,
            LeaveMailToHR = 7,
            LeaveReplyMail = 8,
            ResignationMail = 9,
        }
    }
}

