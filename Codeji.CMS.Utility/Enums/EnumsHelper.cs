using System;
namespace Codeji.CMS.Utility.Enums
{
    public static class EnumsHelper
    {
        public enum StatusEnums
        {
            Inprogress = 1,
            OnHold = 2,
            Shortlisted = 3,
            Rejected = 4
        }
        public enum ActivityTypeEnums
        {
            New = 0,
            Active = 1,
            Inactive = 2,
            Closed = 3
        }
        public enum Roles
        {
            Administrator = 1,
            HR=2,
            Employee=3
        }
    }
}

