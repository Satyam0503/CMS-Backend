using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeIdEditAndCascadeTests
{
    private const string CurrentCompanyId = "company-101";
    private const string ActorUserId = "admin-user-001";
    private const string TargetUserId = "emp-user-002";

    [Fact]
    public async Task EditEmployee_AllowsHrOrAdminToUpdateEmployeeId_Successfully()
    {
        var fixture = CreateEmployeeFixture();
        var existingEmployee = new EmpUser
        {
            UserId = TargetUserId,
            CompanyId = CurrentCompanyId,
            Email = "alice@example.com",
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeId = "EMP-0001",
            Status = true,
            IsDeleted = false
        };

        fixture.EmployeeRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(existingEmployee);

        fixture.EmployeeRepository
            .Setup(x => x.Exist(It.IsAny<Expression<Func<EmpUser, bool>>>(), false))
            .ReturnsAsync(false); // No duplicate exists

        fixture.EmployeeRepository
            .Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpUser>>(), It.IsAny<UpdateDefinition<EmpUser>>(), true))
            .ReturnsAsync(new Result { Success = true, StatusCode = 200 });

        fixture.Mapper
            .Setup(x => x.Map<UserModel>(It.IsAny<EmpUser>()))
            .Returns(new UserModel { UserId = TargetUserId, EmployeeId = "EMP-0042", FirstName = "Alice", LastName = "Smith" });

        var request = new EmployeePersonalInfo
        {
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeId = "EMP-0042",
            RoleId = ""
        };

        var result = await fixture.Service.EditEmployee(request, TargetUserId);

        Assert.True(result.Success);
        Assert.Equal("EMP-0042", request.EmployeeId);
        fixture.EmployeeRepository.Verify(x => x.UpdateMany(
            It.IsAny<FilterDefinition<EmpUser>>(),
            It.IsAny<UpdateDefinition<EmpUser>>(),
            true
        ), Times.Once);
    }

    [Fact]
    public async Task EditEmployee_RejectsDuplicateEmployeeId_WithinSameCompany()
    {
        var fixture = CreateEmployeeFixture();
        var existingEmployee = new EmpUser
        {
            UserId = TargetUserId,
            CompanyId = CurrentCompanyId,
            Email = "alice@example.com",
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeId = "EMP-0001",
            Status = true,
            IsDeleted = false
        };

        fixture.EmployeeRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(existingEmployee);

        // Simulate another employee already having EMP-0042 in company-101
        fixture.EmployeeRepository
            .Setup(x => x.Exist(It.IsAny<Expression<Func<EmpUser, bool>>>(), false))
            .ReturnsAsync(true);

        var request = new EmployeePersonalInfo
        {
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeId = "EMP-0042",
            RoleId = ""
        };

        var result = await fixture.Service.EditEmployee(request, TargetUserId);

        Assert.False(result.Success);
        Assert.Equal(CustomStatusCode.EmployeeIdAlreadyExist, result.StatusCode);
        Assert.Contains("already assigned to another employee", result.Message);
        fixture.EmployeeRepository.Verify(x => x.UpdateMany(
            It.IsAny<FilterDefinition<EmpUser>>(),
            It.IsAny<UpdateDefinition<EmpUser>>(),
            true
        ), Times.Never);
    }

    [Fact]
    public async Task EditEmployee_AllowsSameEmployeeId_WithoutConflict()
    {
        var fixture = CreateEmployeeFixture();
        var existingEmployee = new EmpUser
        {
            UserId = TargetUserId,
            CompanyId = CurrentCompanyId,
            Email = "alice@example.com",
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeId = "EMP-0001",
            Status = true,
            IsDeleted = false
        };

        fixture.EmployeeRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(existingEmployee);

        fixture.EmployeeRepository
            .Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpUser>>(), It.IsAny<UpdateDefinition<EmpUser>>(), true))
            .ReturnsAsync(new Result { Success = true, StatusCode = 200 });

        fixture.Mapper
            .Setup(x => x.Map<UserModel>(It.IsAny<EmpUser>()))
            .Returns(new UserModel { UserId = TargetUserId, EmployeeId = "EMP-0001", FirstName = "Alice", LastName = "Smith" });

        var request = new EmployeePersonalInfo
        {
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeId = "EMP-0001", // Unchanged
            RoleId = ""
        };

        var result = await fixture.Service.EditEmployee(request, TargetUserId);

        Assert.True(result.Success);
        // Exist should NOT even be called since the ID didn't change
        fixture.EmployeeRepository.Verify(x => x.Exist(It.IsAny<Expression<Func<EmpUser, bool>>>(), false), Times.Never);
    }

    [Fact]
    public async Task EditEmployee_PreservesExistingEmployeeId_WhenNullOrWhitespace()
    {
        var fixture = CreateEmployeeFixture();
        var existingEmployee = new EmpUser
        {
            UserId = TargetUserId,
            CompanyId = CurrentCompanyId,
            Email = "alice@example.com",
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeId = "EMP-0001",
            Status = true,
            IsDeleted = false
        };

        fixture.EmployeeRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(existingEmployee);

        fixture.EmployeeRepository
            .Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpUser>>(), It.IsAny<UpdateDefinition<EmpUser>>(), true))
            .ReturnsAsync(new Result { Success = true, StatusCode = 200 });

        fixture.Mapper
            .Setup(x => x.Map<UserModel>(It.IsAny<EmpUser>()))
            .Returns(new UserModel { UserId = TargetUserId, EmployeeId = "EMP-0001", FirstName = "Alice", LastName = "Smith" });

        var request = new EmployeePersonalInfo
        {
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeId = null, // Omitted
            RoleId = ""
        };

        var result = await fixture.Service.EditEmployee(request, TargetUserId);

        Assert.True(result.Success);
        Assert.Equal("EMP-0001", request.EmployeeId);
    }

    [Fact]
    public async Task EditEmployee_CascadesEmployeeIdUpdate_ToCollectionsAndResyncsSequence()
    {
        var fixture = CreateEmployeeFixture();
        var existingEmployee = new EmpUser
        {
            UserId = TargetUserId,
            CompanyId = CurrentCompanyId,
            Email = "alice@example.com",
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeId = "EMP-0001",
            Status = true,
            IsDeleted = false
        };

        fixture.EmployeeRepository
            .Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(existingEmployee);

        fixture.EmployeeRepository
            .Setup(x => x.Exist(It.IsAny<Expression<Func<EmpUser, bool>>>(), false))
            .ReturnsAsync(false);

        fixture.EmployeeRepository
            .Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmpUser>>(), It.IsAny<UpdateDefinition<EmpUser>>(), true))
            .ReturnsAsync(new Result { Success = true, StatusCode = 200 });

        fixture.Mapper
            .Setup(x => x.Map<UserModel>(It.IsAny<EmpUser>()))
            .Returns(new UserModel { UserId = TargetUserId, EmployeeId = "CD-0050", FirstName = "Alice", LastName = "Smith" });

        fixture.CompanyRepository
            .Setup(c => c.FirstOrDefault(It.IsAny<Expression<Func<Company, bool>>>(), false, true))
            .ReturnsAsync(new Company { CompanyId = CurrentCompanyId, CompanyName = "Codeji", DefaultLanguage = "en", EmployeeIdPrefix = "CD" });

        var existingSequence = new EmployeeIdSequence
        {
            SequenceId = "seq-101",
            CompanyId = CurrentCompanyId,
            Prefix = "CD-",
            NextNumber = 10
        };

        fixture.EmployeeIdSequenceRepository
            .Setup(s => s.FirstOrDefault(It.IsAny<Expression<Func<EmployeeIdSequence, bool>>>(), false, true))
            .ReturnsAsync(existingSequence);

        var mockSequenceCollection = new Mock<IMongoCollection<EmployeeIdSequence>>();
        fixture.EmployeeIdSequenceRepository
            .Setup(r => r.GetCollection())
            .Returns(mockSequenceCollection.Object);

        var request = new EmployeePersonalInfo
        {
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeId = "CD-0050",
            RoleId = ""
        };

        var result = await fixture.Service.EditEmployee(request, TargetUserId);

        Assert.True(result.Success);
        // Sequence should be updated via collection UpdateOneAsync because 50 >= 10, next number should become 51
        mockSequenceCollection.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<EmployeeIdSequence>>(),
            It.IsAny<UpdateDefinition<EmployeeIdSequence>>(),
            null,
            default
        ), Times.Once);
    }

    private static EmployeeServiceFixture CreateEmployeeFixture()
    {
        var fixture = new EmployeeServiceFixture();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(EnumsHelper.ClaimTypesEnum.company_id.ToString(), CurrentCompanyId),
            new Claim(EnumsHelper.ClaimTypesEnum.user_id.ToString(), ActorUserId),
            new Claim(ClaimTypes.Role, "Admin")
        ], "TestAuth"));

        fixture.HttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var mockDb = new Mock<IMongoDatabase>();
        var mockCollection = new Mock<IMongoCollection<EmpUser>>();
        mockCollection.Setup(c => c.Database).Returns(mockDb.Object);
        fixture.EmployeeRepository.Setup(r => r.GetCollection()).Returns(mockCollection.Object);

        var mockBsonCollection = new Mock<IMongoCollection<BsonDocument>>();
        mockDb.Setup(d => d.GetCollection<BsonDocument>(It.IsAny<string>(), null))
            .Returns(mockBsonCollection.Object);

        var mockSequenceCollection = new Mock<IMongoCollection<EmployeeIdSequence>>();
        fixture.EmployeeIdSequenceRepository.Setup(r => r.GetCollection()).Returns(mockSequenceCollection.Object);

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
