using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Companies;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Constraints;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;
using Module = Codeji.CMS.Repository.Entities.RolePermissions.Module;

namespace Codeji.CMS.Services.Tests;

public sealed class ModuleLifecycleEndToEndTests
{
    private const string TestCompanyId = "comp-lifecycle-test";
    private const string AdminRoleId = "role-admin-1";

    [Fact]
    public async Task GetAllModulesDetails_CascadesSuiteInaccessibility_ToAllChildModules()
    {
        var (companyService, _) = BuildCompanyMasterServiceWithModules(jobsAccessible: false, payrollAccessible: false);

        var modules = await companyService.GetAllModulesDetails(TestCompanyId);

        var jobsModule = modules.FirstOrDefault(m => m.ModuleConstant == AppModule.Jobs);
        var applicationsModule = modules.FirstOrDefault(m => m.ModuleConstant == AppModule.Applications);
        var processLogModule = modules.FirstOrDefault(m => m.ModuleConstant == AppModule.ProcessLog);
        var careerProfileModule = modules.FirstOrDefault(m => m.ModuleConstant == AppModule.CareerProfile);

        var payrollModule = modules.FirstOrDefault(m => m.ModuleConstant == AppModule.PayRoll);
        var payrollSettingsModule = modules.FirstOrDefault(m => m.ModuleConstant == AppModule.PayrollSettings);
        var leaveModule = modules.FirstOrDefault(m => m.ModuleConstant == AppModule.LeaveManagement);
        var attendanceModule = modules.FirstOrDefault(m => m.ModuleConstant == AppModule.Attendance);

        Assert.NotNull(jobsModule);
        Assert.False(jobsModule.IsAccessible, "Jobs should be inaccessible");
        Assert.NotNull(applicationsModule);
        Assert.False(applicationsModule.IsAccessible, "Applications should cascade to inaccessible");
        Assert.NotNull(processLogModule);
        Assert.False(processLogModule.IsAccessible, "ProcessLog should cascade to inaccessible");
        Assert.NotNull(careerProfileModule);
        Assert.False(careerProfileModule.IsAccessible, "CareerProfile should cascade to inaccessible");

        Assert.NotNull(payrollModule);
        Assert.False(payrollModule.IsAccessible, "PayRoll should be inaccessible");
        Assert.NotNull(payrollSettingsModule);
        Assert.False(payrollSettingsModule.IsAccessible, "PayrollSettings should cascade to inaccessible");
        Assert.NotNull(leaveModule);
        Assert.False(leaveModule.IsAccessible, "LeaveManagement should cascade to inaccessible");
        Assert.NotNull(attendanceModule);
        Assert.False(attendanceModule.IsAccessible, "Attendance should cascade to inaccessible");
    }

    [Fact]
    public async Task VerifyUserAccess_RejectsAdministrator_WhenModuleIsDisabledForCompany()
    {
        var roleService = BuildRoleServiceWithAccessibility(
            isJobsAccessible: false,
            isPayrollAccessible: false,
            isOffboardingAccessible: false);

        var canAccessJobs = await roleService.VerifyUserAccess(AppModule.Jobs, [PermissionConst.View], "user-1", TestCompanyId);
        var canAccessCareerProfile = await roleService.VerifyUserAccess(AppModule.CareerProfile, [PermissionConst.View], "user-1", TestCompanyId);
        var canAccessPayroll = await roleService.VerifyUserAccess(AppModule.PayRoll, [PermissionConst.View], "user-1", TestCompanyId);
        var canAccessPayrollSettings = await roleService.VerifyUserAccess(AppModule.PayrollSettings, [PermissionConst.View], "user-1", TestCompanyId);
        var canAccessOffboarding = await roleService.VerifyUserAccess(AppModule.Offboarding, [PermissionConst.View], "user-1", TestCompanyId);

        Assert.False(canAccessJobs, "Disabled Jobs must be forbidden even to admin");
        Assert.False(canAccessCareerProfile, "Career profile with disabled Jobs must be forbidden");
        Assert.False(canAccessPayroll, "Disabled Payroll must be forbidden even to admin");
        Assert.False(canAccessPayrollSettings, "Payroll settings with disabled Payroll must be forbidden");
        Assert.False(canAccessOffboarding, "Disabled Offboarding must be forbidden even to admin");
    }

    [Fact]
    public async Task GetRolePermissionOfuser_FiltersOutAllPermissions_ForDisabledSuites()
    {
        var roleService = BuildRoleServiceWithAccessibility(
            isJobsAccessible: false,
            isPayrollAccessible: false,
            isOffboardingAccessible: true);

        var permissions = await roleService.GetRolePermissionOfuser(AdminRoleId, TestCompanyId);

        Assert.DoesNotContain(permissions, p => p.StartsWith("Jobs."));
        Assert.DoesNotContain(permissions, p => p.StartsWith("Applications."));
        Assert.DoesNotContain(permissions, p => p.StartsWith("Process_Log."));
        Assert.DoesNotContain(permissions, p => p.StartsWith("Career_Profile."));

        Assert.DoesNotContain(permissions, p => p.StartsWith("PayRoll."));
        Assert.DoesNotContain(permissions, p => p.StartsWith("Payroll_Settings."));
        Assert.DoesNotContain(permissions, p => p.StartsWith("Attendance."));
        Assert.DoesNotContain(permissions, p => p.StartsWith("Leave_Management."));

        Assert.Contains(permissions, p => p.StartsWith("Employees."));
        Assert.Contains(permissions, p => p.StartsWith("Calendar."));
    }

