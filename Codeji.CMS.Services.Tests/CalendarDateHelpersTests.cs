using Codeji.CMS.Services.Attendance;

public class CalendarDateHelpersTests
{
    [Theory]
    [InlineData(2026, 8, 17, 17, 0, 2026, 8, 16)] // 22:30 in India
    [InlineData(2026, 8, 17, 19, 0, 2026, 8, 17)] // 00:30 next day in India
    public void Last_completed_business_date_excludes_the_current_India_day(
        int year, int month, int day, int hour, int minute,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        var utcNow = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);

        var result = CalendarDateHelpers.GetLastCompletedBusinessDate(utcNow);

        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), result);
    }
}
