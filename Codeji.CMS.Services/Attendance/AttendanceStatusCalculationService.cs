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
        var lateArrivalStart = schedule.CheckInAllowedUntil ?? schedule.StartTime;
        var earlyDepartureStart = schedule.CheckOutAllowedFrom ?? schedule.EndTime;
        var isLate = checkIn.ToTimeSpan() > lateArrivalStart;
        var isEarly = checkOut.ToTimeSpan() < earlyDepartureStart;

        // These schedule-owned limits are the escalation boundaries.  When they
        // are not configured, the system preserves the configured ED/LHD result
        // instead of inventing a universal one-hour Half Day rule.
        var halfDayCheckInBoundary = lateArrivalStart.Add(grace);
        var halfDayCheckOutBoundary = earlyDepartureStart.Subtract(grace);
        var exceedsLateLimit = checkIn.ToTimeSpan() > halfDayCheckInBoundary;
        var exceedsEarlyLimit = checkOut.ToTimeSpan() < halfDayCheckOutBoundary;
        if (exceedsLateLimit || exceedsEarlyLimit)
            return "HD";

        return isLate && isEarly ? "LHD+ED"
            : isLate ? "LHD"
            : isEarly ? "ED"
            : "P";
    }
}
