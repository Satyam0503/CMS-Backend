using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.LeaveManagement;
using Codeji.CMS.Utility.Enums;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public class SandwichRuleAndCompOffPolicyTests
{
    private const string TestCompanyId = "comp-sandwich-101";
    private const string TestUserId = "usr-emp-101";
    private const string TestReviewerId = "usr-mgr-201";

    [Fact]
    public async Task SandwichRule_WhenDisabled_DoesNotIncludeInterveningWeekends()
    {
        // Arrange
        var weeklyOffsMock = new Mock<IMongoDbRepository<WeeklyOffSetting>>();
        weeklyOffsMock.Setup(r => r.GetAll(It.IsAny<Expression<Func<WeeklyOffSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<WeeklyOffSetting>
            {
                new() { CompanyId = TestCompanyId, OffDays = [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday] }
            });

        var calendarMock = new Mock<IMongoDbRepository<CalendarEntity>>();
        calendarMock.Setup(r => r.GetAll(It.IsAny<Expression<Func<CalendarEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<CalendarEntity>());

        var calendarService = new CompanyWorkingCalendarService(weeklyOffsMock.Object, calendarMock.Object);

        // Friday 2026-11-20 to Monday 2026-11-23
        var friday = new DateOnly(2026, 11, 20);
        var monday = new DateOnly(2026, 11, 23);

        // Act: Sandwich rules disabled
        var result = await calendarService.EvaluateSandwichLeaveDatesAsync(
            TestCompanyId, friday, monday,
            weekendInclusive: false,
            holidayInclusive: false,
            enableSandwichRules: false,
            includeWeekendsInSandwich: false,
            includeHolidaysInSandwich: false);

        // Assert: Only Friday and Monday are counted (2 days)
        Assert.False(result.IsSandwichApplied);
        Assert.Equal(0, result.SandwichedDaysCount);
        Assert.Equal(2, result.LeaveDates.Count);
        Assert.Contains(friday, result.LeaveDates);
        Assert.Contains(monday, result.LeaveDates);
    }

    [Fact]
    public async Task SandwichRule_WhenEnabledWithWeekends_IncludesInterveningSaturdayAndSunday()
    {
        // Arrange
        var weeklyOffsMock = new Mock<IMongoDbRepository<WeeklyOffSetting>>();
        weeklyOffsMock.Setup(r => r.GetAll(It.IsAny<Expression<Func<WeeklyOffSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<WeeklyOffSetting>
            {
                new() { CompanyId = TestCompanyId, OffDays = [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday] }
            });

        var calendarMock = new Mock<IMongoDbRepository<CalendarEntity>>();
        calendarMock.Setup(r => r.GetAll(It.IsAny<Expression<Func<CalendarEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<CalendarEntity>());

        var calendarService = new CompanyWorkingCalendarService(weeklyOffsMock.Object, calendarMock.Object);

        // Friday 2026-11-20 to Monday 2026-11-23
        var friday = new DateOnly(2026, 11, 20);
        var saturday = new DateOnly(2026, 11, 21);
        var sunday = new DateOnly(2026, 11, 22);
        var monday = new DateOnly(2026, 11, 23);

        // Act: Sandwich rules enabled for weekends
        var result = await calendarService.EvaluateSandwichLeaveDatesAsync(
            TestCompanyId, friday, monday,
            weekendInclusive: false,
            holidayInclusive: false,
            enableSandwichRules: true,
            includeWeekendsInSandwich: true,
            includeHolidaysInSandwich: false);

        // Assert: 4 days counted, with 2 sandwiched days
        Assert.True(result.IsSandwichApplied);
        Assert.Equal(2, result.SandwichedDaysCount);
        Assert.Equal(4, result.LeaveDates.Count);
        Assert.Contains(friday, result.LeaveDates);
        Assert.Contains(saturday, result.LeaveDates);
        Assert.Contains(sunday, result.LeaveDates);
        Assert.Contains(monday, result.LeaveDates);
    }

    [Fact]
    public async Task SandwichRule_WhenEnabledWithHolidays_IncludesInterveningPublicHoliday()
    {
        // Arrange
        var weeklyOffsMock = new Mock<IMongoDbRepository<WeeklyOffSetting>>();
        weeklyOffsMock.Setup(r => r.GetAll(It.IsAny<Expression<Func<WeeklyOffSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<WeeklyOffSetting>
            {
                new() { CompanyId = TestCompanyId, OffDays = [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday] }
            });

        // Thursday 2026-11-19 is a public holiday
        var thursday = new DateOnly(2026, 11, 19);
        var holidayEvent = new CalendarEntity
        {
            CompanyId = TestCompanyId,
            Type = EnumsHelper.CalendarItem.Holiday,
            Date = thursday.ToDateTime(TimeOnly.MinValue),
            Recurring = false
        };

        var calendarMock = new Mock<IMongoDbRepository<CalendarEntity>>();
        calendarMock.Setup(r => r.GetAll(It.IsAny<Expression<Func<CalendarEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<CalendarEntity> { holidayEvent });

        var calendarService = new CompanyWorkingCalendarService(weeklyOffsMock.Object, calendarMock.Object);

        var wednesday = new DateOnly(2026, 11, 18);
        var friday = new DateOnly(2026, 11, 20);

        // Act: Sandwich rules enabled for holidays
        var result = await calendarService.EvaluateSandwichLeaveDatesAsync(
            TestCompanyId, wednesday, friday,
            weekendInclusive: false,
            holidayInclusive: false,
            enableSandwichRules: true,
            includeWeekendsInSandwich: false,
            includeHolidaysInSandwich: true);

        // Assert: 3 days counted, with Thursday holiday sandwiched
        Assert.True(result.IsSandwichApplied);
        Assert.Equal(1, result.SandwichedDaysCount);
        Assert.Equal(3, result.LeaveDates.Count);
        Assert.Contains(wednesday, result.LeaveDates);
        Assert.Contains(thursday, result.LeaveDates);
        Assert.Contains(friday, result.LeaveDates);
    }

    [Fact]
    public async Task CompOff_RequestAndApproval_CreditsLeaveBalance()
    {
        // Arrange
        var compOffRequests = new List<CompOffCreditRequest>();
        var compOffRepoMock = new Mock<IMongoDbRepository<CompOffCreditRequest>>();
        compOffRepoMock.Setup(r => r.AddOne(It.IsAny<CompOffCreditRequest>()))
            .Callback<CompOffCreditRequest>(req => compOffRequests.Add(req))
            .ReturnsAsync(new Result { Success = true });

        compOffRepoMock.Setup(r => r.FirstOrDefault(It.IsAny<Expression<Func<CompOffCreditRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<CompOffCreditRequest, bool>> expr, bool _, bool _) => compOffRequests.AsQueryable().FirstOrDefault(expr));

        compOffRepoMock.Setup(r => r.Update(It.IsAny<FilterDefinition<CompOffCreditRequest>>(), It.IsAny<CompOffCreditRequest>()))
            .ReturnsAsync(new Result { Success = true });

        var leavePolicy = new LeavePolicy
        {
            Id = "pol-compoff-1",
            CompanyId = TestCompanyId,
            Name = "Compensatory Off",
            Code = "COMP_OFF",
            EnableCompOffEarning = true,
            CompOffValidityDays = 60,
            Status = true
        };
        var policyRepoMock = new Mock<IMongoDbRepository<LeavePolicy>>();
        policyRepoMock.Setup(r => r.GetAll(It.IsAny<Expression<Func<LeavePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<LeavePolicy> { leavePolicy });
        policyRepoMock.Setup(r => r.FirstOrDefault(It.IsAny<Expression<Func<LeavePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(leavePolicy);

        var leaveBalances = new List<EmployeeLeaveBalance>
        {
            new()
            {
                Id = "bal-101",
                CompanyId = TestCompanyId,
                UserId = TestUserId,
                LeavePolicyId = leavePolicy.Id,
                TotalAllocated = 0,
                Taken = 0,
                Remaining = 0
            }
        };
        var balanceRepoMock = new Mock<IMongoDbRepository<EmployeeLeaveBalance>>();
        balanceRepoMock.Setup(r => r.FirstOrDefault(It.IsAny<Expression<Func<EmployeeLeaveBalance, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<EmployeeLeaveBalance, bool>> expr, bool _, bool _) => leaveBalances.AsQueryable().FirstOrDefault(expr));

        balanceRepoMock.Setup(r => r.Update(It.IsAny<FilterDefinition<EmployeeLeaveBalance>>(), It.IsAny<EmployeeLeaveBalance>()))
            .ReturnsAsync(new Result { Success = true });

        var service = new LeaveManagementService(
            new Mock<IMongoDbRepository<EmpUser>>().Object,
            new Mock<IMongoDbRepository<LeaveRequest>>().Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<MapsterMapper.IMapper>().Object,
            new Mock<INotificationService>().Object,
            new Mock<IMongoDbRepository<Notifications>>().Object,
            new Mock<IMongoDbRepository<UserNotifications>>().Object,
            new Mock<IMongoDbRepository<Roles>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new Mock<IEmployeeService>().Object,
            new Mock<IMiddlewareService>().Object,
            policyRepoMock.Object,
            balanceRepoMock.Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            new Mock<IMongoDbRepository<AttendanceStatusSetting>>().Object,
            new Mock<ILeaveAttendanceReconciliationService>().Object,
            new Mock<IMongoDbRepository<WorkFromHomePolicy>>().Object,
            new Mock<IMongoDbRepository<WorkFromHomeRequest>>().Object,
            new Mock<IMongoClient>().Object,
            compOffCreditRepo: compOffRepoMock.Object
        );

        // Act 1: Submit Comp-Off credit request for 8 hours on Sunday
        var requestDto = new CompOffCreditRequestDto
        {
            WorkedDate = DateTime.UtcNow.Date.AddDays(-2),
            HoursWorked = 8,
            CreditDays = 1.0m,
            Reason = "Server emergency maintenance over the weekend"
        };
        var submitResult = await service.RequestCompOffCredit(requestDto, TestUserId, TestCompanyId);

        // Assert 1: Submit succeeded
        Assert.True(submitResult.Success);
        Assert.NotNull(submitResult.MethodResult);
        var createdRequestId = submitResult.MethodResult;

        // Act 2: Review and Approve
        var reviewDto = new ReviewCompOffCreditDto
        {
            RequestId = createdRequestId,
            IsApproved = true,
            ReviewRemarks = "Approved, great job handling the outage"
        };
        var reviewResult = await service.ReviewCompOffCredit(reviewDto, TestReviewerId, TestCompanyId);

        // Assert 2: Review succeeded and balance credited
        Assert.True(reviewResult.Success);
        var targetBalance = leaveBalances.First(b => b.UserId == TestUserId && b.LeavePolicyId == leavePolicy.Id);
        Assert.Equal(1.0m, targetBalance.TotalAllocated);
        Assert.Equal(1.0m, targetBalance.Remaining);
    }
}
