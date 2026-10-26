using System.Linq.Expressions;
using System.Security.Claims;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.PublicCareers;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.CareerPortal;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

[Trait("Category", "Security")]
public sealed class SecurityAuditRemediationTests
{
    private const string CurrentCompanyId = "company-tenant-alpha";
    private const string CurrentUserId = "user-alice";

    [Fact]
    public async Task GetEmployeeById_WhenUserNotFound_ReturnsNullWithoutThrowing()
    {
        var fixture = CreateEmployeeFixture();
        fixture.EmployeeRepository
            .Setup(repo => repo.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync((EmpUser?)null);

        var result = await fixture.Service.GetEmployeeById("non-existent-user");

        Assert.Null(result);
    }

    [Fact]
    public async Task MarkNotificationAsRead_IsScopedToTheAuthenticatedTenantAndRecipient()
    {
        var fixture = CreateEmployeeFixture();
        FilterDefinition<UserNotifications>? captured = null;
        fixture.UserNotificationRepository
            .Setup(repo => repo.Exist(It.IsAny<Expression<Func<UserNotifications, bool>>>(), false))
            .ReturnsAsync(true);
        fixture.UserNotificationRepository
            .Setup(repo => repo.UpdateMany(It.IsAny<FilterDefinition<UserNotifications>>(), It.IsAny<UpdateDefinition<UserNotifications>>(), false))
            .Callback<FilterDefinition<UserNotifications>, UpdateDefinition<UserNotifications>, bool>((filter, _, _) => captured = filter)
            .ReturnsAsync(new Result { Success = true });

        var result = await fixture.Service.MarkNotificationAsRead(CurrentUserId, "notification-row-1");

        Assert.True(result.Success);
        Assert.NotNull(captured);
        var rendered = captured!.Render(new RenderArgs<UserNotifications>(
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<UserNotifications>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry)).ToJson();
        Assert.Contains(CurrentCompanyId, rendered, StringComparison.Ordinal);
        Assert.Contains(CurrentUserId, rendered, StringComparison.Ordinal);
        Assert.Contains("notification-row-1", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteEmployee_WhenEmployeeBelongsToDifferentCompany_ReturnsErrorAndDoesNotMutateChildRecords()
    {
        var fixture = CreateEmployeeFixture();
        // Target employee does not exist in CurrentCompanyId
        fixture.EmployeeRepository
            .Setup(repo => repo.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync((EmpUser?)null);

        var result = await fixture.Service.DeleteEmployee("victim-from-other-company");

        Assert.False(result.Success);
        Assert.Equal("Employee not found.", result.Message);

        // Child collections MUST NOT have been touched:
        fixture.CertificationRepository.Verify(repo => repo.UpdateMany(It.IsAny<FilterDefinition<EmpCertificationDetails>>(), It.IsAny<UpdateDefinition<EmpCertificationDetails>>(), false), Times.Never);
        fixture.EducationRepository.Verify(repo => repo.UpdateMany(It.IsAny<FilterDefinition<EmpEducationDetails>>(), It.IsAny<UpdateDefinition<EmpEducationDetails>>(), false), Times.Never);
        fixture.EmployeeSkillsRepository.Verify(repo => repo.UpdateMany(It.IsAny<FilterDefinition<EmpSkills>>(), It.IsAny<UpdateDefinition<EmpSkills>>(), false), Times.Never);
        fixture.SummaryRepository.Verify(repo => repo.UpdateMany(It.IsAny<FilterDefinition<EmpSummary>>(), It.IsAny<UpdateDefinition<EmpSummary>>(), false), Times.Never);
        fixture.WorkHistoryRepository.Verify(repo => repo.UpdateMany(It.IsAny<FilterDefinition<EmpWorkHistory>>(), It.IsAny<UpdateDefinition<EmpWorkHistory>>(), false), Times.Never);
    }

    [Fact]
    public async Task DeleteWorkHistory_WhenWorkHistoryBelongsToAnotherUser_ReturnsNotFound()
    {
        var fixture = CreateEmployeeFixture();
        fixture.WorkHistoryRepository
            .Setup(repo => repo.Exist(It.IsAny<Expression<Func<EmpWorkHistory, bool>>>(), false))
            .ReturnsAsync(false);

        var result = await fixture.Service.DeleteWorkHistory("work-123", "attacker-user");

        Assert.False(result.Success);
        Assert.Equal("Work history not found.", result.Message);
        fixture.WorkHistoryRepository.Verify(repo => repo.UpdateMany(It.IsAny<FilterDefinition<EmpWorkHistory>>(), It.IsAny<UpdateDefinition<EmpWorkHistory>>(), false), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task PublicCareer_GetCompanyJobs_WhenCompanyCodeIsBlank_ReturnsNotFoundWithoutQuerying(string? code)
    {
        var publicService = CreatePublicCareerService();

        var result = await publicService.GetCompanyJobs(code!, new PublicJobSearchRequest());

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Equal("Career portal not found.", result.Message);
    }

    private static PublicCareerService CreatePublicCareerService()
    {
        var jobRepo = new Mock<IMongoDbRepository<JobVacancy>>();
        jobRepo.Setup(r => r.GetCollection()).Returns(new Mock<IMongoCollection<JobVacancy>>().Object);

        var companyRepo = new Mock<IMongoDbRepository<Company>>();
        companyRepo.Setup(r => r.GetCollection()).Returns(new Mock<IMongoCollection<Company>>().Object);

        var applicantRepo = new Mock<IMongoDbRepository<Applicant>>();
        applicantRepo.Setup(r => r.GetCollection()).Returns(new Mock<IMongoCollection<Applicant>>().Object);

        var applicantLogsRepo = new Mock<IMongoDbRepository<ApplicantLogs>>();
        applicantLogsRepo.Setup(r => r.GetCollection()).Returns(new Mock<IMongoCollection<ApplicantLogs>>().Object);

        var tokenRepo = new Mock<IMongoDbRepository<PublicApplicationToken>>();
        tokenRepo.Setup(r => r.GetCollection()).Returns(new Mock<IMongoCollection<PublicApplicationToken>>().Object);

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(r => r.GetCollection()).Returns(new Mock<IMongoCollection<EmpUser>>().Object);

        var roleRepo = new Mock<IMongoDbRepository<Roles>>();
        roleRepo.Setup(r => r.GetCollection()).Returns(new Mock<IMongoCollection<Roles>>().Object);

        var profileRepo = new Mock<IMongoDbRepository<PublicCompanyProfile>>();
        profileRepo.Setup(r => r.GetCollection()).Returns(new Mock<IMongoCollection<PublicCompanyProfile>>().Object);

        var savedJobRepo = new Mock<IMongoDbRepository<Codeji.CMS.Repository.Entities.CareerPortal.SavedJob>>();
        savedJobRepo.Setup(r => r.GetCollection()).Returns(new Mock<IMongoCollection<Codeji.CMS.Repository.Entities.CareerPortal.SavedJob>>().Object);

        var analyticsRepo = new Mock<IMongoDbRepository<Codeji.CMS.Repository.Entities.CareerPortal.CareerAnalyticsEvent>>();
        analyticsRepo.Setup(r => r.GetCollection()).Returns(new Mock<IMongoCollection<Codeji.CMS.Repository.Entities.CareerPortal.CareerAnalyticsEvent>>().Object);

        var tasks = new Mock<IPriorityTaskQueue>();
        var middleware = new Mock<IMiddlewareService>();

        return new PublicCareerService(
            jobRepo.Object,
            companyRepo.Object,
            applicantRepo.Object,
            applicantLogsRepo.Object,
            tokenRepo.Object,
            empRepo.Object,
            roleRepo.Object,
            profileRepo.Object,
            savedJobRepo.Object,
            analyticsRepo.Object,
            tasks.Object,
            middleware.Object);
    }

    private static EmployeeServiceFixture CreateEmployeeFixture()
    {
        var fixture = new EmployeeServiceFixture();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(EnumsHelper.ClaimTypesEnum.company_id.ToString(), CurrentCompanyId),
            new Claim(EnumsHelper.ClaimTypesEnum.user_id.ToString(), CurrentUserId),
            new Claim(ClaimTypes.Role, "Employee")
        ], "TestAuth"));

        fixture.HttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        fixture.Service = new EmployeeService(
            educationDetailsRepo: fixture.EducationRepository.Object,
            mapper: fixture.Mapper.Object,
            certificationDetailsRepo: fixture.CertificationRepository.Object,
            userSummary: fixture.SummaryRepository.Object,
            employeeRepository: fixture.EmployeeRepository.Object,
            employeeIdSequenceRepository: fixture.EmployeeIdSequenceRepository.Object,
            rolesRepository: fixture.RolesRepository.Object,
            roleService: fixture.RoleService.Object,
            employeeSkillsRepository: fixture.EmployeeSkillsRepository.Object,
            companyRepository: fixture.CompanyRepository.Object,
            mailTemplateRepository: fixture.MailTemplateRepository.Object,
            priorityTaskQueue: fixture.PriorityTaskQueue.Object,
            middlewareService: fixture.MiddlewareService.Object,
            httpContextAccessor: fixture.HttpContextAccessor.Object,
            departmentRepository: fixture.DepartmentRepository.Object,
            skillsRepository: fixture.SkillsRepository.Object,
            empWorkHistoryRepository: fixture.WorkHistoryRepository.Object,
            userNotificationRepository: fixture.UserNotificationRepository.Object,
            notificationsRepository: fixture.NotificationsRepository.Object,
            jobTitlesRepository: fixture.JobTitlesRepository.Object,
            customAttributeRepository: fixture.CustomAttributeRepository.Object,
            customAttributeValueRepository: fixture.CustomAttributeValueRepository.Object,
            notificationService: fixture.NotificationService.Object,
            notificationPreferenceRepository: fixture.NotificationPreferenceRepository.Object,
            userSecurityTokenRepository: fixture.UserSecurityTokenRepository.Object,
            officeScheduleRepository: fixture.OfficeScheduleRepository.Object,
            employeeScheduleAssignmentRepository: fixture.EmployeeScheduleAssignmentRepository.Object,
            logger: fixture.Logger.Object,
            employeeExitRepository: fixture.EmployeeExitRepository.Object,
            refreshTokenRepository: null,
            employeeExitPolicyRepository: fixture.EmployeeExitPolicyRepository.Object);

        return fixture;
    }

    private sealed class EmployeeServiceFixture
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

