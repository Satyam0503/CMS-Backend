using Codeji.CMS.Repository.Entities.Calendar;

namespace Codeji.CMS.Services.Attendance;

public static class CalendarDateHelpers
{
    private static readonly TimeZoneInfo IndiaZone = ResolveIndiaZone();

    /// <summary>
    /// Returns the completed business-date cutoff for attendance validation.
    /// Today's attendance may still be in progress, so exceptions can only be
    /// raised through the preceding India business day.
    /// </summary>
    public static DateOnly GetLastCompletedBusinessDate(DateTime utcNow)
    {
        var utc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, IndiaZone)).AddDays(-1);
    }

    // Recovers the calendar business date a stored value was meant to represent.
    // A correctly stored value is an exact IST midnight and this is a no-op. A
    // legacy row that was saved by converting IST midnight to UTC (shifting it
    // to the previous day) is recovered here by converting back to IST, so both
    // old and new data resolve to the same business date without a data migration.
    public static DateOnly ToBusinessDate(DateTime raw)
    {
        var utc = DateTime.SpecifyKind(raw, DateTimeKind.Utc);
        var ist = TimeZoneInfo.ConvertTimeFromUtc(utc, IndiaZone);
        return DateOnly.FromDateTime(ist);
    }

    public static bool MatchesDate(CalendarEntity? holiday, DateOnly target)
    {
        if (holiday is null)
        {
            return false;
        }

        var businessDate = ToBusinessDate(holiday.Date);
        if (holiday.Recurring)
        {
            return businessDate.Month == target.Month && businessDate.Day == target.Day;
        }

        return businessDate == target;
    }

    public static bool MatchesDate(CalendarEntity? holiday, DateTime target)
        => MatchesDate(holiday, DateOnly.FromDateTime(target.Date));

    private static TimeZoneInfo ResolveIndiaZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }
}

