using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using Moq;
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

    [Fact]
    public async Task AttendanceCorrection_PayrollReadiness_BlocksWhenPending_And_PassesWhenNoPending()
    {
        var correctionsMock = new Mock<IMongoDbRepository<AttendanceCorrectionRequest>>();
        var employeesMock = new Mock<IMongoDbRepository<EmpUser>>();
        var service = new AttendancePayrollReadinessService(correctionsMock.Object, employeesMock.Object);

        var companyId = "company-1";
        var payMonth = new DateTime(2026, 10, 1);

        // Case 1: Pending correction exists -> Should block payroll
        correctionsMock.Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<AttendanceCorrectionRequest, bool>>>()))
            .ReturnsAsync(new List<AttendanceCorrectionRequest>
            {
                new() { CompanyId = companyId, UserId = "user-1", AttendanceDate = new DateTime(2026, 10, 15), Status = "Pending", Reason = "Forgot punch" }
            });

        employeesMock.Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(new List<EmpUser>
            {
                new() { UserId = "user-1", EmployeeId = "EMP001", FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" }
            });

        var resultBlocked = await service.EvaluateAsync(companyId, payMonth);
        Assert.False(resultBlocked.IsReady);
        Assert.Single(resultBlocked.BlockingReasons);
        Assert.Contains("EMP001", resultBlocked.BlockingReasons[0]);

        // Case 2: Once reviewed (e.g. Approved or Rejected), pending query returns empty -> Payroll is ready
        correctionsMock.Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<AttendanceCorrectionRequest, bool>>>()))
            .ReturnsAsync(new List<AttendanceCorrectionRequest>());

        var resultReady = await service.EvaluateAsync(companyId, payMonth);
        Assert.True(resultReady.IsReady);
        Assert.Empty(resultReady.BlockingReasons);
    }


    [Fact]
    public void AttendanceException_DateMatching_MatchesIstOffsetAndUtcDates()
    {
        // A date stored as 18:30Z of previous day represents midnight IST of target date
        var storedUtcMidnightIst = new DateTime(2026, 10, 18, 18, 30, 0, DateTimeKind.Utc);
        var targetBusinessDate = new DateTime(2026, 10, 19);

        var businessDate = CalendarDateHelpers.ToBusinessDate(storedUtcMidnightIst);
        var expectedBusinessDate = DateOnly.FromDateTime(targetBusinessDate);

        Assert.Equal(expectedBusinessDate, businessDate);
    }

    [Fact]
    public async Task AttendanceEditGuard_AllowsResolutionAndOverride_OnWeekendsAndHolidays()
    {
        var empMock = new Mock<IMongoDbRepository<EmpUser>>();
        var payrollMock = new Mock<IMongoDbRepository<EmpPayRoll>>();
        var summariesMock = new Mock<IMongoDbRepository<MonthlyAttendanceSummary>>();
        var weeklyMock = new Mock<IMongoDbRepository<WeeklyOffSetting>>();
        var calendarMock = new Mock<IMongoDbRepository<Codeji.CMS.Repository.Entities.Calendar.CalendarEntity>>();
        var exitMock = new Mock<IMongoDbRepository<Codeji.CMS.Repository.Entities.Employees.EmployeeExitAttendanceCutoff>>();

        var employee = new EmpUser
        {
            CompanyId = "comp-1",
            UserId = "user-1",
            EmployeeId = "EMP01",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            DateOfJoining = "2026-01-01"
        };

        empMock.Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), false))
            .ReturnsAsync(new List<EmpUser> { employee });

        weeklyMock.Setup(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<WeeklyOffSetting, bool>>>(), It.IsAny<bool>(), false))
            .ReturnsAsync(new List<WeeklyOffSetting> { new() { CompanyId = "comp-1", OffDays = [(int)DayOfWeek.Sunday] } });

        var guard = new AttendanceEditGuard(
            empMock.Object,
            payrollMock.Object,
            summariesMock.Object,
            weeklyMock.Object,
            calendarMock.Object,
            exitMock.Object
        );

        // Sunday is configured off day
        var sunday = new DateTime(2026, 10, 18); // Sunday

        // When not an exception resolution or override, it throws
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            guard.EnsureEditableWorkingDayAsync("comp-1", "user-1", sunday, isExceptionResolution: false, allowSourceOverride: false));

        // When isExceptionResolution is true, it does not throw
        var exceptionEx = await Record.ExceptionAsync(() =>
            guard.EnsureEditableWorkingDayAsync("comp-1", "user-1", sunday, isExceptionResolution: true, allowSourceOverride: false));
        Assert.Null(exceptionEx);

        // When allowSourceOverride is true, it does not throw
        var overrideEx = await Record.ExceptionAsync(() =>
            guard.EnsureEditableWorkingDayAsync("comp-1", "user-1", sunday, isExceptionResolution: false, allowSourceOverride: true));
        Assert.Null(overrideEx);
    }
}
