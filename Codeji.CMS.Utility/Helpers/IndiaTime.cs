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
    public static DateTime ToUtc(DateTime indiaLocalTime) => TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(indiaLocalTime, DateTimeKind.Unspecified), Zone);

    private static TimeZoneInfo Resolve()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }
}
