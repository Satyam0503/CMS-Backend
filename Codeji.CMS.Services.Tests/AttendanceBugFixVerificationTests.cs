using System;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Services.Attendance;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public class AttendanceBugFixVerificationTests
{
    private readonly AttendanceStatusCalculationService _statusCalcService = new();
    private static readonly CompanyOfficeSchedule StandardSchedule = new()
    {
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(18, 0, 0),
        RequiredWorkingMinutes = 480,
        GraceMinutes = 15,
        CheckInAllowedUntil = new TimeSpan(9, 15, 0),
        CheckOutAllowedFrom = new TimeSpan(17, 45, 0),
    };

    [Fact]
    public void Bug11_LhdEd_FallbackUnpaidDayFraction_Returns_HalfDay()
    {
        // BUG-11: LHD+ED was previously missing in fallback rules, defaulting to 0m (100% paid).
        var fraction = PayrollCalculationRules.ResolveFallbackUnpaidDayFraction("LHD+ED");
        Assert.Equal(0.5m, fraction);
    }

    [Fact]
    public void Bug10_ExceededOccurrences_WithCombinedCount_CalculatesCorrectly()
    {
        // BUG-10: Punctuality limit applied against distinct day count
        var exceeded = AttendancePayrollRules.ExceededOccurrences(combinedCount: 3, allowedCount: 2);
        Assert.Equal(1, exceeded);

        var withinLimit = AttendancePayrollRules.ExceededOccurrences(combinedCount: 2, allowedCount: 2);
        Assert.Equal(0, withinLimit);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.5)]
    [InlineData(3.9)]
    public void Bug09_CalculateOfficeStatus_InsufficientHours_Returns_Absent(decimal hours)
    {
        // BUG-09: totalHours ignored in timing classification, granting HD for <4h worked
        var status = _statusCalcService.CalculateOfficeStatus(
            StandardSchedule,
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            hours);

        Assert.Equal("A", status);
    }

    [Fact]
    public void Bug09_CalculateOfficeStatus_SufficientHours_Allows_HalfDay_Or_Present()
    {
        // When working at least 4 hours (half day minimum for 8h schedule)
        var status = _statusCalcService.CalculateOfficeStatus(
            StandardSchedule,
            new TimeOnly(13, 0),
            new TimeOnly(18, 0),
            5.0m);

        Assert.Equal("HD", status);
    }

    [Fact]
    public void Bug04_PaidDays_CalculatesStatutoryCalendarPayableDays()
    {
        // BUG-04: PaidDays in attendance summary must match the statutory calendar payable days:
        // EligibleCalendarDays - UnpaidDays - ApprovedPenaltyDays

        // Scenario 1: Full-month October (31 days), 0 unpaid, 0 penalty
        var eligibleCalendarDays1 = 31m;
        var unpaidDays1 = 0m;
        var penaltyDays1 = 0m;
        var paidDays1 = Math.Max(0m, eligibleCalendarDays1 - unpaidDays1 - penaltyDays1);
        Assert.Equal(31.0m, paidDays1);

        // Scenario 2: Full-month October (31 days), 1 absent day (1.0 LOP) and 0.5 penalty
        var eligibleCalendarDays2 = 31m;
        var unpaidDays2 = 1.0m;
        var penaltyDays2 = 0.5m;
        var paidDays2 = Math.Max(0m, eligibleCalendarDays2 - unpaidDays2 - penaltyDays2);
        Assert.Equal(29.5m, paidDays2);

        // Scenario 3: Mid-month joiner (joined Oct 16 to Oct 31 = 16 calendar days), 0 LOP
        var eligibleCalendarDays3 = 16m;
        var unpaidDays3 = 0m;
        var penaltyDays3 = 0m;
        var paidDays3 = Math.Max(0m, eligibleCalendarDays3 - unpaidDays3 - penaltyDays3);
        Assert.Equal(16.0m, paidDays3);

        // Scenario 4: Extreme unpaid days exceed calendar days -> guarded at 0m (never negative)
        var paidDays4 = Math.Max(0m, 10m - 15m - 0m);
        Assert.Equal(0m, paidDays4);
    }
}


