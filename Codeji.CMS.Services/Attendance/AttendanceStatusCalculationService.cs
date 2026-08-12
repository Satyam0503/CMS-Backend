using Codeji.CMS.Repository.Entities.Attendance;

namespace Codeji.CMS.Services.Attendance;

/// <summary>
/// The single timing classifier for editable office attendance.  It deliberately
/// consumes the effective schedule; callers must not reproduce these rules in UI,
/// imports, or workflow services.
/// </summary>
public interface IAttendanceStatusCalculationService
{
    string CalculateOfficeStatus(CompanyOfficeSchedule schedule, TimeOnly checkIn, TimeOnly checkOut, decimal totalHours);
}

public sealed class AttendanceStatusCalculationService : IAttendanceStatusCalculationService
{
    public string CalculateOfficeStatus(CompanyOfficeSchedule schedule, TimeOnly checkIn, TimeOnly checkOut, decimal totalHours)
    {
        var grace = TimeSpan.FromMinutes(Math.Max(schedule.GraceMinutes, 0));
        var isLate = checkIn.ToTimeSpan() > schedule.StartTime.Add(grace);
        var isEarly = checkOut.ToTimeSpan() < schedule.EndTime.Subtract(grace);

        // These schedule-owned limits are the escalation boundaries.  When they
        // are not configured, the system preserves the configured ED/LHD result
        // instead of inventing a universal one-hour Half Day rule.
        var exceedsLateLimit = schedule.CheckInAllowedUntil.HasValue && checkIn.ToTimeSpan() > schedule.CheckInAllowedUntil.Value;
        var exceedsEarlyLimit = schedule.CheckOutAllowedFrom.HasValue && checkOut.ToTimeSpan() < schedule.CheckOutAllowedFrom.Value;
        if (exceedsLateLimit || exceedsEarlyLimit)
            return "HD";

        return isLate && isEarly ? "LHD+ED"
            : isLate ? "LHD"
            : isEarly ? "ED"
            : "P";
    }
}
