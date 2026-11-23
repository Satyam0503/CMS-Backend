using System.Linq.Expressions;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Dashboard;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public class DashboardOverviewServiceTests
{
    private static readonly DateTime Today = new(2026, 11, 16);

    private static EmpUser Employee(string userId, string joined, string companyId = "tenant-a") => new()
    {
        CompanyId = companyId,
        UserId = userId,
        EmployeeId = userId.ToUpperInvariant(),
        FirstName = userId,
        LastName = "Test",
        Email = $"{userId}@example.com",
        DateOfJoining = joined,
        Status = true,
    };

    private static EmpUser Former(string userId, string joined, DateTime lastWorkingDate)
    {
        var employee = Employee(userId, joined);
        employee.Status = false;
        employee.LastWorkingDate = lastWorkingDate;
        employee.Lifecycle.EmploymentStatus = EmployeeLifecycleStatuses.Resigned;
        return employee;
    }

    [Fact]
    public void HeadcountTrend_CountsEmploymentAtEachMonthEndAndTodayForCurrentMonth()
    {
        var employees = new[]
        {
            Employee("a", "2026-06-15"),
            Former("b", "2025-01-01", new DateTime(2026, 8, 20)),
            Former("e", "2026-01-01", new DateTime(2026, 11, 30)), // closed profile with a future last day is historical, not current
            Employee("c", "2026-11-10"),
            Employee("d", "2026-11-20"), // joins after "today": not yet counted
        };

        var trend = DashboardOverviewService.BuildHeadcountTrend(employees, Today);

        Assert.Equal(
            ["2025-12", "2026-01", "2026-02", "2026-03", "2026-04", "2026-05", "2026-06", "2026-07", "2026-08", "2026-09", "2026-10", "2026-11"],
            trend.Select(x => x.Month));
        Assert.Equal([1, 2, 2, 2, 2, 2, 3, 3, 2, 2, 2, 2], trend.Select(x => x.Count));
    }

    [Fact]
    public void WorkforceMovementTrend_CountsOriginalHiresRehiresAndCompletedExitOnEffectiveDate()
    {
        var active = Employee("rehired", "2026-01-15");
        active.Lifecycle.History.Add(new EmployeeLifecycleStatusHistory
        {
            Dimension = nameof(EmployeeLifecycleState.EmploymentStatus),
            ReasonCode = "REJOIN",
            EffectiveFromUtc = new DateTime(2026, 10, 1, 1, 0, 0, DateTimeKind.Utc),
        });
        var second = Employee("new", "2026-11-16");
        var futureJoiner = Employee("future-joiner", "2026-11-17");
        var missingJoinDate = Employee("missing-join-date", "");
        missingJoinDate.CreatedDate = Today;
        active.Lifecycle.History.Add(new EmployeeLifecycleStatusHistory
        {
            Dimension = nameof(EmployeeLifecycleState.EmploymentStatus),
            ReasonCode = "REJOIN",
            EffectiveFromUtc = new DateTime(2026, 11, 17, 1, 0, 0, DateTimeKind.Utc),
        });
        var exits = new[]
        {
            new EmployeeExit { CompanyId = "tenant-a", EmployeeUserId = "rehired", Status = "Rejoined", CompletedAtUtc = Today, LastWorkingDate = new DateTime(2026, 6, 30) },
            new EmployeeExit { CompanyId = "tenant-a", EmployeeUserId = "boundary", Status = "Resigned", CompletedAtUtc = Today, LastWorkingDate = new DateTime(2025, 12, 1) },
            new EmployeeExit { CompanyId = "tenant-a", EmployeeUserId = "too-old", Status = "Resigned", CompletedAtUtc = Today, LastWorkingDate = new DateTime(2025, 11, 30) },
            new EmployeeExit { CompanyId = "tenant-a", EmployeeUserId = "ignored-open", Status = "NoticePeriod", LastWorkingDate = new DateTime(2026, 7, 2) },
            new EmployeeExit { CompanyId = "tenant-a", EmployeeUserId = "uncompleted", Status = "Resigned", LastWorkingDate = new DateTime(2026, 7, 2) },
            new EmployeeExit { CompanyId = "tenant-a", EmployeeUserId = "deleted", Status = "Resigned", CompletedAtUtc = Today, LastWorkingDate = new DateTime(2026, 7, 2), IsDeleted = true },
            new EmployeeExit { CompanyId = "tenant-a", EmployeeUserId = "future", Status = "Resigned", CompletedAtUtc = Today, LastWorkingDate = new DateTime(2026, 11, 30) },
        };

        var trend = DashboardOverviewService.BuildWorkforceMovementTrend([active, second, futureJoiner, missingJoinDate], exits, Today);

        Assert.Equal(12, trend.Count);
        Assert.Equal("2025-12", trend[0].Month);
        Assert.Equal(1, trend[0].Exits); // exact inclusive start of the 12-month window
        Assert.Equal(1, trend.Single(x => x.Month == "2026-01").Hires);
        Assert.Equal(1, trend.Single(x => x.Month == "2026-10").Hires);
        Assert.Equal(1, trend.Single(x => x.Month == "2026-11").Hires); // same-day hire included; future/profile-created hires excluded
        Assert.Equal(1, trend.Single(x => x.Month == "2026-06").Exits);
        Assert.Equal(0, trend.Single(x => x.Month == "2026-07").Exits);
        Assert.Equal(0, trend.Single(x => x.Month == "2026-11").Exits);
        Assert.Equal(-1, trend.Single(x => x.Month == "2026-06").NetChange);
    }

    [Fact]
    public void LeaveRequestCohortTrend_UsesIndiaSubmissionMonthCurrentStatusAndTwelveMonthBoundary()
    {
        var requests = new[]
        {
            new LeaveRequest { CompanyId = "tenant-a", CreatedDate = new DateTime(2026, 10, 31, 20, 0, 0, DateTimeKind.Utc), Status = EnumsHelper.LeaveRequestStatus.Accepted, LeavePolicyId = "p", Reason = "x" }, // Nov 1 in India.
            new LeaveRequest { CompanyId = "tenant-a", CreatedDate = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc), Status = EnumsHelper.LeaveRequestStatus.Pending, LeavePolicyId = "p", Reason = "x" },
            new LeaveRequest { CompanyId = "tenant-a", CreatedDate = new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc), Status = EnumsHelper.LeaveRequestStatus.Rejected, LeavePolicyId = "p", Reason = "x" },
            new LeaveRequest { CompanyId = "tenant-a", CreatedDate = new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc), Status = EnumsHelper.LeaveRequestStatus.WithDrawn, LeavePolicyId = "p", Reason = "x" },
            new LeaveRequest { CompanyId = "tenant-a", CreatedDate = new DateTime(2025, 11, 30, 12, 0, 0, DateTimeKind.Utc), Status = EnumsHelper.LeaveRequestStatus.Accepted, LeavePolicyId = "p", Reason = "x" }, // Outside the 12-month window.
            new LeaveRequest { CompanyId = "tenant-a", CreatedDate = Today.AddDays(1), Status = EnumsHelper.LeaveRequestStatus.Accepted, LeavePolicyId = "p", Reason = "x" }, // Future cohort.
            new LeaveRequest { CompanyId = "tenant-a", CreatedDate = Today, Status = EnumsHelper.LeaveRequestStatus.Accepted, IsDeleted = true, LeavePolicyId = "p", Reason = "x" },
            new LeaveRequest { CompanyId = "tenant-a", CreatedDate = null, Status = EnumsHelper.LeaveRequestStatus.Accepted, LeavePolicyId = "p", Reason = "x" },
        };

        var trend = DashboardOverviewService.BuildLeaveRequestCohortTrend(requests, Today);

        Assert.Equal(12, trend.Count);
        Assert.Equal("2025-12", trend[0].Month);
        Assert.Equal(0, trend[0].TotalRequests);
        var november = Assert.Single(trend.Where(point => point.Month == "2026-11"));
        Assert.Equal(4, november.TotalRequests);
        Assert.Equal(1, november.Pending);
        Assert.Equal(1, november.Approved);
        Assert.Equal(1, november.Rejected);
        Assert.Equal(1, november.Withdrawn);
        Assert.Equal(0, november.OtherStatus);
        Assert.Equal(november.TotalRequests, november.Pending + november.Approved + november.Rejected + november.Withdrawn + november.OtherStatus);
    }

    [Fact]
    public void Absent_ExcludesPresentAndOnLeave_AndIsZeroOnNonWorkingDay()
    {
        var employees = new[] { Employee("u1", "2025-01-01"), Employee("u2", "2025-01-01"), Employee("u3", "2025-01-01"), Employee("u4", "2025-01-01") };
        var present = new HashSet<string> { "u1" };
        var onLeave = new HashSet<string> { "u2", "u1" }; // half-day leave + present must not double count

        Assert.Equal(2, DashboardOverviewService.CalculateAbsent(employees, present, onLeave, Today, isWorkingDay: true));
        Assert.Equal(0, DashboardOverviewService.CalculateAbsent(employees, present, onLeave, Today, isWorkingDay: false));
    }

    [Fact]
    public void AttendanceBreakdown_PartitionsExpectedEmployeesAndKeepsHalfDayOverlapVisible()
    {
        var employees = Enumerable.Range(1, 7).Select(index => Employee($"u{index}", "2025-01-01")).ToList();
        var attendance = new[]
        {
            new AttendanceModel { UserId = "u1", Status = "P" },
            new AttendanceModel { UserId = "u2", Status = "HD" }, // Leave-eligible status, not evidence of work.
            new AttendanceModel { UserId = "u3", Status = "A" },
            new AttendanceModel { UserId = "u4", Status = "HD" }, // No approved request: other status, not present.
            new AttendanceModel { UserId = "u5", Status = "CUSTOM" },
            // u6 has no attendance row and must remain unreported.
            new AttendanceModel { UserId = "u7", Status = "P" },
        };
        var leave = new HashSet<string> { "u1", "u2" }; // u1 is an overlap; u2 is leave-only.

        var result = DashboardOverviewService.BuildAttendanceBreakdown(employees, attendance, leave, Today, isWorkingDay: true);

        Assert.Equal(7, result.ExpectedEmployees);
        Assert.Equal(1, result.PresentOnly);
        Assert.Equal(1, result.LeaveOnly);
        Assert.Equal(1, result.PresentAndOnLeave);
        Assert.Equal(1, result.ExplicitlyAbsent);
        Assert.Equal(2, result.OtherStatus);
        Assert.Equal(1, result.Unreported);
        Assert.Equal(result.ExpectedEmployees,
            result.PresentOnly + result.LeaveOnly + result.PresentAndOnLeave + result.ExplicitlyAbsent + result.OtherStatus + result.Unreported);
    }

    [Fact]
    public void AttendanceBreakdown_ExcludesEmployeesNotExpectedAndNonWorkingDays()
    {
        var expected = Employee("expected", "2025-01-01");
        var suspended = Employee("suspended", "2025-01-01");
        suspended.Lifecycle.AvailabilityStatus = EmployeeLifecycleStatuses.Suspended;

        var workingDay = DashboardOverviewService.BuildAttendanceBreakdown(
            [expected, suspended], [], new HashSet<string>(), Today, isWorkingDay: true);

        Assert.Equal(1, workingDay.ExpectedEmployees);
        Assert.Equal(1, workingDay.Unreported);

        var nonWorkingDay = DashboardOverviewService.BuildAttendanceBreakdown(
            [expected, suspended], [], new HashSet<string>(), Today, isWorkingDay: false);

        Assert.Equal(0, nonWorkingDay.ExpectedEmployees);
        Assert.Equal(0, nonWorkingDay.Unreported);
    }

    [Fact]
    public void RecentActivities_IncludeThirtyCalendarDays_ExcludeFuture_SortAndCapAtEight()
    {
        var atWindowStart = Today.Date.AddDays(-29);
        var activities = new List<DashboardActivityDto>
        {
            new() { Type = "joined", EmployeeId = "boundary", OccurredAt = atWindowStart },
            new() { Type = "joined", EmployeeId = "too-old", OccurredAt = Today.Date.AddDays(-30).AddHours(23) },
            new() { Type = "joined", EmployeeId = "future", OccurredAt = Today.Date.AddDays(1) },
            new() { Type = "joined", EmployeeId = "today-late", OccurredAt = Today.Date.AddHours(23).AddMinutes(59) },
        };
        activities.AddRange(Enumerable.Range(1, 9).Select(hour => new DashboardActivityDto
        {
            Type = "leaveApproved",
            EmployeeId = $"hour-{hour}",
            OccurredAt = Today.Date.AddHours(hour),
        }));

        var selected = DashboardOverviewService.FilterRecentActivities(activities, Today);

        Assert.Equal(8, selected.Count);
        Assert.Equal("today-late", selected[0].EmployeeId);
        Assert.DoesNotContain(selected, x => x.EmployeeId is "too-old" or "future");
        Assert.Equal(selected.OrderByDescending(x => x.OccurredAt).Select(x => x.EmployeeId), selected.Select(x => x.EmployeeId));
        Assert.DoesNotContain(selected, x => x.EmployeeId == "boundary"); // older valid items are dropped after recency sorting
    }

    [Fact]
    public void RecentActivities_IncludeExactThirtyDayWindowBoundaryWhenUnderLimit()
    {
        var activities = new[]
        {
            new DashboardActivityDto { Type = "joined", EmployeeId = "boundary", OccurredAt = Today.Date.AddDays(-29) },
            new DashboardActivityDto { Type = "joined", EmployeeId = "before-boundary", OccurredAt = Today.Date.AddDays(-29).AddTicks(-1) },
        };

        var selected = DashboardOverviewService.FilterRecentActivities(activities, Today);

        Assert.Equal(["boundary"], selected.Select(x => x.EmployeeId));
    }

    [Fact]
    public void RecentActivities_OnlyIncludesDocumentedGeneralDashboardEventTypes()
    {
        var activities = new[]
        {
            new DashboardActivityDto { Type = "SalaryRevised", EmployeeId = "employee-1", EmployeeName = "Employee One", OccurredAt = Today },
            new DashboardActivityDto { Type = "payrollExported", EmployeeId = "employee-2", EmployeeName = "Employee Two", OccurredAt = Today },
            new DashboardActivityDto { Type = "leaveApproved", EmployeeId = "employee-3", EmployeeName = "Employee Three", OccurredAt = Today },
        };

        var selected = DashboardOverviewService.FilterRecentActivities(activities, Today);

        var included = Assert.Single(selected);
        Assert.Equal("leaveApproved", included.Type);
    }

    [Fact]
    public async Task Overview_OnlyCountsCallerTenant()
    {
        var employees = new List<EmpUser>
        {
            Employee("a1", "2025-01-01"), Employee("a2", "2025-01-01"), Employee("a3", "2025-01-01"),
            Employee("future-join", "2026-11-20"),
            Employee("b1", "2025-01-01", "tenant-b"), Employee("b2", "2025-01-01", "tenant-b"),
        };
        var attendance = new List<AttendanceModel>
        {
            new() { CompanyId = "tenant-a", UserId = "a1", Date = Today, CheckInTime = Today.AddHours(9) },
            new() { CompanyId = "tenant-a", UserId = "a3", Date = Today, Status = "A" },
            new() { CompanyId = "tenant-b", UserId = "a2", Date = Today, Status = "P" }, // same user id, other tenant
            new() { CompanyId = "tenant-b", UserId = "b1", Date = Today, Status = "P" },
        };
        var leaves = new List<LeaveRequest>
        {
            new() { CompanyId = "tenant-a", EmployeeId = "a2", LeavePolicyId = "p", Reason = "x", Status = EnumsHelper.LeaveRequestStatus.Accepted, StartDate = Today, EndDate = Today, CreatedDate = Today },
            new() { CompanyId = "tenant-b", EmployeeId = "b2", LeavePolicyId = "p", Reason = "x", Status = EnumsHelper.LeaveRequestStatus.Accepted, StartDate = Today, EndDate = Today, CreatedDate = Today },
        };
        var vacancies = new List<JobVacancy>
        {
            new() { CompanyId = "tenant-a", Status = true, Title = "QA" },
            new() { CompanyId = "tenant-a", Status = false, Title = "Closed" },
            new() { CompanyId = "tenant-a", Status = true, IsDeleted = true, Title = "Removed" },
            new() { CompanyId = "tenant-b", Status = true, Title = "Other" },
        };
        var exitRows = new List<EmployeeExit>
        {
            new() { CompanyId = "tenant-a", EmployeeUserId = "former-a", Status = "Resigned", CompletedAtUtc = Today, LastWorkingDate = new DateTime(2026, 10, 20) },
            new() { CompanyId = "tenant-b", EmployeeUserId = "former-b", Status = "Resigned", CompletedAtUtc = Today, LastWorkingDate = new DateTime(2026, 10, 20) },
        };

        var calendar = new Mock<ICompanyWorkingCalendarService>();
        calendar.Setup(x => x.IsWorkingDayAsync("tenant-a", DateOnly.FromDateTime(Today), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var roles = new Mock<IRoleService>();
        roles.Setup(x => x.GetModuleAvailabilityForCompany("tenant-a", It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [AppModule.Employees] = true,
                [AppModule.Attendance] = true,
                [AppModule.LeaveManagement] = true,
                [AppModule.Jobs] = true,
                [AppModule.Offboarding] = true,
            });

        var service = new DashboardOverviewService(
            Repo(employees).Object,
            Repo(attendance).Object,
            Repo(leaves).Object,
            new Mock<IMongoDbRepository<LeavePolicy>>().Object,
            Repo(vacancies).Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            Repo(exitRows).Object,
            calendar.Object,
            roles.Object);

        var result = await service.GetOverviewAsync("tenant-a", Today);

        Assert.Equal("dashboard-overview.v4", result.DefinitionVersion);
        Assert.Equal(DateOnly.FromDateTime(Today), result.AsOfDate);
        Assert.Equal("Asia/Kolkata", result.TimeZone);
        Assert.Equal(DateTimeKind.Utc, result.GeneratedAtUtc.Kind);
        Assert.True(result.SourceAvailability!.Employees);
        Assert.True(result.SourceAvailability.Attendance);
        Assert.True(result.SourceAvailability.LeaveManagement);
        Assert.Equal(3, result.TotalEmployees);
        Assert.Equal(12, result.HeadcountTrend.Count);
        Assert.Equal(result.TotalEmployees, result.HeadcountTrend[^1].Count);
        Assert.Equal(1, result.PresentToday);
        Assert.Equal(1, result.OnLeaveToday);
        Assert.Equal(1, result.AbsentToday);
        Assert.Equal(3, result.AttendanceBreakdown.ExpectedEmployees);
        Assert.Equal(1, result.AttendanceBreakdown.PresentOnly);
        Assert.Equal(1, result.AttendanceBreakdown.LeaveOnly);
        Assert.Equal(1, result.AttendanceBreakdown.ExplicitlyAbsent);
        Assert.Equal(0, result.AttendanceBreakdown.Unreported);
        Assert.True(result.IsWorkingDay);
        Assert.Equal(1, result.OpenPositions);
        Assert.Equal(0m, result.TotalEmployeesChangePercent);
        Assert.Equal(1, result.WorkforceMovementTrend.Single(x => x.Month == "2026-10").Exits);
        Assert.Equal(1, result.LeaveRequestCohortTrend.Single(x => x.Month == "2026-11").Approved);
        Assert.Equal(1, result.LeaveRequestCohortTrend.Single(x => x.Month == "2026-11").TotalRequests);
        Assert.All(result.RecentActivities, a => Assert.StartsWith("a", a.EmployeeId));
    }

    [Fact]
    public async Task Overview_WhenLeaveModuleDisabled_DoesNotQueryOrReturnLeaveDerivedAnalytics()
    {
        var employees = new List<EmpUser> { Employee("a1", "2025-01-01"), Employee("a2", "2025-01-01") };
        var leaveRepo = Repo(new List<LeaveRequest>
        {
            new() { CompanyId = "tenant-a", EmployeeId = "a2", LeavePolicyId = "policy-1", Reason = "private reason", Status = EnumsHelper.LeaveRequestStatus.Accepted, StartDate = Today, EndDate = Today, CreatedDate = Today },
        });
        var roles = new Mock<IRoleService>();
        roles.Setup(x => x.GetModuleAvailabilityForCompany("tenant-a", It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [AppModule.Employees] = true,
                [AppModule.Attendance] = true,
                [AppModule.LeaveManagement] = false,
                [AppModule.Jobs] = true,
                [AppModule.Offboarding] = true,
            });
        var calendar = new Mock<ICompanyWorkingCalendarService>();
        calendar.Setup(x => x.IsWorkingDayAsync("tenant-a", DateOnly.FromDateTime(Today), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var attendanceRepo = Repo(new List<AttendanceModel>
        {
            new() { CompanyId = "tenant-a", UserId = "a1", Date = Today, CheckInTime = Today.AddHours(9) },
        });
        var service = new DashboardOverviewService(
            Repo(employees).Object,
            attendanceRepo.Object,
            leaveRepo.Object,
            new Mock<IMongoDbRepository<LeavePolicy>>().Object,
            Repo(new List<JobVacancy>()).Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            Repo(new List<EmployeeExit>()).Object,
            calendar.Object,
            roles.Object);

        var result = await service.GetOverviewAsync("tenant-a", Today);

        Assert.False(result.SourceAvailability?.LeaveManagement);
        Assert.Null(result.AttendanceBreakdown);
        Assert.Equal(0, result.OnLeaveToday);
        Assert.Equal(0, result.AbsentToday);
        Assert.Empty(result.LeaveRequestCohortTrend);
        leaveRepo.Verify(x => x.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        leaveRepo.Verify(x => x.GetAggregateDataAsync<LeaveRequest>(
            It.IsAny<Expression<Func<LeaveRequest, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<LeaveRequest, LeaveRequest>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(AppModule.Employees)]
    [InlineData(AppModule.Attendance)]
    [InlineData(AppModule.LeaveManagement)]
    [InlineData(AppModule.Jobs)]
    [InlineData(AppModule.Offboarding)]
    public async Task Overview_WhenOneSourceModuleIsDisabled_KeepsUnrelatedAnalyticsAvailable(string disabledModule)
    {
        var employeeRows = Repo(new List<EmpUser> { Employee("a1", "2025-01-01") });
        var attendanceRows = Repo(new List<AttendanceModel>
        {
            new() { CompanyId = "tenant-a", UserId = "a1", Date = Today, CheckInTime = Today.AddHours(9) },
        });
        var leaveRows = Repo(new List<LeaveRequest>
        {
            new() { CompanyId = "tenant-a", EmployeeId = "a1", LeavePolicyId = "policy-1", Reason = "x", Status = EnumsHelper.LeaveRequestStatus.Accepted, StartDate = Today, EndDate = Today, CreatedDate = Today },
        });
        var policyRows = Repo(new List<LeavePolicy>());
        var vacancyRows = Repo(new List<JobVacancy>
        {
            new() { CompanyId = "tenant-a", Status = true, Title = "Open role", CreatedDate = Today },
        });
        var exitRows = Repo(new List<EmployeeExit>
        {
            new() { CompanyId = "tenant-a", EmployeeUserId = "former", Status = "Resigned", CompletedAtUtc = Today, LastWorkingDate = Today.AddDays(-1), InitiatedAtUtc = Today.AddDays(-2) },
        });
        var jobTitleRows = Repo(new List<JobTitles>());
        var roles = new Mock<IRoleService>();
        var availability = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            [AppModule.Employees] = true,
            [AppModule.Attendance] = true,
            [AppModule.LeaveManagement] = true,
            [AppModule.Jobs] = true,
            [AppModule.Offboarding] = true,
        };
        availability[disabledModule] = false;
        roles.Setup(x => x.GetModuleAvailabilityForCompany("tenant-a", It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(availability);
        var calendar = new Mock<ICompanyWorkingCalendarService>();
        calendar.Setup(x => x.IsWorkingDayAsync("tenant-a", DateOnly.FromDateTime(Today), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new DashboardOverviewService(
            employeeRows.Object,
            attendanceRows.Object,
            leaveRows.Object,
            policyRows.Object,
            vacancyRows.Object,
            jobTitleRows.Object,
            exitRows.Object,
            calendar.Object,
            roles.Object);

        var result = await service.GetOverviewAsync("tenant-a", Today);

        Assert.NotNull(result.SourceAvailability);
        var disabledSourceIsReportedUnavailable = disabledModule switch
        {
            AppModule.Employees => !result.SourceAvailability!.Employees,
            AppModule.Attendance => !result.SourceAvailability!.Attendance,
            AppModule.LeaveManagement => !result.SourceAvailability!.LeaveManagement,
            AppModule.Jobs => !result.SourceAvailability!.Jobs,
            AppModule.Offboarding => !result.SourceAvailability!.Offboarding,
            _ => false,
        };
        Assert.True(disabledSourceIsReportedUnavailable);

        switch (disabledModule)
        {
            case AppModule.Employees:
                Assert.Equal(0, result.TotalEmployees);
                Assert.Empty(result.HeadcountTrend);
                Assert.Null(result.AttendanceBreakdown);
                Assert.Equal(12, result.LeaveRequestCohortTrend.Count);
                Assert.Equal(1, result.OpenPositions);
                employeeRows.Verify(x => x.GetAggregateDataAsync<EmpUser>(
                    It.IsAny<Expression<Func<EmpUser, bool>>>(),
                    It.IsAny<MongoDB.Driver.ProjectionDefinition<EmpUser, EmpUser>>(),
                    It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
                break;
            case AppModule.Attendance:
                Assert.Equal(1, result.TotalEmployees);
                Assert.Equal(0, result.PresentToday);
                Assert.Equal(1, result.OnLeaveToday);
                Assert.Null(result.AttendanceBreakdown);
                Assert.Equal(12, result.HeadcountTrend.Count);
                attendanceRows.Verify(x => x.GetAggregateDataAsync<AttendanceModel>(
                    It.IsAny<Expression<Func<AttendanceModel, bool>>>(),
                    It.IsAny<MongoDB.Driver.ProjectionDefinition<AttendanceModel, AttendanceModel>>(),
                    It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
                break;
            case AppModule.LeaveManagement:
                Assert.Equal(1, result.TotalEmployees);
                Assert.Equal(1, result.PresentToday);
                Assert.Equal(0, result.OnLeaveToday);
                Assert.Null(result.AttendanceBreakdown);
                Assert.Empty(result.LeaveRequestCohortTrend);
                leaveRows.Verify(x => x.GetAggregateDataAsync<LeaveRequest>(
                    It.IsAny<Expression<Func<LeaveRequest, bool>>>(),
                    It.IsAny<MongoDB.Driver.ProjectionDefinition<LeaveRequest, LeaveRequest>>(),
                    It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
                policyRows.Verify(x => x.GetAll(It.IsAny<Expression<Func<LeavePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
                policyRows.Verify(x => x.GetAggregateDataAsync<LeavePolicy>(
                    It.IsAny<Expression<Func<LeavePolicy, bool>>>(),
                    It.IsAny<MongoDB.Driver.ProjectionDefinition<LeavePolicy, LeavePolicy>>(),
                    It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
                break;
            case AppModule.Jobs:
                Assert.Equal(0, result.OpenPositions);
                Assert.Equal(1, result.TotalEmployees);
                Assert.NotNull(result.AttendanceBreakdown);
                Assert.Equal(12, result.HeadcountTrend.Count);
                vacancyRows.Verify(x => x.Count(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>()), Times.Never);
                vacancyRows.Verify(x => x.GetAggregateDataAsync<JobVacancy>(
                    It.IsAny<Expression<Func<JobVacancy, bool>>>(),
                    It.IsAny<MongoDB.Driver.ProjectionDefinition<JobVacancy, JobVacancy>>(),
                    It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
                jobTitleRows.Verify(x => x.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
                jobTitleRows.Verify(x => x.GetAggregateDataAsync<JobTitles>(
                    It.IsAny<Expression<Func<JobTitles, bool>>>(),
                    It.IsAny<MongoDB.Driver.ProjectionDefinition<JobTitles, JobTitles>>(),
                    It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
                break;
            case AppModule.Offboarding:
                Assert.Empty(result.WorkforceMovementTrend);
                Assert.Equal(1, result.TotalEmployees);
                Assert.NotNull(result.AttendanceBreakdown);
                Assert.Equal(12, result.HeadcountTrend.Count);
                exitRows.Verify(x => x.GetAll(It.IsAny<Expression<Func<EmployeeExit, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
                exitRows.Verify(x => x.GetAggregateDataAsync<EmployeeExit>(
                    It.IsAny<Expression<Func<EmployeeExit, bool>>>(),
                    It.IsAny<MongoDB.Driver.ProjectionDefinition<EmployeeExit, EmployeeExit>>(),
                    It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
                break;
        }
    }

    [Fact]
    public async Task RecentActivities_ResolveJoinedRoleAndApprovedLeaveLabelsFromProjectedLookups()
    {
        var employee = Employee("a1", Today.ToString("yyyy-MM-dd"));
        employee.JobRole = "role-1";
        var employeeRows = Repo(new List<EmpUser> { employee });
        var leaveRows = Repo(new List<LeaveRequest>
        {
            new()
            {
                CompanyId = "tenant-a",
                EmployeeId = "a1",
                LeavePolicyId = "policy-1",
                Reason = "private reason",
                Status = EnumsHelper.LeaveRequestStatus.Accepted,
                StartDate = Today,
                EndDate = Today,
                CreatedDate = Today,
                ReviewedAt = Today.AddHours(10),
            },
        });
        var titleRows = Repo(new List<JobTitles>
        {
            new()
            {
                CompanyId = "tenant-a",
                JobTitleId = "role-1",
                Titles =
                [
                    new MultilingualModel { Language = "hi", Label = "इंजीनियर" },
                    new MultilingualModel { Language = "en", Label = "Engineer" },
                ],
            },
        });
        var policyRows = Repo(new List<LeavePolicy>
        {
            new() { CompanyId = "tenant-a", Id = "policy-1", Name = "Annual leave", Code = "AL" },
        });
        var roles = new Mock<IRoleService>();
        roles.Setup(x => x.GetModuleAvailabilityForCompany("tenant-a", It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [AppModule.Employees] = true,
                [AppModule.Attendance] = true,
                [AppModule.LeaveManagement] = true,
                [AppModule.Jobs] = true,
                [AppModule.Offboarding] = true,
            });
        var calendar = new Mock<ICompanyWorkingCalendarService>();
        calendar.Setup(x => x.IsWorkingDayAsync("tenant-a", DateOnly.FromDateTime(Today), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new DashboardOverviewService(
            employeeRows.Object,
            Repo(new List<AttendanceModel>()).Object,
            leaveRows.Object,
            policyRows.Object,
            Repo(new List<JobVacancy>()).Object,
            titleRows.Object,
            Repo(new List<EmployeeExit>()).Object,
            calendar.Object,
            roles.Object);

        var result = await service.GetOverviewAsync("tenant-a", Today);

        Assert.Contains(result.RecentActivities, activity => activity.Type == "joined" && activity.Detail == "Engineer");
        Assert.Contains(result.RecentActivities, activity => activity.Type == "leaveApproved" && activity.Detail == "Annual leave");
        titleRows.Verify(x => x.GetAggregateDataAsync<JobTitles>(
            It.IsAny<Expression<Func<JobTitles, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<JobTitles, JobTitles>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Once);
        policyRows.Verify(x => x.GetAggregateDataAsync<LeavePolicy>(
            It.IsAny<Expression<Func<LeavePolicy, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<LeavePolicy, LeavePolicy>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Overview_WhenAllSourceModulesAreDisabled_FailsClosedWithoutQueryingSourceRepositories()
    {
        var roles = new Mock<IRoleService>();
        roles.Setup(x => x.GetModuleAvailabilityForCompany("tenant-a", It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [AppModule.Employees] = false,
                [AppModule.Attendance] = false,
                [AppModule.LeaveManagement] = false,
                [AppModule.Jobs] = false,
                [AppModule.Offboarding] = false,
            });
        var employees = new Mock<IMongoDbRepository<EmpUser>>();
        var attendance = new Mock<IMongoDbRepository<AttendanceModel>>();
        var leaveRequests = new Mock<IMongoDbRepository<LeaveRequest>>();
        var leavePolicies = new Mock<IMongoDbRepository<LeavePolicy>>();
        var vacancies = new Mock<IMongoDbRepository<JobVacancy>>();
        var jobTitles = new Mock<IMongoDbRepository<JobTitles>>();
        var exits = new Mock<IMongoDbRepository<EmployeeExit>>();
        var calendar = new Mock<ICompanyWorkingCalendarService>();
        calendar.Setup(x => x.IsWorkingDayAsync("tenant-a", DateOnly.FromDateTime(Today), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new DashboardOverviewService(
            employees.Object,
            attendance.Object,
            leaveRequests.Object,
            leavePolicies.Object,
            vacancies.Object,
            jobTitles.Object,
            exits.Object,
            calendar.Object,
            roles.Object);

        var result = await service.GetOverviewAsync("tenant-a", Today);

        Assert.NotNull(result.SourceAvailability);
        Assert.False(result.SourceAvailability.Employees);
        Assert.False(result.SourceAvailability.Attendance);
        Assert.False(result.SourceAvailability.LeaveManagement);
        Assert.False(result.SourceAvailability.Jobs);
        Assert.False(result.SourceAvailability.Offboarding);
        Assert.Equal(0, result.TotalEmployees);
        Assert.Null(result.TotalEmployeesChangePercent);
        Assert.Equal(0, result.PresentToday);
        Assert.Equal(0, result.OnLeaveToday);
        Assert.Equal(0, result.AbsentToday);
        Assert.Equal(0, result.OpenPositions);
        Assert.Null(result.AttendanceBreakdown);
        Assert.Empty(result.HeadcountTrend);
        Assert.Empty(result.WorkforceMovementTrend);
        Assert.Empty(result.LeaveRequestCohortTrend);
        Assert.Empty(result.RecentActivities);
        employees.Verify(x => x.GetAggregateDataAsync<EmpUser>(
            It.IsAny<Expression<Func<EmpUser, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<EmpUser, EmpUser>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
        attendance.Verify(x => x.GetAggregateDataAsync<AttendanceModel>(
            It.IsAny<Expression<Func<AttendanceModel, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<AttendanceModel, AttendanceModel>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
        attendance.Verify(x => x.GetAll(It.IsAny<Expression<Func<AttendanceModel, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        leaveRequests.Verify(x => x.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        leaveRequests.Verify(x => x.GetAggregateDataAsync<LeaveRequest>(
            It.IsAny<Expression<Func<LeaveRequest, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<LeaveRequest, LeaveRequest>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
        vacancies.Verify(x => x.Count(It.IsAny<Expression<Func<JobVacancy, bool>>>(), It.IsAny<bool>()), Times.Never);
        vacancies.Verify(x => x.GetAggregateDataAsync<JobVacancy>(
            It.IsAny<Expression<Func<JobVacancy, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<JobVacancy, JobVacancy>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
        jobTitles.Verify(x => x.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        jobTitles.Verify(x => x.GetAggregateDataAsync<JobTitles>(
            It.IsAny<Expression<Func<JobTitles, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<JobTitles, JobTitles>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
        leavePolicies.Verify(x => x.GetAll(It.IsAny<Expression<Func<LeavePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        leavePolicies.Verify(x => x.GetAggregateDataAsync<LeavePolicy>(
            It.IsAny<Expression<Func<LeavePolicy, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<LeavePolicy, LeavePolicy>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
        exits.Verify(x => x.GetAll(It.IsAny<Expression<Func<EmployeeExit, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        exits.Verify(x => x.GetAggregateDataAsync<EmployeeExit>(
            It.IsAny<Expression<Func<EmployeeExit, bool>>>(),
            It.IsAny<MongoDB.Driver.ProjectionDefinition<EmployeeExit, EmployeeExit>>(),
            It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Overview_WhenSourceQueryFails_PropagatesFailureInsteadOfReturningSuccessfulZeroMetrics()
    {
        var roles = new Mock<IRoleService>();
        roles.Setup(x => x.GetModuleAvailabilityForCompany("tenant-a", It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [AppModule.Employees] = true,
                [AppModule.Attendance] = true,
                [AppModule.LeaveManagement] = true,
                [AppModule.Jobs] = true,
                [AppModule.Offboarding] = true,
            });
        var employees = new Mock<IMongoDbRepository<EmpUser>>();
        employees.Setup(x => x.GetAggregateDataAsync<EmpUser>(
                It.IsAny<Expression<Func<EmpUser, bool>>>(),
                It.IsAny<MongoDB.Driver.ProjectionDefinition<EmpUser, EmpUser>>(),
                It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("database connection string must not be exposed"));
        var calendar = new Mock<ICompanyWorkingCalendarService>();
        var service = new DashboardOverviewService(
            employees.Object,
            new Mock<IMongoDbRepository<AttendanceModel>>().Object,
            new Mock<IMongoDbRepository<LeaveRequest>>().Object,
            new Mock<IMongoDbRepository<LeavePolicy>>().Object,
            new Mock<IMongoDbRepository<JobVacancy>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new Mock<IMongoDbRepository<EmployeeExit>>().Object,
            calendar.Object,
            roles.Object);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetOverviewAsync("tenant-a", Today));

        Assert.Contains("database connection string", failure.Message);
        calendar.Verify(x => x.IsWorkingDayAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IMongoDbRepository<T>> Repo<T>(List<T> rows) where T : class
    {
        var repo = new Mock<IMongoDbRepository<T>>();
        repo.Setup(x => x.GetAll(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter, bool _, bool _) => rows.Where(filter.Compile()).ToList());
        repo.Setup(x => x.GetAggregateDataAsync<T>(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<MongoDB.Driver.ProjectionDefinition<T, T>>(),
                It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter,
                MongoDB.Driver.ProjectionDefinition<T, T>? _, bool _, bool? _, string? _, int? _, int? _, string? _) =>
                rows.Where(filter.Compile()).ToList());
        repo.Setup(x => x.Count(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter, bool _) => rows.Count(filter.Compile()));
        return repo;
    }
}
