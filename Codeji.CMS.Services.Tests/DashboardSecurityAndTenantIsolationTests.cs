using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Dashboard;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using AuthRole = Codeji.CMS.Utility.Enums.EnumsHelper.Roles;
using Module = Codeji.CMS.Repository.Entities.RolePermissions.Module;
using Permission = Codeji.CMS.Repository.Entities.RolePermissions.Permission;

namespace Codeji.CMS.Services.Tests;

public class DashboardSecurityAndTenantIsolationTests
{
    private const string CompanyA = "company-alpha";
    private const string CompanyB = "company-beta";

    #region Permission Matrix and Resolution Tests

    [Fact]
    public async Task GetAllRolesWithPermission_IncludesDashboardWithViewAllPermission()
    {
        var fixture = new RoleServicesFixture();
        fixture.ModuleRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Module { ModuleId = 14, ModuleConstant = AppModule.Dashboard, ModuleName = "Dashboard" }]);
        fixture.PermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Permission { PermissionId = 5, PermissionConstant = Utility.Constraints.Permission.ViewAll, PermissionName = "View all" }]);
        fixture.ModulePermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new ModulePermission { ModulePermissionId = 63, ModuleId = 14, PermissionId = 5, HasModuleAccess = true }]);

        var result = await fixture.Service.GetAllRolesWithPermission(CompanyA);

        var dashModule = Assert.Single(result, m => m.ModuleConstant == AppModule.Dashboard);
        Assert.Equal("Dashboard", dashModule.ModuleName);
        var perm = Assert.Single(dashModule.Permissions);
        Assert.Equal(63, perm.ModulePermissionId);
        Assert.Equal("View all", perm.PermissionName);
        Assert.Equal(Utility.Constraints.Permission.ViewAll, perm.PermissionConstant);
    }

    [Fact]
    public async Task GetRolePermissionOfuser_EmitsDashboardViewAll_WhenRoleHasAccess()
    {
        var fixture = new RoleServicesFixture();
        var role = new Roles { RolesId = "role-lead", CompanyId = CompanyA, Titles = "Team Lead", UserRoles = [] };
        fixture.RoleRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false, true))
            .ReturnsAsync(role);
        fixture.ModuleRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Module { ModuleId = 14, ModuleConstant = AppModule.Dashboard, ModuleName = "Dashboard" }]);
        fixture.PermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Permission { PermissionId = 5, PermissionConstant = Utility.Constraints.Permission.ViewAll, PermissionName = "View all" }]);
        fixture.ModulePermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new ModulePermission { ModulePermissionId = 63, ModuleId = 14, PermissionId = 5, HasModuleAccess = true }]);

        var rolePermissions = new[]
        {
            new RolePermission { CompanyId = CompanyA, RoleId = "role-lead", ModulePermissionId = 63, HasAccess = true, IsAccessible = true }
        };
        fixture.RolePermissionRepository.Setup(x => x.Get(It.IsAny<Expression<Func<RolePermission, bool>>>(), null, false))
            .Returns((Expression<Func<RolePermission, bool>> predicate, object? _, bool _) => rolePermissions.Where(predicate.Compile()).AsQueryable());

        var permissions = await fixture.Service.GetRolePermissionOfuser("role-lead", CompanyA);

        Assert.Contains($"{AppModule.Dashboard}.{Utility.Constraints.Permission.ViewAll}", permissions);
    }

    [Fact]
    public async Task GetRolePermissionOfuser_DoesNotEmitDashboardViewAll_WhenRoleHasNoAccess()
    {
        var fixture = new RoleServicesFixture();
        var role = new Roles { RolesId = "role-emp", CompanyId = CompanyA, Titles = "Employee", UserRoles = [] };
        fixture.RoleRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false, true))
            .ReturnsAsync(role);
        fixture.ModuleRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Module { ModuleId = 14, ModuleConstant = AppModule.Dashboard, ModuleName = "Dashboard" }]);
        fixture.PermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Permission { PermissionId = 5, PermissionConstant = Utility.Constraints.Permission.ViewAll, PermissionName = "View all" }]);
        fixture.ModulePermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new ModulePermission { ModulePermissionId = 63, ModuleId = 14, PermissionId = 5, HasModuleAccess = true }]);

        // HasAccess is false
        var rolePermissions = new[]
        {
            new RolePermission { CompanyId = CompanyA, RoleId = "role-emp", ModulePermissionId = 63, HasAccess = false, IsAccessible = true }
        };
        fixture.RolePermissionRepository.Setup(x => x.Get(It.IsAny<Expression<Func<RolePermission, bool>>>(), null, false))
            .Returns((Expression<Func<RolePermission, bool>> predicate, object? _, bool _) => rolePermissions.Where(predicate.Compile()).AsQueryable());

        var permissions = await fixture.Service.GetRolePermissionOfuser("role-emp", CompanyA);

        Assert.DoesNotContain($"{AppModule.Dashboard}.{Utility.Constraints.Permission.ViewAll}", permissions);
    }

    #endregion

    #region Security and Access Control (VerifyUserAccess) Tests

    [Fact]
    public async Task VerifyUserAccess_AdministratorRole_AlwaysAllowedForDashboard()
    {
        var fixture = new RoleServicesFixture();
        fixture.Middleware.Setup(x => x.GetUserById("admin-1"))
            .ReturnsAsync(new UserModel { UserId = "admin-1", RoleId = "role-admin", CompanyId = CompanyA });
        fixture.RoleRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false, true))
            .ReturnsAsync(new Roles { RolesId = "role-admin", CompanyId = CompanyA, RoleType = (int)AuthRole.Administrator, Titles = "Admin", UserRoles = [] });
        fixture.ModuleRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Module { ModuleId = 14, ModuleConstant = AppModule.Dashboard, ModuleName = "Dashboard" }]);
        fixture.ModulePermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new ModulePermission { ModulePermissionId = 63, ModuleId = 14, PermissionId = 5 }]);
        fixture.RolePermissionRepository.Setup(x => x.Get(It.IsAny<Expression<Func<RolePermission, bool>>>(), null, false))
            .Returns(new[] { new RolePermission { CompanyId = CompanyA, RoleId = "role-admin", ModulePermissionId = 63, IsAccessible = true, HasAccess = true } }.AsQueryable());

        var allowed = await fixture.Service.VerifyUserAccess(
            AppModule.Dashboard,
            [Utility.Constraints.Permission.ViewAll],
            "admin-1",
            CompanyA);

        Assert.True(allowed);
    }

    [Theory]
    [InlineData((int)AuthRole.HR)]
    [InlineData((int)AuthRole.HRExecutive)]
    public async Task VerifyUserAccess_HRManagerAndExecutive_AlwaysAllowedForDashboard(int roleType)
    {
        var fixture = new RoleServicesFixture();
        fixture.Middleware.Setup(x => x.GetUserById("hr-1"))
            .ReturnsAsync(new UserModel { UserId = "hr-1", RoleId = "role-hr", CompanyId = CompanyA });
        fixture.RoleRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false, true))
            .ReturnsAsync(new Roles { RolesId = "role-hr", CompanyId = CompanyA, RoleType = roleType, Titles = "HR Role", UserRoles = [] });
        fixture.ModuleRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Module { ModuleId = 14, ModuleConstant = AppModule.Dashboard, ModuleName = "Dashboard" }]);
        fixture.ModulePermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new ModulePermission { ModulePermissionId = 63, ModuleId = 14, PermissionId = 5 }]);
        fixture.RolePermissionRepository.Setup(x => x.Get(It.IsAny<Expression<Func<RolePermission, bool>>>(), null, false))
            .Returns(new[] { new RolePermission { CompanyId = CompanyA, RoleId = "role-hr", ModulePermissionId = 63, IsAccessible = true, HasAccess = true } }.AsQueryable());

        var allowed = await fixture.Service.VerifyUserAccess(
            AppModule.Dashboard,
            [Utility.Constraints.Permission.ViewAll],
            "hr-1",
            CompanyA);

        Assert.True(allowed);
    }

    [Fact]
    public async Task VerifyUserAccess_CustomRole_WithDashboardViewAll_IsAllowed()
    {
        var fixture = new RoleServicesFixture();
        fixture.Middleware.Setup(x => x.GetUserById("auditor-1"))
            .ReturnsAsync(new UserModel { UserId = "auditor-1", RoleId = "role-auditor", CompanyId = CompanyA });
        fixture.RoleRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false, true))
            .ReturnsAsync(new Roles { RolesId = "role-auditor", CompanyId = CompanyA, RoleType = 99, Titles = "Auditor", UserRoles = [] });
        fixture.PermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Permission { PermissionId = 5, PermissionConstant = Utility.Constraints.Permission.ViewAll, PermissionName = "View all" }]);
        fixture.ModuleRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Module { ModuleId = 14, ModuleConstant = AppModule.Dashboard, ModuleName = "Dashboard" }]);
        fixture.ModulePermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new ModulePermission { ModulePermissionId = 63, ModuleId = 14, PermissionId = 5 }]);

        var grants = new[]
        {
            new RolePermission { CompanyId = CompanyA, RoleId = "role-auditor", ModulePermissionId = 63, IsAccessible = true, HasAccess = true }
        };
        fixture.RolePermissionRepository.Setup(x => x.Get(It.IsAny<Expression<Func<RolePermission, bool>>>(), null, false))
            .Returns((Expression<Func<RolePermission, bool>> predicate, object? _, bool _) => grants.Where(predicate.Compile()).AsQueryable());

        var allowed = await fixture.Service.VerifyUserAccess(
            AppModule.Dashboard,
            [Utility.Constraints.Permission.ViewAll],
            "auditor-1",
            CompanyA);

        Assert.True(allowed);
    }

    [Fact]
    public async Task VerifyUserAccess_CustomRole_WithoutDashboardViewAll_IsDenied()
    {
        var fixture = new RoleServicesFixture();
        fixture.Middleware.Setup(x => x.GetUserById("emp-1"))
            .ReturnsAsync(new UserModel { UserId = "emp-1", RoleId = "role-emp", CompanyId = CompanyA });
        fixture.RoleRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false, true))
            .ReturnsAsync(new Roles { RolesId = "role-emp", CompanyId = CompanyA, RoleType = (int)AuthRole.Employee, Titles = "Employee", UserRoles = [] });
        fixture.PermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Permission { PermissionId = 5, PermissionConstant = Utility.Constraints.Permission.ViewAll, PermissionName = "View all" }]);
        fixture.ModuleRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Module { ModuleId = 14, ModuleConstant = AppModule.Dashboard, ModuleName = "Dashboard" }]);
        fixture.ModulePermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new ModulePermission { ModulePermissionId = 63, ModuleId = 14, PermissionId = 5 }]);

        var grants = new[]
        {
            new RolePermission { CompanyId = CompanyA, RoleId = "role-emp", ModulePermissionId = 63, IsAccessible = true, HasAccess = false }
        };
        fixture.RolePermissionRepository.Setup(x => x.Get(It.IsAny<Expression<Func<RolePermission, bool>>>(), null, false))
            .Returns((Expression<Func<RolePermission, bool>> predicate, object? _, bool _) => grants.Where(predicate.Compile()).AsQueryable());

        var allowed = await fixture.Service.VerifyUserAccess(
            AppModule.Dashboard,
            [Utility.Constraints.Permission.ViewAll],
            "emp-1",
            CompanyA);

        Assert.False(allowed);
    }

    [Fact]
    public async Task VerifyUserAccess_WhenDashboardDisabledForCompany_DeniedEvenForAdmin()
    {
        var fixture = new RoleServicesFixture();
        fixture.Middleware.Setup(x => x.GetUserById("admin-1"))
            .ReturnsAsync(new UserModel { UserId = "admin-1", RoleId = "role-admin", CompanyId = CompanyA });
        fixture.RoleRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false, true))
            .ReturnsAsync(new Roles { RolesId = "role-admin", CompanyId = CompanyA, RoleType = (int)AuthRole.Administrator, Titles = "Admin", UserRoles = [] });
        fixture.ModuleRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new Module { ModuleId = 14, ModuleConstant = AppModule.Dashboard, ModuleName = "Dashboard" }]);
        fixture.ModulePermissionRepository.Setup(x => x.GetAll(null, false, true))
            .ReturnsAsync([new ModulePermission { ModulePermissionId = 63, ModuleId = 14, PermissionId = 5 }]);

        var disabled = new[]
        {
            new RolePermission { CompanyId = CompanyA, RoleId = "role-admin", ModulePermissionId = 63, IsAccessible = false, HasAccess = true }
        };
        fixture.RolePermissionRepository.Setup(x => x.Get(It.IsAny<Expression<Func<RolePermission, bool>>>(), null, false))
            .Returns((Expression<Func<RolePermission, bool>> predicate, object? _, bool _) => disabled.Where(predicate.Compile()).AsQueryable());

        var allowed = await fixture.Service.VerifyUserAccess(
            AppModule.Dashboard,
            [Utility.Constraints.Permission.ViewAll],
            "admin-1",
            CompanyA);

        Assert.False(allowed);
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public async Task VerifyUserAccess_CrossTenantUserAttempt_IsDenied()
    {
        var fixture = new RoleServicesFixture();
        fixture.Middleware.Setup(x => x.GetUserById("user-b"))
            .ReturnsAsync(new UserModel { UserId = "user-b", RoleId = "role-admin", CompanyId = CompanyB });

        var allowed = await fixture.Service.VerifyUserAccess(
            AppModule.Dashboard,
            [Utility.Constraints.Permission.ViewAll],
            "user-b",
            CompanyA);

        Assert.False(allowed);
    }

    [Fact]
    public async Task VerifyUserAccess_RoleBelongsToDifferentCompany_IsDenied()
    {
        var fixture = new RoleServicesFixture();
        fixture.Middleware.Setup(x => x.GetUserById("user-a"))
            .ReturnsAsync(new UserModel { UserId = "user-a", RoleId = "role-foreign", CompanyId = CompanyA });
        fixture.RoleRepository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false, true))
            .ReturnsAsync((Expression<Func<Roles, bool>> predicate, bool _, bool _) => null);

        var allowed = await fixture.Service.VerifyUserAccess(
            AppModule.Dashboard,
            [Utility.Constraints.Permission.ViewAll],
            "user-a",
            CompanyA);

        Assert.False(allowed);
    }

    [Fact]
    public async Task DashboardServices_GetAllGenderDetails_StrictlyFiltersByCompanyId()
    {
        var empUserRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var deptRepo = new Mock<IMongoDbRepository<Department>>();
        var applicantRepo = new Mock<IMongoDbRepository<Applicant>>();
        var calendarRepo = new Mock<IMongoDbRepository<CalendarEntity>>();
        var jobRepo = new Mock<IMongoDbRepository<JobTitles>>();
        var mapper = new Mock<IMapper>();
        var httpAccessor = CreateHttpContextAccessorWithCompany(CompanyA);

        var allUsers = new List<EmpUser>
        {
            new() { EmployeeId = "emp-a1", CompanyId = CompanyA, Gender = "Male", Status = true, FirstName = "A1", LastName = "User", Email = "a1@test.com" },
            new() { EmployeeId = "emp-a2", CompanyId = CompanyA, Gender = "Female", Status = true, FirstName = "A2", LastName = "User", Email = "a2@test.com" },
            new() { EmployeeId = "emp-a3", CompanyId = CompanyA, Gender = "Female", Status = true, FirstName = "A3", LastName = "User", Email = "a3@test.com" },
            new() { EmployeeId = "emp-b1", CompanyId = CompanyB, Gender = "Male", Status = true, FirstName = "B1", LastName = "User", Email = "b1@test.com" },
            new() { EmployeeId = "emp-b2", CompanyId = CompanyB, Gender = "Male", Status = true, FirstName = "B2", LastName = "User", Email = "b2@test.com" },
        };

        empUserRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync((Expression<Func<EmpUser, bool>> predicate, bool _, bool _) =>
                allUsers.Where(predicate.Compile()).ToList());

        var service = new DashboardServices(deptRepo.Object, empUserRepo.Object, httpAccessor, applicantRepo.Object, calendarRepo.Object, jobRepo.Object, mapper.Object);

        var result = await service.GetAllGenderDetails(CompanyA);

        Assert.Equal(2, result.Count);
        var male = Assert.Single(result, g => g.Gender == "Male");
        Assert.Equal(1, male.Count);
        var female = Assert.Single(result, g => g.Gender == "Female");
        Assert.Equal(2, female.Count);
    }

    [Fact]
    public async Task DashboardServices_GetAllDepartmentsDetails_StrictlyFiltersByCompanyId()
    {
        var empUserRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var deptRepo = new Mock<IMongoDbRepository<Department>>();
        var applicantRepo = new Mock<IMongoDbRepository<Applicant>>();
        var calendarRepo = new Mock<IMongoDbRepository<CalendarEntity>>();
        var jobRepo = new Mock<IMongoDbRepository<JobTitles>>();
        var mapper = new Mock<IMapper>();
        var httpAccessor = CreateHttpContextAccessorWithCompany(CompanyA);

        var depts = new List<Department>
        {
            new() { DepartmentId = "dept-a", CompanyId = CompanyA, IsDeleted = false, Titles = [new MultilingualModel { Language = "en", Label = "Engineering" }] },
            new() { DepartmentId = "dept-b", CompanyId = CompanyB, IsDeleted = false, Titles = [new MultilingualModel { Language = "en", Label = "Finance" }] },
        };

        var users = new List<EmpUser>
        {
            new() { EmployeeId = "emp-1", CompanyId = CompanyA, Department = "dept-a", Status = true, FirstName = "A", LastName = "User", Email = "a@test.com" },
            new() { EmployeeId = "emp-2", CompanyId = CompanyB, Department = "dept-b", Status = true, FirstName = "B", LastName = "User", Email = "b@test.com" },
        };

        deptRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<Department, bool>>>(), false, true))
            .ReturnsAsync((Expression<Func<Department, bool>> predicate, bool _, bool _) =>
                depts.Where(predicate.Compile()).ToList());

        empUserRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync((Expression<Func<EmpUser, bool>> predicate, bool _, bool _) =>
                users.Where(predicate.Compile()).ToList());

        var service = new DashboardServices(deptRepo.Object, empUserRepo.Object, httpAccessor, applicantRepo.Object, calendarRepo.Object, jobRepo.Object, mapper.Object);

        var result = await service.GetAllDepartmentsDetails(CompanyA);

        var singleDept = Assert.Single(result);
        Assert.Equal("dept-a", singleDept.DepartmentId);
        Assert.Equal(1, singleDept.EmployeeCount);
    }

    [Fact]
    public async Task DashboardServices_GetApplicationStatusData_StrictlyFiltersByCompanyId()
    {
        var empUserRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var deptRepo = new Mock<IMongoDbRepository<Department>>();
        var applicantRepo = new Mock<IMongoDbRepository<Applicant>>();
        var calendarRepo = new Mock<IMongoDbRepository<CalendarEntity>>();
        var jobRepo = new Mock<IMongoDbRepository<JobTitles>>();
        var mapper = new Mock<IMapper>();
        var httpAccessor = CreateHttpContextAccessorWithCompany(CompanyA);

        var applicants = new List<Applicant>
        {
            new() { ApplicantId = "app-a1", CompanyId = CompanyA, ActivityType = EnumsHelper.ActivityType.InProgress, Phone = "1234567890" },
            new() { ApplicantId = "app-a2", CompanyId = CompanyA, ActivityType = EnumsHelper.ActivityType.Shortlisted, Phone = "1234567891" },
            new() { ApplicantId = "app-b1", CompanyId = CompanyB, ActivityType = EnumsHelper.ActivityType.InProgress, Phone = "1234567892" },
            new() { ApplicantId = "app-b2", CompanyId = CompanyB, ActivityType = EnumsHelper.ActivityType.Rejected, Phone = "1234567893" },
        };

        applicantRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<Applicant, bool>>>(), false, true))
            .ReturnsAsync((Expression<Func<Applicant, bool>> predicate, bool _, bool _) =>
                applicants.Where(predicate.Compile()).ToList());

        var service = new DashboardServices(deptRepo.Object, empUserRepo.Object, httpAccessor, applicantRepo.Object, calendarRepo.Object, jobRepo.Object, mapper.Object);

        var response = await service.GetApplicationStatusData(null);

        Assert.NotNull(response.MethodResult);
        Assert.Equal(2, response.MethodResult.TotalApplications);
    }

    [Fact]
    public async Task DashboardServices_GetUpComingHolidayAndEvents_FiltersToTwoMonthWindow_AndLoadsAllWithoutFiveCap()
    {
        var empUserRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var deptRepo = new Mock<IMongoDbRepository<Department>>();
        var applicantRepo = new Mock<IMongoDbRepository<Applicant>>();
        var calendarRepo = new Mock<IMongoDbRepository<CalendarEntity>>();
        var jobRepo = new Mock<IMongoDbRepository<JobTitles>>();
        var mapper = new Mock<IMapper>();
        var httpAccessor = CreateHttpContextAccessorWithCompany(CompanyA);

        var today = Codeji.CMS.Utility.Helpers.IndiaTime.Today;
        // 7 holidays in the upcoming 2-month window
        var holidays = new List<CalendarEntity>();
        for (int i = 1; i <= 7; i++)
        {
            holidays.Add(new CalendarEntity
            {
                Id = $"hol-{i}",
                CompanyId = CompanyA,
                Name = $"Holiday {i}",
                Date = today.AddDays(i * 5), // all within 35 days (<= 2 months)
                Type = EnumsHelper.CalendarItem.Holiday,
                Recurring = false,
            });
        }
        // 1 holiday 9 months away
        holidays.Add(new CalendarEntity
        {
            Id = "hol-future",
            CompanyId = CompanyA,
            Name = "Future Holiday",
            Date = today.AddMonths(9),
            Type = EnumsHelper.CalendarItem.Holiday,
            Recurring = false,
        });
        // 1 event 9 months away
        holidays.Add(new CalendarEntity
        {
            Id = "evt-future",
            CompanyId = CompanyA,
            Name = "Future Event",
            Date = today.AddMonths(9),
            Type = EnumsHelper.CalendarItem.Event,
            Recurring = false,
        });

        calendarRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<CalendarEntity, bool>>>(), false, true))
            .ReturnsAsync((Expression<Func<CalendarEntity, bool>> predicate, bool _, bool _) =>
                holidays.Where(predicate.Compile()).ToList());

        var service = new DashboardServices(deptRepo.Object, empUserRepo.Object, httpAccessor, applicantRepo.Object, calendarRepo.Object, jobRepo.Object, mapper.Object);
        var result = await service.GetUpComingHolidayAndEvents();

        Assert.NotNull(result.MethodResult);
        // All 7 holidays within the 2-month window should be loaded at once (no 5-item cap)
        Assert.Equal(7, result.MethodResult.Holiday.Count);
        // Future items outside the 2-month window should be excluded
        Assert.DoesNotContain(result.MethodResult.Holiday, h => h.Name == "Future Holiday");
        Assert.Empty(result.MethodResult.Event);
    }

    [Fact]
    public async Task DashboardServices_GetUpComingCelebrations_FiltersToTwoMonthWindow_AndLoadsAllWithoutFiveCap()
    {
        var empUserRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var deptRepo = new Mock<IMongoDbRepository<Department>>();
        var applicantRepo = new Mock<IMongoDbRepository<Applicant>>();
        var calendarRepo = new Mock<IMongoDbRepository<CalendarEntity>>();
        var jobRepo = new Mock<IMongoDbRepository<JobTitles>>();
        var mapper = new Mock<IMapper>();
        var httpAccessor = CreateHttpContextAccessorWithCompany(CompanyA);

        var today = Codeji.CMS.Utility.Helpers.IndiaTime.Today;
        var employees = new List<EmpUser>();
        // 7 employees with birthdays within the 2-month window
        for (int i = 1; i <= 7; i++)
        {
            var targetBday = today.AddDays(i * 5); // within 35 days
            employees.Add(new EmpUser
            {
                EmployeeId = $"emp-{i}",
                CompanyId = CompanyA,
                FirstName = $"Emp{i}",
                LastName = "Test",
                Email = $"emp{i}@example.com",
                Status = true,
                DateOfBirth = new DateTime(1995, targetBday.Month, targetBday.Day).ToString("yyyy-MM-dd"),
                DateOfJoining = new DateTime(2020, 1, 1).ToString("yyyy-MM-dd"),
                JobRole = "job-dev",
            });
        }
        // 2 employees with birthdays 9 months away
        var futureDate = today.AddMonths(9);
        employees.Add(new EmpUser
        {
            EmployeeId = "emp-future-1",
            CompanyId = CompanyA,
            FirstName = "Future1",
            LastName = "Test",
            Email = "future1@example.com",
            Status = true,
            DateOfBirth = new DateTime(1990, futureDate.Month, futureDate.Day).ToString("yyyy-MM-dd"),
            DateOfJoining = new DateTime(2020, 1, 1).ToString("yyyy-MM-dd"),
            JobRole = "job-dev",
        });

        empUserRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync((Expression<Func<EmpUser, bool>> predicate, bool _, bool _) =>
                employees.Where(predicate.Compile()).ToList());

        jobRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), false, true))
            .ReturnsAsync(new List<JobTitles>
            {
                new() { JobTitleId = "job-dev", CompanyId = CompanyA, Titles = [new() { Language = "en", Label = "Developer" }] }
            });

        var service = new DashboardServices(deptRepo.Object, empUserRepo.Object, httpAccessor, applicantRepo.Object, calendarRepo.Object, jobRepo.Object, mapper.Object);
        var result = await service.GetUpComingCelebrations();

        Assert.NotNull(result.MethodResult);
        // All 7 birthdays within the 2-month window should be loaded at once (no 5-item cap)
        Assert.Equal(7, result.MethodResult.Birthday.Count);
        // Employees with birthdays 9 months away should be excluded
        Assert.DoesNotContain(result.MethodResult.Birthday, b => b.EmployeeName.Contains("Future1"));
    }

    #endregion

    private static IHttpContextAccessor CreateHttpContextAccessorWithCompany(string companyId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(EnumsHelper.ClaimTypesEnum.company_id.ToString(), companyId),
            new Claim(EnumsHelper.ClaimTypesEnum.user_id.ToString(), "test-user"),
            new Claim(EnumsHelper.ClaimTypesEnum.role_id.ToString(), "test-role")
        ], "TestAuth"));

        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(x => x.HttpContext).Returns(context);
        return mockAccessor.Object;
    }

    private sealed class RoleServicesFixture
    {
        public Mock<IMongoDbRepository<Roles>> RoleRepository { get; } = new();
        public Mock<IMongoDbRepository<RolePermission>> RolePermissionRepository { get; } = new();
        public Mock<IMongoDbRepository<ModulePermission>> ModulePermissionRepository { get; } = new();
        public Mock<IMongoDbRepository<Module>> ModuleRepository { get; } = new();
        public Mock<IMongoDbRepository<Permission>> PermissionRepository { get; } = new();
        public Mock<IMiddlewareService> Middleware { get; } = new();
        public RoleServices Service { get; }

        public RoleServicesFixture()
        {
            Service = new RoleServices(
                RoleRepository.Object,
                RolePermissionRepository.Object,
                ModulePermissionRepository.Object,
                ModuleRepository.Object,
                PermissionRepository.Object,
                Mock.Of<IMongoDbRepository<EmpUser>>(),
                Mock.Of<IMapper>(),
                Middleware.Object);
        }
    }
}
