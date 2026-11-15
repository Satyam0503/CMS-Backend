using System.Linq.Expressions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Dashboard;
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
            Employee("c", "2026-11-10"),
            Employee("d", "2026-11-20"), // joins after "today": not yet counted
        };

        var trend = DashboardOverviewService.BuildHeadcountTrend(employees, Today);

        Assert.Equal(["2026-06", "2026-07", "2026-08", "2026-09", "2026-10", "2026-11"], trend.Select(x => x.Month));
        Assert.Equal([2, 2, 1, 1, 1, 2], trend.Select(x => x.Count));
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
    public async Task Overview_OnlyCountsCallerTenant()
    {
        var employees = new List<EmpUser>
        {
            Employee("a1", "2025-01-01"), Employee("a2", "2025-01-01"), Employee("a3", "2025-01-01"),
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
            new() { CompanyId = "tenant-a", EmployeeId = "a2", LeavePolicyId = "p", Reason = "x", Status = EnumsHelper.LeaveRequestStatus.Accepted, StartDate = Today, EndDate = Today },
            new() { CompanyId = "tenant-b", EmployeeId = "b2", LeavePolicyId = "p", Reason = "x", Status = EnumsHelper.LeaveRequestStatus.Accepted, StartDate = Today, EndDate = Today },
        };
        var vacancies = new List<JobVacancy>
        {
            new() { CompanyId = "tenant-a", Status = true, Title = "QA" },
            new() { CompanyId = "tenant-a", Status = false, Title = "Closed" },
            new() { CompanyId = "tenant-b", Status = true, Title = "Other" },
        };

        var calendar = new Mock<ICompanyWorkingCalendarService>();
        calendar.Setup(x => x.IsWorkingDayAsync("tenant-a", DateOnly.FromDateTime(Today), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = new DashboardOverviewService(
            Repo(employees).Object,
            Repo(attendance).Object,
            Repo(leaves).Object,
            new Mock<IMongoDbRepository<LeavePolicy>>().Object,
            Repo(vacancies).Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new Mock<IMongoDbRepository<EmployeeExit>>().Object,
            new Mock<IMongoDbRepository<EmployeeSalaryStructure>>().Object,
            calendar.Object);

        var result = await service.GetOverviewAsync("tenant-a", Today);

        Assert.Equal(3, result.TotalEmployees);
        Assert.Equal(1, result.PresentToday);
        Assert.Equal(1, result.OnLeaveToday);
        Assert.Equal(1, result.AbsentToday);
        Assert.True(result.IsWorkingDay);
        Assert.Equal(1, result.OpenPositions);
        Assert.Equal(0m, result.TotalEmployeesChangePercent);
        Assert.All(result.RecentActivities, a => Assert.StartsWith("a", a.EmployeeId));
    }

    private static Mock<IMongoDbRepository<T>> Repo<T>(List<T> rows) where T : class
    {
        var repo = new Mock<IMongoDbRepository<T>>();
        repo.Setup(x => x.GetAll(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter, bool _, bool _) => rows.Where(filter.Compile()).ToList());
        repo.Setup(x => x.Count(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter, bool _) => rows.Count(filter.Compile()));
        return repo;
    }
}
