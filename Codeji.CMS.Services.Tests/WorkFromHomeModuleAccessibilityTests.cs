using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.LeaveManagement;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
using System.Security.Claims;

namespace Codeji.CMS.Services.Tests;

public class WorkFromHomeModuleAccessibilityTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IMongoDbRepository<WorkFromHomePolicy>> _policyRepo = new();
    private readonly Mock<IRoleService> _roles = new();

    private WorkFromHomeService CreateService(bool isModuleAccessible, string companyId = "comp-123", string userId = "user-456")
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("CompanyId", companyId),
            new Claim("UserId", userId),
            new Claim(ClaimTypes.Role, "Administrator")
        ]));
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        _roles.Setup(x => x.IsModuleAccessibleForCompany("Work_From_Home", companyId))
            .ReturnsAsync(isModuleAccessible);

        return new WorkFromHomeService(
            _httpContextAccessor.Object,
            _policyRepo.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeRequest>>(),
            Mock.Of<IMongoDbRepository<WorkFromHomeRequestLog>>(),
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            Mock.Of<IMongoDbRepository<EmpUser>>(),
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            Mock.Of<IMongoDbRepository<AttendanceStatusSetting>>(),
            Mock.Of<IMongoDbRepository<AttendanceModel>>(),
            Mock.Of<IMongoDbRepository<AttendanceDaySegment>>(),
            Mock.Of<IMongoDbRepository<AttendancePayrollException>>(),
            Mock.Of<IMongoDbRepository<MonthlyAttendanceSummary>>(),
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            Mock.Of<IAttendanceEditGuard>(),
            Mock.Of<IEffectiveOfficeScheduleService>(),
            Mock.Of<IAttendanceInitializationService>(),
            _roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoDbRepository<Notifications>>(),
            Mock.Of<IMongoDbRepository<UserNotifications>>(),
            Mock.Of<INotificationService>(),
            Mock.Of<IPriorityTaskQueue>(),
            Mock.Of<IMiddlewareService>(),
            Mock.Of<IMongoClient>(),
            Mock.Of<ILeaveManagementService>()
        );
    }

    [Fact]
    public async Task GetPolicy_WhenModuleDisabled_ReturnsDisabledPolicyDto()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.GetPolicy();

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.False(result.MethodResult.IsEnabled);
        Assert.Equal(0, result.MethodResult.MaxDaysPerWeek);
    }

    [Fact]
    public async Task GetEmployeeContext_WhenModuleDisabled_ReturnsModuleDisabledReason()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.GetEmployeeContext();

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal("WFH_MODULE_DISABLED", result.MethodResult.DisabledReasonCode);
        Assert.False(result.MethodResult.IsEligible);
    }

    [Fact]
    public async Task SavePolicy_WhenModuleDisabled_ReturnsAccessError()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.SavePolicy(new WorkFromHomePolicyDto { IsEnabled = true, MaxDaysPerWeek = 2 });

        Assert.False(result.Success);
        Assert.Contains("disabled", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveEmployeeAllocation_WhenModuleDisabled_ReturnsAccessError()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.SaveEmployeeAllocation(new WorkFromHomeEmployeeAllocationDto { EmployeeId = "EMP101", WeeklyLimit = 2 });

        Assert.False(result.Success);
        Assert.Contains("disabled", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetEmployeeAllocations_WhenModuleDisabled_ReturnsEmptyList()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.GetEmployeeAllocations();

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Empty(result.MethodResult);
    }

    [Fact]
    public async Task GetMy_WhenModuleDisabled_ReturnsEmptyResults()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.GetMy();

        Assert.True(result.Success);
        Assert.Empty(result.MethodResults);
        Assert.Equal(0, result.TotalRecords);
    }
}
