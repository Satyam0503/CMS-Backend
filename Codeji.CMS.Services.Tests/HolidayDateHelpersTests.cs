using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Services.Attendance;

public class HolidayDateHelpersTests
{
    [Fact]
    public void NonRecurringHoliday_MatchesExactDateOnly()
    {
        var holiday = new CalendarEntity { Date = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Unspecified), Recurring = false };
        var target = new DateOnly(2026, 8, 28);
        Assert.True(CalendarDateHelpers.MatchesDate(holiday, target));
        Assert.False(CalendarDateHelpers.MatchesDate(holiday, target.AddDays(-1)));
    }

    [Fact]
    public void RecurringHoliday_MatchesAnyYearSameMonthDay()
    {
        var holiday = new CalendarEntity { Date = new DateTime(2000, 9, 4, 0, 0, 0, DateTimeKind.Unspecified), Recurring = true };
        var target2026 = new DateOnly(2026, 9, 4);
        var target2025 = new DateOnly(2025, 9, 4);
        Assert.True(CalendarDateHelpers.MatchesDate(holiday, target2026));
        Assert.True(CalendarDateHelpers.MatchesDate(holiday, target2025));
        Assert.False(CalendarDateHelpers.MatchesDate(holiday, target2026.AddDays(-1)));
    }

    [Fact]
    public void MonthBoundaryAndYearBoundary_AreHandledCorrectly()
    {
        var newYearHoliday = new CalendarEntity { Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), Recurring = false };
        Assert.True(CalendarDateHelpers.MatchesDate(newYearHoliday, new DateOnly(2026, 1, 1)));
        Assert.False(CalendarDateHelpers.MatchesDate(newYearHoliday, new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public void ISTMidnight_DoesNotMoveHolidayToPreviousDay()
    {
        // Simulate stored date as local-midnight Unspecified and ensure match
        var holiday = new CalendarEntity { Date = DateTime.SpecifyKind(new DateTime(2026, 8, 28, 0, 0, 0), DateTimeKind.Unspecified), Recurring = false };
        var target = new DateOnly(2026, 8, 28);
        Assert.True(CalendarDateHelpers.MatchesDate(holiday, target));
    }

    [Fact]
    public void RakshaBandhan_DateIsPreservedAsExactBusinessDate()
    {
        var holiday = new CalendarEntity { Date = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Unspecified), Recurring = false };
        Assert.True(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2026, 8, 28)));
        Assert.False(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2026, 8, 27)));
    }

    // A legacy row stored by converting IST midnight to UTC lands on the previous
    // calendar day (e.g. 28 Aug 2026 00:00 IST becomes 27 Aug 2026 18:30 UTC). The
    // helper must still resolve it to the originally intended business date.
    [Fact]
    public void RakshaBandhan_LegacyUtcShiftedRow_StillResolvesToTwentyEighthAugust()
    {
        var holiday = new CalendarEntity { Date = new DateTime(2026, 8, 27, 18, 30, 0, DateTimeKind.Utc), Recurring = false };
        Assert.True(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2026, 8, 28)));
        Assert.False(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2026, 8, 27)));
    }

    [Fact]
    public void Janmashtami_ResolvesToFourthSeptember_NotThird()
    {
        var holiday = new CalendarEntity { Date = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Unspecified), Recurring = false };
        Assert.True(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2026, 9, 4)));
        Assert.False(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2026, 9, 3)));
    }

    [Fact]
    public void Janmashtami_LegacyUtcShiftedRow_StillResolvesToFourthSeptember()
    {
        var holiday = new CalendarEntity { Date = new DateTime(2026, 9, 3, 18, 30, 0, DateTimeKind.Utc), Recurring = false };
        Assert.True(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2026, 9, 4)));
        Assert.False(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2026, 9, 3)));
    }

    [Fact]
    public void RecurringHoliday_LegacyUtcShiftedRow_StillMatchesCorrectMonthDay()
    {
        var holiday = new CalendarEntity { Date = new DateTime(2000, 9, 3, 18, 30, 0, DateTimeKind.Utc), Recurring = true };
        Assert.True(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2026, 9, 4)));
        Assert.False(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2026, 9, 3)));
    }

    [Fact]
    public void YearBoundary_LegacyUtcShiftedRow_DoesNotStayOnPreviousYear()
    {
        // 1 Jan 2026 00:00 IST stored as the equivalent UTC instant lands on 31 Dec 2025.
        var holiday = new CalendarEntity { Date = new DateTime(2025, 12, 31, 18, 30, 0, DateTimeKind.Utc), Recurring = false };
        Assert.True(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2026, 1, 1)));
        Assert.False(CalendarDateHelpers.MatchesDate(holiday, new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public void ToBusinessDate_CleanMidnightValue_IsUnchanged()
    {
        var clean = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Unspecified);
        Assert.Equal(new DateOnly(2026, 8, 28), CalendarDateHelpers.ToBusinessDate(clean));
    }

    [Fact]
    public void ToBusinessDate_LegacyShiftedValue_RecoversIntendedDay()
    {
        var shifted = new DateTime(2026, 8, 27, 18, 30, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 8, 28), CalendarDateHelpers.ToBusinessDate(shifted));
    }
}
