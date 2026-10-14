using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Company.JobTitle;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Companies;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Helpers;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class JobTitleDepartmentDecouplingTests
{
    [Fact]
    public void DefaultCompanySeeds_BuildJobTitles_AlwaysSetsDepartmentIdToNull()
    {
        var dummyDeptIds = new Dictionary<string, string>
        {
            ["Engineering"] = "dept-1",
            ["Human Resources"] = "dept-2",
            ["Finance"] = "dept-3"
        };

        var jobTitles = DefaultCompanySeeds.BuildJobTitles("company-test", ["en"], dummyDeptIds, "admin-1");

        Assert.NotEmpty(jobTitles);
        Assert.All(jobTitles, jt =>
        {
            Assert.Null(jt.DepartmentId);
            Assert.Equal("company-test", jt.CompanyId);
            Assert.True(jt.IsActive);
            Assert.False(jt.IsDeleted);
        });
    }

    [Fact]
    public async Task CompanyMasterService_AddUpdateJobTitle_SucceedsWithoutDepartmentId()
    {
        var fixture = CreateMasterFixture();

        var request = new List<JobTitleRequestDto>
        {
            new()
            {
                JobTitleId = null,
                DepartmentId = null,
                IsActive = true,
                Titles = [new MultilingualModel { Language = "en", Label = "Principal Architect" }]
            }
        };

        var result = await fixture.Service.AddUpdateJobTitle(request, "user-1");

        Assert.True(result.Success);
        fixture.DepartmentRepository.Verify(x => x.Exist(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<bool>()), Times.Never);
        fixture.JobTitlesRepository.Verify(x => x.AddOne(It.Is<JobTitles>(jt =>
            jt.DepartmentId == null && jt.Titles.Any(t => t.Label == "Principal Architect"))), Times.Once);
    }

    [Fact]
    public async Task CompanyMasterService_GetJobTitles_WithoutDepartment_ReturnsAllActiveJobTitles()
    {
        var fixture = CreateMasterFixture();
        var titles = new List<JobTitles>
        {
            new() { JobTitleId = "jt-1", CompanyId = "company-1", DepartmentId = null, IsActive = true, Titles = [new MultilingualModel { Language = "en", Label = "Role 1" }] },
            new() { JobTitleId = "jt-2", CompanyId = "company-1", DepartmentId = "dept-other", IsActive = true, Titles = [new MultilingualModel { Language = "en", Label = "Role 2" }] }
        };
        fixture.JobTitlesRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(titles);

        var result = await fixture.Service.GetJobTitles(true, null);

        Assert.True(result.Success);
        Assert.Equal(2, result.TotalRecords);
        Assert.Contains(result.MethodResults, r => r.JobTitleId == "jt-1");
        Assert.Contains(result.MethodResults, r => r.JobTitleId == "jt-2");
    }

    [Fact]
    public async Task EmployeeService_InviteNewEmployee_SucceedsWhenJobTitleHasNoDepartment()
    {
        var fixture = CreateEmployeeFixture();

        var inviteDto = new InviteEmployeeDto
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada.lovelace@example.com",
            EmployeeId = "EMP-ADA",
            DepartmentId = "dept-1",
            JobRoleId = "job-no-dept"
        };

        var result = await fixture.Service.InviteNewEmployee(inviteDto, "admin-user-id");

        Assert.True(result.Success);
        fixture.EmployeeRepository.Verify(x => x.AddOne(It.Is<EmpUser>(u =>
            u.Department == "dept-1" && u.JobRole == "job-no-dept")), Times.Once);
    }

    [Fact]
    public async Task EmployeeService_InviteNewEmployee_SucceedsWhenJobTitleDepartmentDiffersFromEmployeeDepartment()
    {
        var fixture = CreateEmployeeFixture();

        var inviteDto = new InviteEmployeeDto
        {
            FirstName = "Grace",
            LastName = "Hopper",
            Email = "grace.hopper@example.com",
            EmployeeId = "EMP-GRACE",
            DepartmentId = "dept-engineering",
            JobRoleId = "job-different-dept"
        };

        var result = await fixture.Service.InviteNewEmployee(inviteDto, "admin-user-id");

        Assert.True(result.Success);
        fixture.EmployeeRepository.Verify(x => x.AddOne(It.Is<EmpUser>(u =>
            u.Department == "dept-engineering" && u.JobRole == "job-different-dept")), Times.Once);
    }

    [Fact]
    public async Task EmployeeService_BulkImport_CreatesNewJobTitleWithNullDepartment()
    {
        var fixture = CreateEmployeeFixture();
        var request = new BulkImportEmployeesRequestDto
        {
            Employees =
            [
                new BulkImportEmployeeItemDto
                {
                    EmpId = "EMP099",
                    FirstName = "Test",
                    LastName = "Decoupled",
                    Email = "test.decoupled@example.com",
                    Role = "Employee",
                    Department = "Engineering",
                    JobRole = "Brand New Decoupled Role",
                    DateOfJoining = "2026-10-01"
                }
            ]
        };

        var result = await fixture.Service.BulkImportEmployees(request, "admin-user-id");

        Assert.True(result.Success);
        Assert.Equal(1, result.MethodResult.CreatedCount);
        fixture.JobTitlesRepository.Verify(r => r.AddOne(It.Is<JobTitles>(j =>
            j.DepartmentId == null &&
            j.Titles.Any(t => t.Label == "Brand New Decoupled Role"))), Times.Once);
    }

    private static MasterFixture CreateMasterFixture()
    {
        var fixture = new MasterFixture();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("CompanyId", "company-1"), new Claim("UserId", "user-1")]));
        fixture.HttpContextAccessor.SetupGet(x => x.HttpContext).Returns(context);

        fixture.JobTitlesRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([]);
        fixture.JobTitlesRepository
            .Setup(x => x.AddOne(It.IsAny<JobTitles>()))
            .ReturnsAsync(new Result { Success = true });
        fixture.JobTitlesRepository
            .Setup(x => x.AddMany(It.IsAny<List<JobTitles>>()))
            .ReturnsAsync(new Result { Success = true });

        fixture.Service = new CompanyMasterService(
            fixture.RoleService.Object,
            fixture.CustomAttributeRepository.Object,
            fixture.CustomAttributeValueRepository.Object,
            fixture.DepartmentRepository.Object,
            fixture.Mapper.Object,
            fixture.ModuleRepository.Object,
            fixture.ModulePermissionRepository.Object,
            fixture.PermissionRepository.Object,
            fixture.RolePermissionRepository.Object,
            fixture.JobTitlesRepository.Object,
            fixture.EmployeeRepository.Object,
            fixture.HttpContextAccessor.Object);

        return fixture;
    }

    private static EmployeeFixture CreateEmployeeFixture()
    {
        var fixture = new EmployeeFixture();
        ConfigManager.AppSettings = new AppSettings { AppUrl = "http://localhost/", APIUrl = "http://localhost/api", AppVersion = "test", IsForDebug = true };

        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("CompanyId", "company-1"), new Claim("UserId", "admin-user-id")]));
        fixture.HttpContextAccessor.SetupGet(x => x.HttpContext).Returns(context);

        fixture.EmployeeRepository
            .Setup(x => x.Exist(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(false);
        fixture.EmployeeRepository
            .Setup(x => x.AddOne(It.IsAny<EmpUser>()))
            .ReturnsAsync(new Result { Success = true });

        fixture.NotificationPreferenceRepository
            .Setup(x => x.AddOne(It.IsAny<NotificationPreference>()))
            .ReturnsAsync(new Result { Success = true });
        fixture.MiddlewareService
            .Setup(x => x.GetDefaultNotificationPreferences())
            .Returns([]);
        fixture.MiddlewareService
            .Setup(x => x.GetUserById(It.IsAny<string>()))
            .ReturnsAsync(new UserModel { UserId = "admin-user-id", CompanyId = "company-1", FirstName = "Admin", LastName = "User" });

        fixture.RolesRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Roles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([
                new Roles { RolesId = "role-1", CompanyId = "company-1", Titles = "Employee", IsDeleted = false, UserRoles = [] }
            ]);
        fixture.RolesRepository
            .Setup(x => x.Exist(It.IsAny<Expression<Func<Roles, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        fixture.DepartmentRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([
                new Department { DepartmentId = "dept-1", CompanyId = "company-1", IsActive = true, Titles = [new MultilingualModel { Language = "en", Label = "Engineering" }] },
                new Department { DepartmentId = "dept-engineering", CompanyId = "company-1", IsActive = true, Titles = [new MultilingualModel { Language = "en", Label = "Engineering" }] }
            ]);
        fixture.DepartmentRepository
            .Setup(x => x.Exist(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        var existingTitles = new List<JobTitles>
        {
            new()
            {
                JobTitleId = "job-no-dept",
                CompanyId = "company-1",
                DepartmentId = null,
                IsActive = true,
                Titles = [new MultilingualModel { Language = "en", Label = "No Dept Role" }]
            },
            new()
            {
                JobTitleId = "job-different-dept",
                CompanyId = "company-1",
                DepartmentId = "dept-marketing",
                IsActive = true,
                Titles = [new MultilingualModel { Language = "en", Label = "Cross Dept Role" }]
            }
        };

        fixture.JobTitlesRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<JobTitles, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(existingTitles);
        fixture.JobTitlesRepository
            .Setup(x => x.FirstOrDefault(It.Is<Expression<Func<JobTitles, bool>>>(e => e.Compile()(existingTitles[0])), It.IsAny<bool>()))
            .ReturnsAsync(existingTitles[0]);
        fixture.JobTitlesRepository
            .Setup(x => x.FirstOrDefault(It.Is<Expression<Func<JobTitles, bool>>>(e => e.Compile()(existingTitles[1])), It.IsAny<bool>()))
            .ReturnsAsync(existingTitles[1]);
        fixture.JobTitlesRepository
            .Setup(x => x.AddOne(It.IsAny<JobTitles>()))
            .ReturnsAsync(new Result { Success = true });

        fixture.CompanyRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Company { CompanyId = "company-1", CompanyName = "Codeji", DefaultLanguage = "en", ApplicationLanguage = [], Status = true, AutoGenerateEmployeeId = false });

        fixture.MailTemplateRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<MailTemplate, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new MailTemplate { subject = "Welcome", body = "Hello {{EmployeeName}}" });

        fixture.UserSecurityTokenRepository
            .Setup(x => x.AddOne(It.IsAny<UserSecurityToken>()))
            .ReturnsAsync(new Result { Success = true });

        fixture.Service = new EmployeeService(
            fixture.EducationRepository.Object,
            fixture.Mapper.Object,
            fixture.CertificationRepository.Object,
            fixture.SummaryRepository.Object,
            fixture.EmployeeRepository.Object,
            fixture.EmployeeIdSequenceRepository.Object,
            fixture.RolesRepository.Object,
            fixture.RoleService.Object,
            fixture.EmployeeSkillsRepository.Object,
            fixture.CompanyRepository.Object,
            fixture.MailTemplateRepository.Object,
            fixture.PriorityTaskQueue.Object,
            fixture.MiddlewareService.Object,
            fixture.HttpContextAccessor.Object,
            fixture.DepartmentRepository.Object,
            fixture.SkillsRepository.Object,
            fixture.WorkHistoryRepository.Object,
            fixture.UserNotificationRepository.Object,
            fixture.NotificationsRepository.Object,
            fixture.JobTitlesRepository.Object,
            fixture.CustomAttributeRepository.Object,
            fixture.CustomAttributeValueRepository.Object,
            fixture.NotificationService.Object,
            fixture.NotificationPreferenceRepository.Object,
            fixture.UserSecurityTokenRepository.Object,
            fixture.OfficeScheduleRepository.Object,
            fixture.EmployeeScheduleAssignmentRepository.Object,
            fixture.Logger.Object,
            employeeExitRepository: fixture.EmployeeExitRepository.Object,
            employeeExitPolicyRepository: fixture.EmployeeExitPolicyRepository.Object);

        return fixture;
    }

    private sealed class MasterFixture
    {
        public CompanyMasterService Service { get; set; } = null!;
        public Mock<IRoleService> RoleService { get; } = new();
        public Mock<IMongoDbRepository<CustomAttribute>> CustomAttributeRepository { get; } = new();
        public Mock<IMongoDbRepository<CustomAttributeValue>> CustomAttributeValueRepository { get; } = new();
        public Mock<IMongoDbRepository<Department>> DepartmentRepository { get; } = new();
        public Mock<IMapper> Mapper { get; } = new();
        public Mock<IMongoDbRepository<Module>> ModuleRepository { get; } = new();
        public Mock<IMongoDbRepository<ModulePermission>> ModulePermissionRepository { get; } = new();
        public Mock<IMongoDbRepository<Permission>> PermissionRepository { get; } = new();
        public Mock<IMongoDbRepository<RolePermission>> RolePermissionRepository { get; } = new();
        public Mock<IMongoDbRepository<JobTitles>> JobTitlesRepository { get; } = new();
        public Mock<IMongoDbRepository<EmpUser>> EmployeeRepository { get; } = new();
        public Mock<IHttpContextAccessor> HttpContextAccessor { get; } = new();
    }

    private sealed class EmployeeFixture
    {
        public EmployeeService Service { get; set; } = null!;
        public Mock<IMongoDbRepository<EmpEducationDetails>> EducationRepository { get; } = new();
        public Mock<IMapper> Mapper { get; } = new();
        public Mock<IMongoDbRepository<EmpCertificationDetails>> CertificationRepository { get; } = new();
        public Mock<IMongoDbRepository<EmpSummary>> SummaryRepository { get; } = new();
        public Mock<IMongoDbRepository<EmpUser>> EmployeeRepository { get; } = new();
        public Mock<IMongoDbRepository<EmployeeIdSequence>> EmployeeIdSequenceRepository { get; } = new();
        public Mock<IMongoDbRepository<Roles>> RolesRepository { get; } = new();
        public Mock<IRoleService> RoleService { get; } = new();
        public Mock<IMongoDbRepository<EmpSkills>> EmployeeSkillsRepository { get; } = new();
        public Mock<IMongoDbRepository<Company>> CompanyRepository { get; } = new();
        public Mock<IMongoDbRepository<MailTemplate>> MailTemplateRepository { get; } = new();
        public Mock<IPriorityTaskQueue> PriorityTaskQueue { get; } = new();
        public Mock<IMiddlewareService> MiddlewareService { get; } = new();
        public Mock<IHttpContextAccessor> HttpContextAccessor { get; } = new();
        public Mock<IMongoDbRepository<Department>> DepartmentRepository { get; } = new();
        public Mock<IMongoDbRepository<Skills>> SkillsRepository { get; } = new();
        public Mock<IMongoDbRepository<EmpWorkHistory>> WorkHistoryRepository { get; } = new();
        public Mock<IMongoDbRepository<UserNotifications>> UserNotificationsRepository { get; } = new();
        public Mock<IMongoDbRepository<UserNotifications>> UserNotificationRepository { get; } = new();
        public Mock<IMongoDbRepository<Notifications>> NotificationsRepository { get; } = new();
        public Mock<IMongoDbRepository<JobTitles>> JobTitlesRepository { get; } = new();
        public Mock<IMongoDbRepository<CustomAttribute>> CustomAttributeRepository { get; } = new();
        public Mock<IMongoDbRepository<CustomAttributeValue>> CustomAttributeValueRepository { get; } = new();
        public Mock<INotificationService> NotificationService { get; } = new();
        public Mock<IMongoDbRepository<NotificationPreference>> NotificationPreferenceRepository { get; } = new();
        public Mock<IMongoDbRepository<UserSecurityToken>> UserSecurityTokenRepository { get; } = new();
        public Mock<IMongoDbRepository<CompanyOfficeSchedule>> OfficeScheduleRepository { get; } = new();
        public Mock<IMongoDbRepository<EmployeeScheduleAssignment>> EmployeeScheduleAssignmentRepository { get; } = new();
        public Mock<IMongoDbRepository<EmployeeExit>> EmployeeExitRepository { get; } = new();
        public Mock<IMongoDbRepository<EmployeeExitPolicy>> EmployeeExitPolicyRepository { get; } = new();
        public Mock<ILogger<EmployeeService>> Logger { get; } = new();
    }
}