    private static (CompanyMasterService Service, Mock<IMongoDbRepository<RolePermission>> RolePermRepo) BuildCompanyMasterServiceWithModules(bool jobsAccessible, bool payrollAccessible)
    {
        var modules = new List<Module>
        {
            new() { _id = "m-1", ModuleId = 1, ModuleConstant = AppModule.Employees, ModuleName = "Employees", SortOrder = 1 },
            new() { _id = "m-2", ModuleId = 2, ModuleConstant = AppModule.Calendar, ModuleName = "Calendar", SortOrder = 2 },
            new() { _id = "m-3", ModuleId = 3, ModuleConstant = AppModule.NoticeBoard, ModuleName = "Notice Board", SortOrder = 3 },
            new() { _id = "m-4", ModuleId = 4, ModuleConstant = AppModule.Policy, ModuleName = "Policy", SortOrder = 4 },
            new() { _id = "m-5", ModuleId = 5, ModuleConstant = AppModule.WorkFromHome, ModuleName = "Work From Home", SortOrder = 5 },
            new() { _id = "m-6", ModuleId = 6, ModuleConstant = AppModule.Offboarding, ModuleName = "Offboarding", SortOrder = 6 },
            new() { _id = "m-7", ModuleId = 7, ModuleConstant = AppModule.Jobs, ModuleName = "Jobs", SortOrder = 7 },
            new() { _id = "m-8", ModuleId = 8, ModuleConstant = AppModule.Applications, ModuleName = "Applications", SortOrder = 8 },
            new() { _id = "m-9", ModuleId = 9, ModuleConstant = AppModule.ProcessLog, ModuleName = "Process Log", SortOrder = 9 },
            new() { _id = "m-10", ModuleId = 10, ModuleConstant = AppModule.CareerProfile, ModuleName = "Career Profile", SortOrder = 10 },
            new() { _id = "m-11", ModuleId = 11, ModuleConstant = AppModule.PayRoll, ModuleName = "PayRoll", SortOrder = 11 },
            new() { _id = "m-12", ModuleId = 12, ModuleConstant = AppModule.PayrollSettings, ModuleName = "Payroll Settings", SortOrder = 12 },
            new() { _id = "m-13", ModuleId = 13, ModuleConstant = AppModule.LeaveManagement, ModuleName = "Leave Management", SortOrder = 13 },
            new() { _id = "m-14", ModuleId = 14, ModuleConstant = AppModule.Attendance, ModuleName = "Attendance", SortOrder = 14 },
        };

        var modulePerms = modules.Select(m => new ModulePermission
        {
            ModulePermissionId = m.ModuleId * 10,
            ModuleId = m.ModuleId,
            PermissionId = 1,
            HasModuleAccess = true
        }).ToList();

        var rolePerms = new List<RolePermission>();
        foreach (var mp in modulePerms)
        {
            var isAccessible = true;
            if (mp.ModuleId == 7 && !jobsAccessible) isAccessible = false;
            if (mp.ModuleId == 11 && !payrollAccessible) isAccessible = false;

            rolePerms.Add(new RolePermission
            {
                CompanyId = TestCompanyId,
                RoleId = AdminRoleId,
                ModulePermissionId = mp.ModulePermissionId,
                IsAccessible = isAccessible,
                HasAccess = true
            });
        }

        var moduleRepo = new Mock<IMongoDbRepository<Module>>();
        moduleRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<Module, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(modules);

        var modulePermRepo = new Mock<IMongoDbRepository<ModulePermission>>();
        modulePermRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<ModulePermission, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(modulePerms);

        var rolePermRepo = new Mock<IMongoDbRepository<RolePermission>>();
        rolePermRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<RolePermission, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(rolePerms);

        var roleService = new Mock<IRoleService>();
        var httpContext = new Mock<IHttpContextAccessor>();

        var service = new CompanyMasterService(
            roleService.Object,
            Mock.Of<IMongoDbRepository<CustomAttribute>>(),
            Mock.Of<IMongoDbRepository<CustomAttributeValue>>(),
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMapper>(),
            moduleRepo.Object,
            modulePermRepo.Object,
            Mock.Of<IMongoDbRepository<PermissionEntity>>(),
            rolePermRepo.Object,
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            Mock.Of<IMongoDbRepository<EmpUser>>(),
            httpContext.Object);

        return (service, rolePermRepo);
    }

