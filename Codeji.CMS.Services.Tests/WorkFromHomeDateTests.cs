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

    [Theory]
    [InlineData("FullDay", 1)]
    [InlineData("FirstHalf", .5)]
    [InlineData("SecondHalf", .5)]
    public void WeeklyUsage_ClipsAtMondayBoundary(string durationType, decimal expected)
    {
        // Week periods are Monday through Sunday. This request crosses Sunday/Monday.
        var monday = new DateTime(2026, 8, 10);

        var usage = WorkFromHomeService.WeeklyUsageForPeriod(
            monday.AddDays(-1), monday, durationType, monday);

        Assert.Equal(expected, usage);
    }

    [Theory]
    [InlineData("FullDay", 1)]
    [InlineData("FirstHalf", .5)]
    [InlineData("SecondHalf", .5)]
    public void MonthlyUsage_ClipsAtMonthEndAndCountsHalfDaysFractionally(string durationType, decimal expected)
    {
        var month = new DateTime(2026, 8, 1);

        var usage = WorkFromHomeService.MonthlyUsageForPeriod(
            new DateTime(2026, 7, 31), new DateTime(2026, 8, 1), durationType, month);

        Assert.Equal(expected, usage);
    }

    [Theory]
    [InlineData("FullDay", 1)]
    [InlineData("FirstHalf", .5)]
    [InlineData("SecondHalf", .5)]
    public void MonthlyUsage_IncludesLastCalendarDayOfMonth(string durationType, decimal expected)
    {
        var month = new DateTime(2026, 8, 1);

        var usage = WorkFromHomeService.MonthlyUsageForPeriod(
            new DateTime(2026, 8, 31), new DateTime(2026, 9, 1), durationType, month);

        Assert.Equal(expected, usage);
    }

    [Fact]
    public void BusinessDateUtc_PreservesTheSelectedCalendarDay()
    {
        var date = WorkFromHomeService.BusinessDateUtc(new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Utc), date);
    }

    [Theory]
    [InlineData("SecondHalf", 12, 59, false)]
    [InlineData("SecondHalf", 13, 0, true)]
    [InlineData("FirstHalf", 12, 59, true)]
    [InlineData("FirstHalf", 13, 0, false)]
    public void HalfDayClockIn_IsLimitedToTheRequestedAttendanceSegment(string durationType, int hour, int minute, bool expected)
    {
        var allowed = WorkFromHomeService.IsHalfDayClockInWithinRequestedSegment(
            durationType, new TimeSpan(hour, minute, 0), new TimeSpan(9, 0, 0), 480, 8m);

        Assert.Equal(expected, allowed);
    }

    [Fact]
    public void SameDayCutoff_WithoutCustomCutoff_FallsBackToOfficeStart_AndBlocksAfterShiftStart()
    {
        var date = new DateTime(2026, 10, 23);
        var companyNow = new DateTime(2026, 10, 23, 10, 25, 0);
        var officeStart = new TimeSpan(9, 30, 0);

        var isBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            date, companyNow, "FullDay", officeStart, null, 0, 480, 8m);

        Assert.True(isBlocked);
    }

    [Fact]
    public void SameDayCutoff_WithoutCustomCutoff_BeforeShiftStart_IsNotBlocked()
    {
        var date = new DateTime(2026, 10, 23);
        var companyNow = new DateTime(2026, 10, 23, 9, 15, 0);
        var officeStart = new TimeSpan(9, 30, 0);

        var isBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            date, companyNow, "FullDay", officeStart, null, 0, 480, 8m);

        Assert.False(isBlocked);
    }

    [Theory]
    [InlineData(9, 29, false)]
    [InlineData(9, 31, false)] // within 60 min grace
    [InlineData(10, 25, false)] // user scenario at 10:25 with grace
    [InlineData(10, 31, true)] // after 10:30 cutoff
    public void SameDayCutoff_WithGracePeriod_ExtendsCutoff(int hour, int minute, bool expectedBlocked)
    {
        var date = new DateTime(2026, 10, 23);
        var companyNow = new DateTime(2026, 10, 23, hour, minute, 0);
        var officeStart = new TimeSpan(9, 30, 0);
        var customCutoff = new TimeSpan(9, 30, 0);

        var isBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            date, companyNow, "FullDay", officeStart, customCutoff, 60, 480, 8m);

        Assert.Equal(expectedBlocked, isBlocked);
    }

    [Theory]
    [InlineData(10, 25, false)]
    [InlineData(10, 59, false)]
    [InlineData(11, 1, true)]
    public void SameDayCutoff_WithCustomCutoffTime_OverridesOfficeStart(int hour, int minute, bool expectedBlocked)
    {
        var date = new DateTime(2026, 10, 23);
        var companyNow = new DateTime(2026, 10, 23, hour, minute, 0);
        var officeStart = new TimeSpan(9, 30, 0);
        var customCutoff = new TimeSpan(11, 0, 0);

        var isBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            date, companyNow, "FullDay", officeStart, customCutoff, 0, 480, 8m);

        Assert.Equal(expectedBlocked, isBlocked);
    }

    [Theory]
    [InlineData(10, 25, false)]
    [InlineData(13, 29, false)]
    [InlineData(13, 31, true)]
    public void SameDayCutoff_SecondHalf_UsesHalfDayBoundaryNotMorningStart(int hour, int minute, bool expectedBlocked)
    {
        var date = new DateTime(2026, 10, 23);
        var companyNow = new DateTime(2026, 10, 23, hour, minute, 0);
        var officeStart = new TimeSpan(9, 30, 0);
        var customCutoff = new TimeSpan(9, 30, 0);

        var isBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            date, companyNow, "SecondHalf", officeStart, customCutoff, 0, 480, 8m);

        Assert.Equal(expectedBlocked, isBlocked);
    }

    [Fact]
    public void SameDayCutoff_FutureDate_IsNotCutoffReached()
    {
        var tomorrow = new DateTime(2026, 10, 24);
        var companyNow = new DateTime(2026, 10, 23, 17, 0, 0);
        var officeStart = new TimeSpan(9, 30, 0);

        var isBlocked = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            tomorrow, companyNow, "FullDay", officeStart, null, 0, 480, 8m);

        Assert.False(isBlocked);
    }
}
