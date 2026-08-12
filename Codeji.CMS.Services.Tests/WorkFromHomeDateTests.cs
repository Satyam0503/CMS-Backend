using Codeji.CMS.Services.Attendance;

namespace Codeji.CMS.Services.Tests;

public class WorkFromHomeDateTests
{
    [Theory]
    [InlineData("FullDay", 1)]
    [InlineData("FirstHalf", .5)]
    [InlineData("SecondHalf", .5)]
    public void WeeklyUsage_UsesFractionalUnitsForHalfDayWfh(string durationType, decimal expected)
    {
        var week = new DateTime(2026, 8, 10);

        var usage = WorkFromHomeService.WeeklyUsageForPeriod(week.AddDays(2), week.AddDays(2), durationType, week);

        Assert.Equal(expected, usage);
    }

    [Fact]
    public void WeeklyUsage_OnlyCountsDaysInsideTheCurrentWeek()
    {
        var week = new DateTime(2026, 8, 10);

        var usage = WorkFromHomeService.WeeklyUsageForPeriod(week.AddDays(-1), week.AddDays(1), "FullDay", week);

        Assert.Equal(2m, usage);
    }

    [Fact]
    public void BusinessDateUtc_PreservesTheSelectedCalendarDay()
    {
        var date = WorkFromHomeService.BusinessDateUtc(new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Utc), date);
    }
}
