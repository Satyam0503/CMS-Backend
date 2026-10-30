using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.Constraints;
using RoleEntity = Codeji.CMS.Repository.Entities.RolePermissions.Roles;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Tests;

public sealed class CompanyRegistrationProvisioningTests
{
    [Fact]
    public async Task Register_uses_explicit_global_identity_read_and_rejects_active_duplicate()
    {
        var fixture = new Fixture();
        var user = AdminUser("company-active", "CorrectPassword1!");
        var company = CompanyRecord("company-active", user.UserId, user.EmailNormalized, active: true);
        fixture.UserRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, false))
            .ReturnsAsync(user);
        fixture.CompanyRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), false, false))
            .ReturnsAsync(company);

        var result = await fixture.Service.Register(Request("CorrectPassword1!"));

        Assert.False(result.Success);
        fixture.UserRepository.Verify(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, false), Times.Once);
        fixture.RoleService.Verify(x => x.AddDefaultRole(It.IsAny<string>()), Times.Never);
        fixture.CompanyRepository.Verify(x => x.AddOne(It.IsAny<Company>()), Times.Never);
    }

    [Fact]
    public async Task Register_rejects_inactive_retry_when_password_does_not_match()
    {
        var fixture = new Fixture();
        var user = AdminUser("company-pending", "CorrectPassword1!");
        var company = CompanyRecord("company-pending", user.UserId, user.EmailNormalized, active: false);
        fixture.UserRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, false))
            .ReturnsAsync(user);
        fixture.CompanyRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), false, false))
            .ReturnsAsync(company);

        var result = await fixture.Service.Register(Request("WrongPassword1!"));

        Assert.False(result.Success);
        fixture.RoleService.Verify(x => x.AddDefaultRole(It.IsAny<string>()), Times.Never);
        fixture.CompanyRepository.Verify(x => x.Update(It.IsAny<FilterDefinition<Company>>(), It.IsAny<Company>()), Times.Never);
    }

    [Fact]
    public async Task Register_resumes_matching_inactive_company_without_duplicate_company_or_user()
    {
        var fixture = new Fixture();
        var user = AdminUser("company-pending", "CorrectPassword1!");
        var company = CompanyRecord("company-pending", user.UserId, user.EmailNormalized, active: false);
        var role = new RoleEntity { RolesId = "admin-role", CompanyId = company.CompanyId, RoleType = 1, Titles = "Administrator", UserRoles = [] };
        var config = new RecruitmentEnterpriseConfig { CompanyId = company.CompanyId, DefaultCandidateRetentionDays = 730 };
        var token = new UserSecurityToken
        {
            UserId = user.UserId,
            TokenHash = "hash",
            IsUsed = false,
            Expiry = DateTime.UtcNow.AddHours(1),
            Type = SecurityTokenType.EmailVerification
        };
        var department = new Department { DepartmentId = "department-1", CompanyId = company.CompanyId, Titles = [] };
        var jobTitle = new JobTitles { JobTitleId = "job-1", CompanyId = company.CompanyId, Titles = [] };

        fixture.UserRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, false))
            .ReturnsAsync(user);
        fixture.CompanyRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), false, false))
            .ReturnsAsync(company);
        fixture.CompanyRepository
            .Setup(x => x.Update(It.IsAny<FilterDefinition<Company>>(), It.IsAny<Company>()))
            .ReturnsAsync(new Result { Success = true });
        fixture.RoleService.Setup(x => x.AddDefaultRole(company.CompanyId)).ReturnsAsync([role]);
        fixture.AttendanceStatusService.Setup(x => x.EnsureCompanyDefaults(company.CompanyId)).Returns(Task.CompletedTask);
        fixture.RecruitmentConfigRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(), false, false))
            .ReturnsAsync(config);
        fixture.TokenRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<UserSecurityToken, bool>>>(), false, true))
            .ReturnsAsync(token);
        fixture.DepartmentRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Department, bool>>>(), false, false))
            .ReturnsAsync(department);
        fixture.DepartmentRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<Department, bool>>>(), false, false))
            .ReturnsAsync([department]);
        fixture.JobTitleRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<JobTitles, bool>>>(), false, false))
            .ReturnsAsync(jobTitle);

        var result = await fixture.Service.Register(Request("CorrectPassword1!"));

        Assert.True(result.Success);
        Assert.True(company.Status);
        Assert.Equal("Active", company.ProvisioningState);
        Assert.Null(company.ProvisioningLastError);
        fixture.CompanyRepository.Verify(x => x.AddOne(It.IsAny<Company>()), Times.Never);
        fixture.UserRepository.Verify(x => x.AddOne(It.IsAny<EmpUser>()), Times.Never);
        fixture.EmployeeService.Verify(x => x.ReserveNextEmployeeId(It.IsAny<string>()), Times.Never);
        fixture.TokenRepository.Verify(x => x.AddOne(It.IsAny<UserSecurityToken>()), Times.Never);
        fixture.OfficeScheduleSettingsService.Verify(x => x.EnsureDefaultScheduleAsync(company.CompanyId, It.IsAny<CancellationToken>()), Times.Once);
        fixture.CompanyRepository.Verify(x => x.Update(It.IsAny<FilterDefinition<Company>>(), It.Is<Company>(saved =>
            saved.CompanyId == "company-pending" && saved.Status && saved.ProvisioningState == "Active")), Times.Once);
    }

    [Fact]
    public async Task Register_fails_provisioning_when_office_schedule_defaults_fail()
    {
        var fixture = new Fixture();
        var user = AdminUser("company-pending", "CorrectPassword1!");
        var company = CompanyRecord("company-pending", user.UserId, user.EmailNormalized, active: false);
        var role = new RoleEntity { RolesId = "admin-role", CompanyId = company.CompanyId, RoleType = 1, Titles = "Administrator", UserRoles = [] };

        fixture.UserRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, false))
            .ReturnsAsync(user);
        fixture.CompanyRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), false, false))
            .ReturnsAsync(company);
        fixture.CompanyRepository
            .Setup(x => x.Update(It.IsAny<FilterDefinition<Company>>(), It.IsAny<Company>()))
            .ReturnsAsync(new Result { Success = true });
        fixture.RoleService.Setup(x => x.AddDefaultRole(company.CompanyId)).ReturnsAsync([role]);
        fixture.AttendanceStatusService.Setup(x => x.EnsureCompanyDefaults(company.CompanyId)).Returns(Task.CompletedTask);
        fixture.OfficeScheduleSettingsService
            .Setup(x => x.EnsureDefaultScheduleAsync(company.CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<OfficeScheduleSettingsDto> { Success = false, Message = "OFFICE_SCHEDULE_DEFAULT_CREATE_FAILED" });

        var result = await fixture.Service.Register(Request("CorrectPassword1!"));

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Contains("office schedule", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("The office schedule defaults could not be created.", company.ProvisioningLastError);
        Assert.Equal("Failed", company.ProvisioningState);
    }

    [Fact]
    public async Task Register_returns_retryable_conflict_when_concurrent_company_wins_unique_key()
    {
        var fixture = new Fixture();
        var insertAttempted = false;
        var winner = CompanyRecord("company-winner", "admin-winner", "admin@example.com", active: false);
        fixture.UserRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, false))
            .ReturnsAsync((EmpUser?)null);
        fixture.CompanyRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), false, false))
            .ReturnsAsync((Expression<Func<Company, bool>> predicate, bool _, bool _) =>
                insertAttempted && predicate.Compile()(winner) ? winner : null);
        fixture.CompanyRepository
            .Setup(x => x.AddOne(It.IsAny<Company>()))
            .Callback(() => insertAttempted = true)
            .ReturnsAsync(new Result { Success = false, StatusCode = StatusCodes.Status409Conflict });

        var result = await fixture.Service.Register(Request("CorrectPassword1!"));

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Contains("Retry", result.Message, StringComparison.OrdinalIgnoreCase);
        fixture.RoleService.Verify(x => x.AddDefaultRole(It.IsAny<string>()), Times.Never);
        fixture.UserRepository.Verify(x => x.AddOne(It.IsAny<EmpUser>()), Times.Never);
    }
    private static CompanyRequestModel Request(string password) => new()
    {
        CompanyName = "Acme",
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = "Admin@Example.com",
        Password = password,
        AcceptTerms = true
    };

    private static EmpUser AdminUser(string companyId, string password) => new()
    {
        UserId = "admin-1",
        CompanyId = companyId,
        RoleId = "admin-role",
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = "Admin@Example.com",
        EmailNormalized = "admin@example.com",
        Password = AuthenticationHandler.HashedPassword(password),
        EmployeeId = "ACME-0001",
        Status = true
    };

    private static Company CompanyRecord(string id, string userId, string email, bool active) => new()
    {
        CompanyId = id,
        CompanyName = "Acme",
        PrimaryContact = userId,
        DefaultLanguage = Languages.English,
        ApplicationLanguage = [Languages.English],
        RegistrationEmailNormalized = email,
        ProvisioningState = active ? "Active" : "Failed",
        Status = active
    };

    private sealed class Fixture
    {
        public Mock<IMongoDbRepository<Company>> CompanyRepository { get; } = new();
        public Mock<IMongoDbRepository<EmpUser>> UserRepository { get; } = new();
        public Mock<IMongoDbRepository<UserSecurityToken>> TokenRepository { get; } = new();
        public Mock<IMongoDbRepository<Department>> DepartmentRepository { get; } = new();
        public Mock<IMongoDbRepository<JobTitles>> JobTitleRepository { get; } = new();
        public Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>> RecruitmentConfigRepository { get; } = new();
        public Mock<IRoleService> RoleService { get; } = new();
        public Mock<IEmployeeService> EmployeeService { get; } = new();
        public Mock<IMiddlewareService> MiddlewareService { get; } = new();
        public Mock<IAttendanceStatusService> AttendanceStatusService { get; } = new();
        public Mock<IOfficeScheduleSettingsService> OfficeScheduleSettingsService { get; } = new();
        public CompanyService Service { get; }

        public Fixture()
        {
            Service = new CompanyService(
                CompanyRepository.Object,
                new Mock<IMapper>().Object,
                UserRepository.Object,
                new Mock<IMongoDbRepository<RoleEntity>>().Object,
                new Mock<IMongoDbRepository<ModulePermission>>().Object,
                new Mock<IMongoDbRepository<RolePermission>>().Object,
                new Mock<IMongoDbRepository<NotificationPreference>>().Object,
                TokenRepository.Object,
                new Mock<IMongoDbRepository<Policy>>().Object,
                new Mock<IMongoDbRepository<PolicyVersion>>().Object,
                DepartmentRepository.Object,
                JobTitleRepository.Object,
                RoleService.Object,
                EmployeeService.Object,
                MiddlewareService.Object,
                AttendanceStatusService.Object,
                new Mock<IMongoClient>().Object,
                new Mock<ILogger<CompanyService>>().Object,
                RecruitmentConfigRepository.Object,
                OfficeScheduleSettingsService.Object);
            OfficeScheduleSettingsService.Setup(x => x.EnsureDefaultScheduleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Result<OfficeScheduleSettingsDto> { Success = true });
        }
    }
}