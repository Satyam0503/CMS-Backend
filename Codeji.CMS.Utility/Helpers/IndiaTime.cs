namespace Codeji.CMS.Utility.Helpers;

/// <summary>
/// India business clock. Persist instants in UTC; use this only where a
/// calendar date or scheduled business time must follow IST.
/// </summary>
public static class IndiaTime
{
    private static readonly TimeZoneInfo Zone = Resolve();

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
    public static DateTime Today => Now.Date;
    public static TimeZoneInfo TimeZone => Zone;

    public static DateTime ToIndiaTime(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
    }

    /// <summary>
    /// Resolves a persisted date-only value to its intended India business date.
    /// MongoDB returns dates as UTC instants, so an older value saved from an
    /// IST midnight may otherwise appear to be the previous UTC calendar day.
    /// </summary>
    public static DateOnly ToBusinessDate(DateTime value) =>
        DateOnly.FromDateTime(ToIndiaTime(value));

    public static DateTime ToStoredBusinessDate(DateOnly value) =>
        DateTime.SpecifyKind(value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    public static DateTime ToUtc(DateTime indiaLocalTime) => TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(indiaLocalTime, DateTimeKind.Unspecified), Zone);

    private static TimeZoneInfo Resolve()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }
}

