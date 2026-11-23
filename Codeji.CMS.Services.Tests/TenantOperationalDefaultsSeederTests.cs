using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Companies;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.LeaveManagement;
using Codeji.CMS.Utility.Enums;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class TenantOperationalDefaultsSeederTests
{
    [Fact]
    public void BuildDefaultLeavePolicies_ReturnsStandardStatutoryPolicies()
    {
        // Act
        var policies = DefaultCompanySeeds.BuildDefaultLeavePolicies("comp-100", "admin-1");

        // Assert
        Assert.NotNull(policies);
        Assert.Equal(3, policies.Count);

        var cl = policies.FirstOrDefault(p => p.Code == "CL");
        Assert.NotNull(cl);
        Assert.Equal("Casual Leave", cl.Name);
        Assert.Equal(12m, cl.MaxBalance);
        Assert.Equal(1.0m, cl.AccrualAmount);
        Assert.True(cl.HalfDayAllowed);
        Assert.False(cl.CarryOverAllowed);
        Assert.Equal("comp-100", cl.CompanyId);
        Assert.Equal("admin-1", cl.CreatedBy);

        var sl = policies.FirstOrDefault(p => p.Code == "SL");
        Assert.NotNull(sl);
        Assert.Equal("Sick Leave", sl.Name);
        Assert.Equal(12m, sl.MaxBalance);
        Assert.Equal(1.0m, sl.AccrualAmount);
        Assert.True(sl.HalfDayAllowed);
        Assert.False(sl.CarryOverAllowed);

        var pl = policies.FirstOrDefault(p => p.Code == "PL");
        Assert.NotNull(pl);
        Assert.Equal("Privilege Leave", pl.Name);
        Assert.Equal(30m, pl.MaxBalance);
        Assert.Equal(1.25m, pl.AccrualAmount);
        Assert.True(pl.CarryOverAllowed);
        Assert.Equal(30m, pl.CarryOverLimit);
        Assert.True(pl.HalfDayAllowed);
    }

    [Fact]
    public async Task GetAllLeavePolicies_AutoBootstrapsDefaultPolicies_WhenCompanyHasNoPolicies()
    {
        // Arrange
        var mockLeaveRepo = new Mock<IMongoDbRepository<LeavePolicy>>();
        var seeded = new List<LeavePolicy>();

        mockLeaveRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<LeavePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => seeded);

        mockLeaveRepo.Setup(r => r.FirstOrDefault(It.IsAny<Expression<Func<LeavePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => seeded.FirstOrDefault());

        mockLeaveRepo.Setup(r => r.AddOne(It.IsAny<LeavePolicy>()))
            .Callback<LeavePolicy>(p => seeded.Add(p))
            .ReturnsAsync(new Result { Success = true });

        var mockMapper = new Mock<MapsterMapper.IMapper>();
        mockMapper.Setup(m => m.Map<List<Codeji.CMS.DTO.LeaveManagement.LeavePolicy.UpdateLeavePolicyRequest>>(It.IsAny<List<LeavePolicy>>()))
            .Returns<List<LeavePolicy>>(src => src.Select(p => new Codeji.CMS.DTO.LeaveManagement.LeavePolicy.UpdateLeavePolicyRequest
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                PolicyType = p.PolicyType,
                Status = p.Status,
                Paid = p.Paid,
                MaxBalance = p.MaxBalance ?? 0,
                CarryOverAllowed = p.CarryOverAllowed,
                MinNoticeDays = p.MinNoticeDays,
                HalfDayAllowed = p.HalfDayAllowed,
                WeekendInclusive = p.WeekendInclusive,
                HolidayInclusive = p.HolidayInclusive
            }).ToList());

        var service = new LeaveManagementService(
            new Mock<IMongoDbRepository<EmpUser>>().Object,
            new Mock<IMongoDbRepository<LeaveRequest>>().Object,
            new Mock<IHttpContextAccessor>().Object,
            mockMapper.Object,
            new Mock<INotificationService>().Object,
            new Mock<IMongoDbRepository<Notifications>>().Object,
            new Mock<IMongoDbRepository<UserNotifications>>().Object,
            new Mock<IMongoDbRepository<Roles>>().Object,
            new Mock<IMongoDbRepository<JobTitles>>().Object,
            new Mock<IEmployeeService>().Object,
            new Mock<IMiddlewareService>().Object,
            mockLeaveRepo.Object,
            new Mock<IMongoDbRepository<EmployeeLeaveBalance>>().Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            new Mock<IMongoDbRepository<AttendanceStatusSetting>>().Object,
            new Mock<ILeaveAttendanceReconciliationService>().Object,
            new Mock<IMongoDbRepository<WorkFromHomePolicy>>().Object,
            new Mock<IMongoDbRepository<WorkFromHomeRequest>>().Object,
            new Mock<IMongoClient>().Object
        );

        // Act
        var result = await service.GetAllLeavePolicies("comp-new-tenant", true);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, seeded.Count);
        Assert.Contains(seeded, p => p.Code == "CL");
        Assert.Contains(seeded, p => p.Code == "SL");
        Assert.Contains(seeded, p => p.Code == "PL");
    }
}
