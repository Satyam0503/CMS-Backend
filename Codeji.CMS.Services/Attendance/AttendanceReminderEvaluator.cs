using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Attendance;

public static class AttendanceReminderEvaluator
{
    public static readonly TimeSpan ReviewReminderTime = new(16, 0, 0);

    public static bool HasOfficeStarted(DateTime localReferenceTime, TimeSpan officeStartTime) =>
        localReferenceTime.TimeOfDay >= officeStartTime;

    public static bool IsReviewReminderDue(DateTime localReferenceTime) =>
        localReferenceTime.TimeOfDay >= ReviewReminderTime;

    public static IReadOnlyList<EmpUser> FindMissingAttendance(
        IEnumerable<EmpUser> employees,
        IEnumerable<AttendanceModel> attendanceRecords,
        DateTime date)
    {
        var attendanceByUser = attendanceRecords
            .Where(x => x.Date.Date == date.Date && !string.IsNullOrWhiteSpace(x.UserId))
            .GroupBy(x => x.UserId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(x => x.UserId!, StringComparer.OrdinalIgnoreCase);

        return employees
            .Where(x => x.Status && !string.IsNullOrWhiteSpace(x.UserId) && !attendanceByUser.ContainsKey(x.UserId!))
            .ToList();
    }
}
