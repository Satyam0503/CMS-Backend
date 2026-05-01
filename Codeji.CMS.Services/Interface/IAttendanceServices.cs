using Codeji.CMS.DTO.Attendance;

namespace Codeji.CMS.Services.Interface
{
    public interface IAttendanceService
    {
        Task<AttendanceSummary> GetAttendanceSummary(string employeeId, DateTime fromDate, DateTime toDate);
    }
}