    private static IRoleService BuildRoleServiceWithAccessibility(
        bool isJobsAccessible,
        bool isPayrollAccessible,
        bool isOffboardingAccessible)
    {
        var modules = new List<Module>
        {
            new() { ModuleId = 1, ModuleConstant = AppModule.Employees, ModuleName = "Employees" },
            new() { ModuleId = 2, ModuleConstant = AppModule.Calendar, ModuleName = "Calendar" },
            new() { ModuleId = 3, ModuleConstant = AppModule.NoticeBoard, ModuleName = "Notice Board" },
            new() { ModuleId = 4, ModuleConstant = AppModule.Policy, ModuleName = "Policy" },
            new() { ModuleId = 5, ModuleConstant = AppModule.WorkFromHome, ModuleName = "Work From Home" },
            new() { ModuleId = 6, ModuleConstant = AppModule.Offboarding, ModuleName = "Offboarding" },
            new() { ModuleId = 7, ModuleConstant = AppModule.Jobs, ModuleName = "Jobs" },
            new() { ModuleId = 8, ModuleConstant = AppModule.Applications, ModuleName = "Applications" },
            new() { ModuleId = 9, ModuleConstant = AppModule.ProcessLog, ModuleName = "Process Log" },
            new() { ModuleId = 10, ModuleConstant = AppModule.CareerProfile, ModuleName = "Career Profile" },
            new() { ModuleId = 11, ModuleConstant = AppModule.PayRoll, ModuleName = "PayRoll" },
            new() { ModuleId = 12, ModuleConstant = AppModule.PayrollSettings, ModuleName = "Payroll Settings" },
            new() { ModuleId = 13, ModuleConstant = AppModule.LeaveManagement, ModuleName = "Leave Management" },
            new() { ModuleId = 14, ModuleConstant = AppModule.Attendance, ModuleName = "Attendance" },
        };

        var permissions = new List<PermissionEntity>
        {
            new() { PermissionId = 1, PermissionName = "View", PermissionConstant = "View" },
            new() { PermissionId = 2, PermissionName = "Edit", PermissionConstant = "Edit" }
        };

        var modulePerms = new List<ModulePermission>();
        foreach (var m in modules)
        {
            modulePerms.Add(new ModulePermission { ModulePermissionId = m.ModuleId * 10 + 1, ModuleId = m.ModuleId, PermissionId = 1 });
            modulePerms.Add(new ModulePermission { ModulePermissionId = m.ModuleId * 10 + 2, ModuleId = m.ModuleId, PermissionId = 2 });
        }

        var rolePerms = new List<RolePermission>();
        foreach (var mp in modulePerms)
        {
            bool isAccessible = true;
            if (mp.ModuleId is 7 or 8 or 9 or 10 && !isJobsAccessible) isAccessible = false;
            if (mp.ModuleId is 11 or 12 or 13 or 14 && !isPayrollAccessible) isAccessible = false;
            if (mp.ModuleId == 6 && !isOffboardingAccessible) isAccessible = false;

            rolePerms.Add(new RolePermission
            {
                CompanyId = TestCompanyId,
                RoleId = AdminRoleId,
                ModulePermissionId = mp.ModulePermissionId,
                IsAccessible = isAccessible,
                HasAccess = true
            });
        }

        var rolesList = new List<Roles>
        {
            new() { RolesId = AdminRoleId, RoleType = 1, Titles = "Administrator", CompanyId = TestCompanyId }
        };

        var usersList = new List<EmpUser>
        {
            new() { UserId = "user-1", RoleId = AdminRoleId, CompanyId = TestCompanyId, FirstName = "Admin", LastName = "User", Email = "admin@example.com" }
        };

        var moduleRepo = new Mock<IMongoDbRepository<Module>>();
        moduleRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<Module, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(modules);

        var permRepo = new Mock<IMongoDbRepository<PermissionEntity>>();
        permRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<PermissionEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(permissions);

        var modulePermRepo = new Mock<IMongoDbRepository<ModulePermission>>();
        modulePermRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<ModulePermission, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(modulePerms);

        var rolePermRepo = new Mock<IMongoDbRepository<RolePermission>>();
        rolePermRepo.Setup(x => x.Get(It.IsAny<Expression<Func<RolePermission, bool>>>(), null, false))
            .Returns((Expression<Func<RolePermission, bool>> predicate, object? _, bool _) =>
                rolePerms.Where(predicate.Compile()).AsQueryable());

        var rolesRepo = new Mock<IMongoDbRepository<Roles>>();
        rolesRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Roles, bool>>>(), false))
            .ReturnsAsync(rolesList.First());

        var userRepo = new Mock<IMongoDbRepository<EmpUser>>();
        userRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false))
            .ReturnsAsync(usersList.First());

        return new RoleServices(
            rolesRepo.Object,
            rolePermRepo.Object,
            modulePermRepo.Object,
            moduleRepo.Object,
            permRepo.Object,
            userRepo.Object,
            Mock.Of<IMapper>(),
            Mock.Of<IMiddlewareService>());
    }
}
