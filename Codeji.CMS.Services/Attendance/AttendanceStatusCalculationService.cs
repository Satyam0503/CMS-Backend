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
        var minHalfDayHours = schedule.RequiredWorkingMinutes > 0 ? (schedule.RequiredWorkingMinutes / 120m) : 4.0m;
        if (totalHours <= 0 || totalHours < minHalfDayHours)
            return "A";

        var grace = TimeSpan.FromMinutes(Math.Max(schedule.GraceMinutes, 0));
        var lateArrivalStart = schedule.LateArrivalAfter ?? schedule.StartTime.Add(grace);
        var earlyDepartureStart = schedule.EarlyDepartureBefore ?? schedule.EndTime.Subtract(grace);
        var isLate = checkIn.ToTimeSpan() > lateArrivalStart;
        var isEarly = checkOut.ToTimeSpan() < earlyDepartureStart;

        // 1-hour allowed window boundaries:
        // Exceeding 1 hour late escalates to Half Day (HD)
        var halfDayCheckInBoundary = schedule.HalfDayCheckInAfter
            ?? (schedule.CheckInAllowedUntil.HasValue && schedule.CheckInAllowedUntil.Value > lateArrivalStart
                ? schedule.CheckInAllowedUntil.Value
                : schedule.StartTime.Add(TimeSpan.FromHours(1)));

        // Exceeding 1 hour early departure escalates to Half Day (HD)
        var halfDayCheckOutBoundary = schedule.HalfDayCheckOutBefore
            ?? (schedule.CheckOutAllowedFrom.HasValue && schedule.CheckOutAllowedFrom.Value < earlyDepartureStart
                ? schedule.CheckOutAllowedFrom.Value
                : schedule.EndTime.Subtract(TimeSpan.FromHours(1)));

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
