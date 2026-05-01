    namespace Codeji.CMS.DTO.Attendance
    {
        public class AttendanceSummary
        {
            public int FullDayLeaves { get; set; } = 0;
            public int HalfDayLeaves { get; set; } = 0;
            public int LateCount { get; set; } = 0;
            public int EarlyExitCount { get; set; } = 0;

        }
    }
