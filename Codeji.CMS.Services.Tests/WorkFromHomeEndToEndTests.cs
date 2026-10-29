using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.DTO.LeaveManagement.LeavePolicy;
using Codeji.CMS.DTO.RolePermissions;
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
using Codeji.CMS.Utility.Enums;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Moq;

namespace Codeji.CMS.Services.Tests;

public class WorkFromHomeEndToEndTests
{
    private static MongoCommandException CreateStandaloneMongoException()
    {
        var connectionId = new ConnectionId(
            new ServerId(new ClusterId(1), new System.Net.DnsEndPoint("192.168.0.172", 27017)), 1);
        var response = new BsonDocument
        {
            { "ok", 0.0 },
            { "errmsg", "Transaction numbers are only allowed on a replica set member or mongos" },
            { "code", 20 },
            { "codeName", "IllegalOperation" }
        };
        return new MongoCommandException(
            connectionId,
            "Command failed: Transaction numbers are only allowed on a replica set member or mongos.",
            new BsonDocument("insert", "Attendance"),
            response);
    }

    [Fact]
    public void ResolveEffectiveCutoffTime_WhenCustomCutoffNull_FallsBackToOfficeStart()
    {
        var officeStart = new TimeSpan(9, 30, 0);
        var cutoff = WorkFromHomeService.ResolveEffectiveCutoffTime(officeStart, null, 15);

        // 9:30 + 15 min grace = 9:45
        Assert.Equal(new TimeSpan(9, 45, 0), cutoff);
    }

    [Fact]
    public void ResolveEffectiveCutoffTime_WhenBothCustomAndBaseNull_ReturnsNull()
    {
        var cutoff = WorkFromHomeService.ResolveEffectiveCutoffTime(null, null, 15);

        Assert.Null(cutoff);
    }

    [Fact]
    public void ResolveEffectiveCutoffTime_WithCustomCutoff_OverridesOfficeStart()
    {
        var officeStart = new TimeSpan(9, 30, 0);
        var custom = new TimeSpan(11, 0, 0);
        var cutoff = WorkFromHomeService.ResolveEffectiveCutoffTime(officeStart, custom, 15);

        // 11:00 + 15 min grace = 11:15
        Assert.Equal(new TimeSpan(11, 15, 0), cutoff);
    }

    [Fact]
    public void ResolveEffectiveCutoffTime_WithNegativeGraceMinutes_TreatedAsZero()
    {
        var officeStart = new TimeSpan(9, 30, 0);
        var custom = new TimeSpan(9, 30, 0);
        var cutoff = WorkFromHomeService.ResolveEffectiveCutoffTime(officeStart, custom, -30);

        Assert.Equal(new TimeSpan(9, 30, 0), cutoff);
    }

    [Theory]
    [InlineData("FullDay", 10, 25, 9, 30, 60, false)] // 10:25 is BEFORE 10:30 (9:30 + 60 grace) => NOT blocked
    [InlineData("FullDay", 10, 31, 9, 30, 60, true)]  // 10:31 is AFTER 10:30 => blocked
    [InlineData("SecondHalf", 10, 25, 9, 30, 0, false)] // SecondHalf boundary is 13:30 => 10:25 is NOT blocked
    [InlineData("SecondHalf", 13, 29, 9, 30, 0, false)] // 13:29 is BEFORE 13:30 => NOT blocked
    [InlineData("SecondHalf", 13, 31, 9, 30, 0, true)]  // 13:31 is AFTER 13:30 => blocked
    [InlineData("FirstHalf", 10, 25, 9, 30, 0, true)]   // FirstHalf cutoff is 9:30 => 10:25 is blocked
    public void CutoffCalculation_RespectsDurationAndGrace(
        string duration, int nowHour, int nowMinute, int startHour, int startMinute, int graceMinutes, bool expectedCutoffPassed)
    {
        var today = new DateTime(2026, 10, 23);
        var companyNow = new DateTime(2026, 10, 23, nowHour, nowMinute, 0);
        var officeStart = new TimeSpan(startHour, startMinute, 0);
        var customCutoff = new TimeSpan(startHour, startMinute, 0);

        var cutoffPassed = WorkFromHomeService.IsSameDayWfhStartCutoffReached(
            today, companyNow, duration, officeStart, customCutoff, graceMinutes, 480, 8m);

        Assert.Equal(expectedCutoffPassed, cutoffPassed);
    }

    [Fact]
    public void WorkFromHomePolicySettings_DefaultsAllowLateApplication()
    {
        var settings = new WorkFromHomePolicySettings();
        Assert.True(settings.AllowLateApplicationWithApproval);
        Assert.Equal(0, settings.SameDayGraceMinutes);
        Assert.Null(settings.SameDayCutoffTime);
    }

    [Fact]
    public void WorkFromHomeRequest_DefaultNotLateSubmission()
    {
        var request = new WorkFromHomeRequest();
        Assert.False(request.IsLateSubmission);
    }

    [Fact]
    public void IsTransactionUnsupported_DetectsStandaloneMongoCommandException()
    {
        var ex = CreateStandaloneMongoException();
        Assert.True(WorkFromHomeService.IsTransactionUnsupported(ex));
        Assert.True(LeaveManagementService.IsTransactionUnsupported(ex));
    }

    [Theory]
    [InlineData("Transaction numbers are only allowed on a replica set member or mongos")]
    [InlineData("Cannot run in transaction: replica set required")]
    [InlineData("standalone servers do not support transactions")]
    public void IsTransactionUnsupported_DetectsMessageVariants(string message)
    {
        var ex = new Exception(message);
        Assert.True(WorkFromHomeService.IsTransactionUnsupported(ex));
        Assert.True(LeaveManagementService.IsTransactionUnsupported(ex));
    }

    [Fact]
    public async Task Create_WhenMongoTransactionsFailOnStandalone_AutoApprovesAndInsertsAttendanceSuccessfully()
    {
        const string companyId = "c1";
        const string userId = "u1";
        var futureDate = DateTime.UtcNow.Date.AddDays(7);
        var targetDateOnly = DateOnly.FromDateTime(futureDate);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.company_id.ToString(), companyId),
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.user_id.ToString(), userId),
            new Claim(ClaimTypes.Role, "Employee")
        ], "TestAuth"));
        httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        var roles = new Mock<IRoleService>();
        roles.Setup(x => x.IsModuleAccessibleForCompany("Work_From_Home", companyId)).ReturnsAsync(true);
        roles.Setup(x => x.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserCheckModel>())).ReturnsAsync(true);

        var policy = new WorkFromHomePolicy
        {
            CompanyId = companyId,
            PolicyId = "policy-1",
            IsEnabled = true,
            Version = 1,
            ManagerApprovalRequired = false, // Auto-approval
            MaxDaysPerWeek = 5,
            MaxDaysPerMonth = 20,
            FullDayAttendanceStatusCode = "WFH",
            HalfDayAttendanceStatusCode = "WFH-HD",
            FullDayMinimumHours = 8m,
            HalfDayMinimumHours = 4m,
            TimeZoneId = "UTC",
            ApplicableDepartments = [],
            ApplicableEmployeeIds = [],
            ExcludedEmployeeIds = [],
            ApplicableEmploymentTypes = []
        };
        var policyRepo = new Mock<IMongoDbRepository<WorkFromHomePolicy>>();
        policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>())).ReturnsAsync(policy);
        var policyCollection = new Mock<IMongoCollection<WorkFromHomePolicy>>();
        policyCollection.Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<WorkFromHomePolicy>>(), It.IsAny<UpdateDefinition<WorkFromHomePolicy>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        policyRepo.Setup(x => x.GetCollection()).Returns(policyCollection.Object);

        var employee = new EmpUser
        {
            CompanyId = companyId,
            UserId = userId,
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Status = true
        };
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>())).ReturnsAsync(employee);

        var requestRepo = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>())).ReturnsAsync((WorkFromHomeRequest?)null);
        requestRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Enumerable.Empty<WorkFromHomeRequest>());
        WorkFromHomeRequest? savedRequest = null;
        var requestCollection = new Mock<IMongoCollection<WorkFromHomeRequest>>();
        requestCollection.Setup(x => x.InsertOneAsync(It.IsAny<WorkFromHomeRequest>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<WorkFromHomeRequest, InsertOneOptions, CancellationToken>((r, _, _) => savedRequest = r)
            .Returns(Task.CompletedTask);
        requestRepo.Setup(x => x.GetCollection()).Returns(requestCollection.Object);

        var attendanceRepo = new Mock<IMongoDbRepository<AttendanceModel>>();
        attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>())).ReturnsAsync((AttendanceModel?)null);
        AttendanceModel? savedAttendance = null;
        var attendanceCollection = new Mock<IMongoCollection<AttendanceModel>>();
        attendanceCollection.Setup(x => x.InsertOneAsync(It.IsAny<AttendanceModel>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceModel, InsertOneOptions, CancellationToken>((a, _, _) => savedAttendance = a)
            .Returns(Task.CompletedTask);
        attendanceRepo.Setup(x => x.GetCollection()).Returns(attendanceCollection.Object);

        var outboxRepo = new Mock<IMongoDbRepository<WorkFromHomeOutboxEvent>>();
        var outboxEvents = new List<WorkFromHomeOutboxEvent>();
        var outboxCollection = new Mock<IMongoCollection<WorkFromHomeOutboxEvent>>();
        outboxCollection.Setup(x => x.InsertManyAsync(It.IsAny<IEnumerable<WorkFromHomeOutboxEvent>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<WorkFromHomeOutboxEvent>, InsertManyOptions, CancellationToken>((evts, _, _) => outboxEvents.AddRange(evts))
            .Returns(Task.CompletedTask);
        outboxRepo.Setup(x => x.GetCollection()).Returns(outboxCollection.Object);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>()))
            .ReturnsAsync(new AttendanceStatusSetting { CompanyId = companyId, Code = "WFH", Name = "Work From Home", IsActive = true });
        statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        // Simulate Standalone MongoDB: session starts, but WithTransactionAsync throws MongoCommandException (code 20)
        var mongoClient = new Mock<IMongoClient>();
        var sessionMock = new Mock<IClientSessionHandle>();
        sessionMock.Setup(x => x.WithTransactionAsync(
                It.IsAny<Func<IClientSessionHandle, CancellationToken, Task<bool>>>(),
                It.IsAny<TransactionOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateStandaloneMongoException());
        mongoClient.Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            policyRepo.Object,
            requestRepo.Object,
            outboxRepo.Object,
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            empRepo.Object,
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            statusRepo.Object,
            attendanceRepo.Object,
            Mock.Of<IMongoDbRepository<AttendanceDaySegment>>(),
            Mock.Of<IMongoDbRepository<AttendancePayrollException>>(),
            Mock.Of<IMongoDbRepository<MonthlyAttendanceSummary>>(),
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            Mock.Of<IAttendanceEditGuard>(),
            Mock.Of<IEffectiveOfficeScheduleService>(),
            Mock.Of<IAttendanceInitializationService>(),
            roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            mongoClient.Object,
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        var dto = new WorkFromHomeRequestDto
        {
            FromDate = targetDateOnly,
            ToDate = targetDateOnly,
            DurationType = "FullDay",
            ReasonCode = "PERSONAL",
            ReasonText = "Doctor consultation"
        };

        var result = await service.Create(dto, "idempotency-key-standalone-test-123");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.MethodResult);
        Assert.Equal("Approved", result.MethodResult.Status);
        Assert.NotNull(savedRequest);
        Assert.Equal("Approved", savedRequest.Status);
        Assert.NotNull(savedAttendance);
        Assert.Equal("WFH", savedAttendance.Status);
        Assert.Equal("WFH_REQUEST", savedAttendance.SourceType);
        Assert.NotEmpty(outboxEvents);
        Assert.False(string.IsNullOrWhiteSpace(savedRequest.RequestId));
        Assert.All(outboxEvents, evt =>
        {
            Assert.Equal(savedRequest.RequestId, evt.RequestId);
            Assert.False(string.IsNullOrWhiteSpace(evt.EventId));
            Assert.Contains(savedRequest.RequestId, evt.DedupeKey);
        });
    }

    [Fact]
    public async Task Decide_WhenMongoTransactionsFailOnStandalone_ApprovesAndReconcilesAttendanceSuccessfully()
    {
        const string companyId = "c1";
        const string managerId = "manager-1";
        const string employeeId = "u1";
        var targetDate = DateTime.UtcNow.Date.AddDays(7);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.company_id.ToString(), companyId),
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.user_id.ToString(), managerId),
            new Claim(ClaimTypes.Role, "Manager")
        ], "TestAuth"));
        httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        var roles = new Mock<IRoleService>();
        roles.Setup(x => x.IsModuleAccessibleForCompany("Work_From_Home", companyId)).ReturnsAsync(true);
        roles.Setup(x => x.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserCheckModel>())).ReturnsAsync(true);

        var request = new WorkFromHomeRequest
        {
            CompanyId = companyId,
            RequestId = "req-decide-1",
            UserId = employeeId,
            EmployeeId = "EMP-001",
            Status = "Pending",
            Version = 1,
            FromDate = targetDate,
            ToDate = targetDate,
            DurationType = "FullDay",
            ReasonCode = "PERSONAL",
            ApproverUserId = managerId
        };

        var policy = new WorkFromHomePolicy
        {
            CompanyId = companyId,
            PolicyId = "policy-1",
            IsEnabled = true,
            Version = 1,
            MaxDaysPerWeek = 5,
            MaxDaysPerMonth = 20,
            FullDayAttendanceStatusCode = "WFH",
            HalfDayAttendanceStatusCode = "WFH-HD",
            FullDayMinimumHours = 8m,
            HalfDayMinimumHours = 4m,
            TimeZoneId = "UTC",
            ApplicableDepartments = [],
            ApplicableEmployeeIds = [],
            ExcludedEmployeeIds = [],
            ApplicableEmploymentTypes = []
        };
        var policyRepo = new Mock<IMongoDbRepository<WorkFromHomePolicy>>();
        policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>())).ReturnsAsync(policy);
        var policyCollection = new Mock<IMongoCollection<WorkFromHomePolicy>>();
        policyCollection.Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<WorkFromHomePolicy>>(), It.IsAny<UpdateDefinition<WorkFromHomePolicy>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        policyRepo.Setup(x => x.GetCollection()).Returns(policyCollection.Object);

        var employee = new EmpUser
        {
            CompanyId = companyId,
            UserId = employeeId,
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Status = true
        };
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>())).ReturnsAsync(employee);
        var empCollection = new Mock<IMongoCollection<EmpUser>>();
        var empCursor = new Mock<IAsyncCursor<EmpUser>>();
        empCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        empCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true).ReturnsAsync(false);
        empCursor.Setup(x => x.Current).Returns([employee]);
        empCollection.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<EmpUser>>(), It.IsAny<FindOptions<EmpUser, EmpUser>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empCursor.Object);
        empRepo.Setup(x => x.GetCollection()).Returns(empCollection.Object);

        var requestRepo = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>())).ReturnsAsync(request);
        WorkFromHomeRequest? replacedRequest = null;
        var requestCollection = new Mock<IMongoCollection<WorkFromHomeRequest>>();
        requestCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<WorkFromHomeRequest>>(), It.IsAny<WorkFromHomeRequest>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<WorkFromHomeRequest>, WorkFromHomeRequest, ReplaceOptions, CancellationToken>((_, r, _, _) => replacedRequest = r)
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        requestRepo.Setup(x => x.GetCollection()).Returns(requestCollection.Object);

        var attendanceRepo = new Mock<IMongoDbRepository<AttendanceModel>>();
        attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>())).ReturnsAsync((AttendanceModel?)null);
        AttendanceModel? insertedAttendance = null;
        var attendanceCollection = new Mock<IMongoCollection<AttendanceModel>>();
        attendanceCollection.Setup(x => x.InsertOneAsync(It.IsAny<AttendanceModel>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceModel, InsertOneOptions, CancellationToken>((a, _, _) => insertedAttendance = a)
            .Returns(Task.CompletedTask);
        attendanceRepo.Setup(x => x.GetCollection()).Returns(attendanceCollection.Object);

        var outboxRepo = new Mock<IMongoDbRepository<WorkFromHomeOutboxEvent>>();
        var outboxCollection = new Mock<IMongoCollection<WorkFromHomeOutboxEvent>>();
        outboxCollection.Setup(x => x.InsertManyAsync(It.IsAny<IEnumerable<WorkFromHomeOutboxEvent>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        outboxRepo.Setup(x => x.GetCollection()).Returns(outboxCollection.Object);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>()))
            .ReturnsAsync(new AttendanceStatusSetting { CompanyId = companyId, Code = "WFH", Name = "Work From Home", IsActive = true });
        statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        var mongoClient = new Mock<IMongoClient>();
        var sessionMock = new Mock<IClientSessionHandle>();
        sessionMock.Setup(x => x.WithTransactionAsync(
                It.IsAny<Func<IClientSessionHandle, CancellationToken, Task<bool>>>(),
                It.IsAny<TransactionOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateStandaloneMongoException());
        mongoClient.Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            policyRepo.Object,
            requestRepo.Object,
            outboxRepo.Object,
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            empRepo.Object,
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            statusRepo.Object,
            attendanceRepo.Object,
            Mock.Of<IMongoDbRepository<AttendanceDaySegment>>(),
            Mock.Of<IMongoDbRepository<AttendancePayrollException>>(),
            Mock.Of<IMongoDbRepository<MonthlyAttendanceSummary>>(),
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            Mock.Of<IAttendanceEditGuard>(),
            Mock.Of<IEffectiveOfficeScheduleService>(),
            Mock.Of<IAttendanceInitializationService>(),
            roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            mongoClient.Object,
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        var result = await service.Decide("req-decide-1", "Approved", new WorkFromHomeDecisionDto { Version = 1, RemarksText = "Approved by manager" });

        Assert.True(result.Success, result.Message);
        Assert.NotNull(replacedRequest);
        Assert.Equal("Approved", replacedRequest.Status);
        Assert.Equal(2, replacedRequest.Version);
        Assert.NotNull(insertedAttendance);
        Assert.Equal("WFH", insertedAttendance.Status);
    }

    [Fact]
    public async Task Cancel_WhenMongoTransactionsFailOnStandalone_ApprovedRequestCleansUpAttendanceSuccessfully()
    {
        const string companyId = "c1";
        const string userId = "u1";
        var targetDate = DateTime.UtcNow.Date.AddDays(7);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.company_id.ToString(), companyId),
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.user_id.ToString(), userId),
            new Claim(ClaimTypes.Role, "Employee")
        ], "TestAuth"));
        httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        var roles = new Mock<IRoleService>();
        roles.Setup(x => x.IsModuleAccessibleForCompany("Work_From_Home", companyId)).ReturnsAsync(true);
        roles.Setup(x => x.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserCheckModel>())).ReturnsAsync(true);

        var request = new WorkFromHomeRequest
        {
            CompanyId = companyId,
            RequestId = "req-cancel-1",
            UserId = userId,
            EmployeeId = "EMP-001",
            Status = "Approved",
            Version = 2,
            FromDate = targetDate,
            ToDate = targetDate,
            DurationType = "FullDay"
        };

        var employee = new EmpUser
        {
            CompanyId = companyId,
            UserId = userId,
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Status = true
        };
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>())).ReturnsAsync(employee);

        var existingAttendance = new AttendanceModel
        {
            AttendanceId = "att-cancel-1",
            CompanyId = companyId,
            UserId = userId,
            Date = targetDate,
            Status = "WFH",
            SourceType = "WFH_REQUEST",
            SourceId = "req-cancel-1",
            Remarks = "Generated from approved WFH request req-cancel-1",
            Version = 1
        };

        var requestRepo = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>())).ReturnsAsync(request);
        WorkFromHomeRequest? cancelledRequest = null;
        var requestCollection = new Mock<IMongoCollection<WorkFromHomeRequest>>();
        requestCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<WorkFromHomeRequest>>(), It.IsAny<WorkFromHomeRequest>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<WorkFromHomeRequest>, WorkFromHomeRequest, ReplaceOptions, CancellationToken>((_, r, _, _) => cancelledRequest = r)
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        requestRepo.Setup(x => x.GetCollection()).Returns(requestCollection.Object);

        var attendanceRepo = new Mock<IMongoDbRepository<AttendanceModel>>();
        attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>())).ReturnsAsync(existingAttendance);
        var attendanceCollection = new Mock<IMongoCollection<AttendanceModel>>();
        var attendanceCursor = new Mock<IAsyncCursor<AttendanceModel>>();
        attendanceCursor.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>())).Returns(true).Returns(false);
        attendanceCursor.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true).ReturnsAsync(false);
        attendanceCursor.Setup(x => x.Current).Returns([existingAttendance]);
        attendanceCollection.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<FindOptions<AttendanceModel, AttendanceModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attendanceCursor.Object);
        var deletedAttendance = false;
        attendanceCollection.Setup(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<CancellationToken>()))
            .Callback(() => deletedAttendance = true)
            .ReturnsAsync(new DeleteResult.Acknowledged(1));
        attendanceRepo.Setup(x => x.GetCollection()).Returns(attendanceCollection.Object);

        var outboxRepo = new Mock<IMongoDbRepository<WorkFromHomeOutboxEvent>>();
        var outboxCollection = new Mock<IMongoCollection<WorkFromHomeOutboxEvent>>();
        outboxCollection.Setup(x => x.InsertManyAsync(It.IsAny<IEnumerable<WorkFromHomeOutboxEvent>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        outboxRepo.Setup(x => x.GetCollection()).Returns(outboxCollection.Object);

        var mongoClient = new Mock<IMongoClient>();
        var sessionMock = new Mock<IClientSessionHandle>();
        sessionMock.Setup(x => x.WithTransactionAsync(
                It.IsAny<Func<IClientSessionHandle, CancellationToken, Task<bool>>>(),
                It.IsAny<TransactionOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(CreateStandaloneMongoException());
        mongoClient.Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionMock.Object);

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomePolicy>>(),
            requestRepo.Object,
            outboxRepo.Object,
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            empRepo.Object,
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            Mock.Of<IMongoDbRepository<AttendanceStatusSetting>>(),
            attendanceRepo.Object,
            Mock.Of<IMongoDbRepository<AttendanceDaySegment>>(),
            Mock.Of<IMongoDbRepository<AttendancePayrollException>>(),
            Mock.Of<IMongoDbRepository<MonthlyAttendanceSummary>>(),
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            Mock.Of<IAttendanceEditGuard>(),
            Mock.Of<IEffectiveOfficeScheduleService>(),
            Mock.Of<IAttendanceInitializationService>(),
            roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            mongoClient.Object,
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        var result = await service.Cancel("req-cancel-1", new WorkFromHomeCancelDto { Version = 2, RemarksText = "Change of plan" });

        Assert.True(result.Success, result.Message);
        Assert.NotNull(cancelledRequest);
        Assert.Equal("Cancelled", cancelledRequest.Status);
        Assert.True(deletedAttendance);
    }

    [Fact]
    public async Task AdminGrantAsync_WhenAttendanceAlreadyExistsAsHD_OverridesAttendanceRecordAndSegmentsSuccessfully()
    {
        var companyId = "company-admin-grant-test";
        var adminUserId = "admin-1";
        var targetUserId = "user-satyam";
        var date = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new System.Security.Claims.Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.company_id.ToString(), companyId),
            new System.Security.Claims.Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.user_id.ToString(), adminUserId)
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(identity);
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var roles = new Mock<IRoleService>();
        roles.Setup(r => r.IsModuleAccessibleForCompany(It.IsAny<string>(), companyId))
            .ReturnsAsync(true);
        roles.Setup(r => r.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), adminUserId, companyId))
            .ReturnsAsync(true);

        var employee = new EmpUser
        {
            CompanyId = companyId,
            UserId = targetUserId,
            EmployeeId = "EMP-SATYAM",
            FirstName = "Satyam",
            LastName = "Dubey",
            Email = "satyam@example.com",
            DateOfJoining = "2025-01-01",
            Status = true
        };
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([employee]);
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        var policy = new WorkFromHomePolicy
        {
            CompanyId = companyId,
            IsEnabled = true,
            ApplyToAllEmployees = true,
            AllowHalfDay = true,
            AllowMixedDay = true,
            FullDayAttendanceStatusCode = "WFH",
            HalfDayAttendanceStatusCode = "WFH-HD",
            WfhWfoAttendanceStatusCode = "WFH+WFO",
            WfhHdAttendanceStatusCode = "WFH-HD",
            OfficeStartTime = new TimeSpan(9, 30, 0),
            OfficeEndTime = new TimeSpan(18, 30, 0)
        };
        var policyRepo = new Mock<IMongoDbRepository<WorkFromHomePolicy>>();
        policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var requestRepo = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        requestRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), true))
            .ReturnsAsync([]);
        var requestCollection = new Mock<IMongoCollection<WorkFromHomeRequest>>();
        requestCollection.Setup(x => x.InsertOneAsync(It.IsAny<WorkFromHomeRequest>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        requestRepo.Setup(x => x.GetCollection()).Returns(requestCollection.Object);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>()))
            .ReturnsAsync(true);

        var existingAttendance = new AttendanceModel
        {
            AttendanceId = "att-hd-1",
            CompanyId = companyId,
            UserId = targetUserId,
            EmployeeId = "EMP-SATYAM",
            Date = date,
            Status = "HD",
            SourceType = "ADMIN_MANUAL",
            SourceId = null,
            Remarks = "etretyretretrelt",
            Version = 1
        };

        var attendanceRepo = new Mock<IMongoDbRepository<AttendanceModel>>();
        attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(existingAttendance);
        AttendanceModel? replacedRow = null;
        var attendanceCollection = new Mock<IMongoCollection<AttendanceModel>>();
        attendanceCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<AttendanceModel>, AttendanceModel, ReplaceOptions, CancellationToken>((_, r, _, _) => replacedRow = r)
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        attendanceRepo.Setup(x => x.GetCollection()).Returns(attendanceCollection.Object);

        var segmentRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        var segmentList = new List<AttendanceDaySegment>();
        segmentRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), true))
            .ReturnsAsync(segmentList);
        segmentRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<AttendanceDaySegment, bool>> expr, bool _, bool _) => segmentList.AsQueryable().FirstOrDefault(expr));
        segmentRepo.Setup(x => x.DeleteAll(It.IsAny<FilterDefinition<AttendanceDaySegment>>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        segmentRepo.Setup(x => x.AddOne(It.IsAny<AttendanceDaySegment>()))
            .Callback<AttendanceDaySegment>(s => segmentList.Add(s))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var segmentCollection = new Mock<IMongoCollection<AttendanceDaySegment>>();
        segmentCollection.Setup(x => x.InsertOneAsync(It.IsAny<AttendanceDaySegment>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceDaySegment, InsertOneOptions, CancellationToken>((s, _, _) => segmentList.Add(s))
            .Returns(Task.CompletedTask);
        segmentRepo.Setup(x => x.GetCollection()).Returns(segmentCollection.Object);

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            policyRepo.Object,
            requestRepo.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeOutboxEvent>>(),
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            empRepo.Object,
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            statusRepo.Object,
            attendanceRepo.Object,
            segmentRepo.Object,
            Mock.Of<IMongoDbRepository<AttendancePayrollException>>(),
            Mock.Of<IMongoDbRepository<MonthlyAttendanceSummary>>(),
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            Mock.Of<IAttendanceEditGuard>(),
            Mock.Of<IEffectiveOfficeScheduleService>(),
            Mock.Of<IAttendanceInitializationService>(),
            roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoClient>(),
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        var grantDto = new WorkFromHomeAdminGrantDto
        {
            TargetUserId = targetUserId,
            AttendanceDate = date,
            GrantType = "WfhWfo",
            Remarks = "Approved hybrid schedule",
            OfficeCheckInTime = new TimeSpan(14, 0, 0),
            OfficeCheckOutTime = new TimeSpan(18, 30, 0)
        };

        var result = await service.AdminGrantAsync(grantDto);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(replacedRow);
        Assert.Equal("WFH+WFO", replacedRow.Status);
        Assert.Equal("WFH_REQUEST", replacedRow.SourceType);
        Assert.Equal("Administrative WFH grant: Approved hybrid schedule", replacedRow.Remarks);
        Assert.Equal(2, segmentList.Count);
        Assert.Contains(segmentList, s => s.Segment == "FIRST_HALF" && s.Status == "WFH");
        Assert.Contains(segmentList, s => s.Segment == "SECOND_HALF" && s.Status == "P");
    }

    [Theory]
    [InlineData(9, 30, true)]   // Shift start: allowed
    [InlineData(11, 0, true)]   // Mid-morning: allowed
    [InlineData(13, 29, true)]  // 1 min before cutoff: allowed
    [InlineData(13, 30, false)] // At boundary: rejected (first-half closed)
    [InlineData(14, 0, false)]  // Afternoon: rejected
    [InlineData(18, 14, false)] // Evening: rejected
    public void IsHalfDayClockInWithinRequestedSegment_FirstHalf_AllowedBeforeBoundary_RejectedAfter(int hour, int minute, bool expectedAllowed)
    {
        var officeStart = new TimeSpan(9, 30, 0);
        var clockInTime = new TimeSpan(hour, minute, 0);

        var allowed = WorkFromHomeService.IsHalfDayClockInWithinRequestedSegment(
            "FirstHalf", clockInTime, officeStart, 480, 8m);

        Assert.Equal(expectedAllowed, allowed);
    }

    [Theory]
    [InlineData(10, 0, false)]  // Morning: rejected (second-half not open yet)
    [InlineData(13, 29, false)] // 1 min before cutoff: rejected
    [InlineData(13, 30, true)]  // At boundary: allowed (second-half opens)
    [InlineData(14, 0, true)]   // Afternoon: allowed
    [InlineData(18, 0, true)]   // Late afternoon: allowed
    public void IsHalfDayClockInWithinRequestedSegment_SecondHalf_RejectedBeforeBoundary_AllowedAfter(int hour, int minute, bool expectedAllowed)
    {
        var officeStart = new TimeSpan(9, 30, 0);
        var clockInTime = new TimeSpan(hour, minute, 0);

        var allowed = WorkFromHomeService.IsHalfDayClockInWithinRequestedSegment(
            "SecondHalf", clockInTime, officeStart, 480, 8m);

        Assert.Equal(expectedAllowed, allowed);
    }

    [Fact]
    public async Task AdminGrantAsync_WithWfhHd_ReplacesManualRecordAndSetsHalfDayCompanionSegment()
    {
        var companyId = "company-admin-test";
        var adminUserId = "admin-user-id";
        var targetUserId = "target-emp-id";
        var date = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("company_id", companyId),
            new Claim("user_id", adminUserId),
            new Claim("CompanyId", companyId),
            new Claim("UserId", adminUserId),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var policyRepo = new Mock<IMongoDbRepository<WorkFromHomePolicy>>();
        policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new WorkFromHomePolicy
            {
                CompanyId = companyId,
                IsEnabled = true,
                AllowHalfDay = true,
                AllowMixedDay = true,
                FullDayAttendanceStatusCode = "WFH",
                HalfDayAttendanceStatusCode = "HD",
                WfhWfoAttendanceStatusCode = "WFH+WFO",
                WfhHdAttendanceStatusCode = "WFH-HD",
                FullDayMinimumHours = 8m,
                HalfDayMinimumHours = 4m,
                OfficeStartTime = new TimeSpan(9, 30, 0),
                OfficeEndTime = new TimeSpan(18, 30, 0)
            });

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var targetEmployee = new EmpUser
        {
            CompanyId = companyId,
            UserId = targetUserId,
            FirstName = "Test",
            LastName = "Employee",
            Email = "test.employee@example.invalid",
            EmployeeId = "EMP-001",
            Status = true,
            DateOfJoining = "2025-01-01"
        };
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(targetEmployee);
        empRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { targetEmployee });

        var roles = new Mock<IRoleService>();
        roles.Setup(r => r.IsModuleAccessibleForCompany(It.IsAny<string>(), companyId)).ReturnsAsync(true);
        roles.Setup(r => r.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserCheckModel>())).ReturnsAsync(true);

        var requestRepo = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        requestRepo.Setup(x => x.AddOne(It.IsAny<WorkFromHomeRequest>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        requestRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), true))
            .ReturnsAsync(new List<WorkFromHomeRequest>());
        var requestCollection = new Mock<IMongoCollection<WorkFromHomeRequest>>();
        requestCollection.Setup(x => x.InsertOneAsync(It.IsAny<WorkFromHomeRequest>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        requestRepo.Setup(x => x.GetCollection()).Returns(requestCollection.Object);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>()))
            .ReturnsAsync(true);

        AttendanceModel? replacedRow = null;
        var existingManualHd = new AttendanceModel
        {
            AttendanceId = "existing-att-id",
            CompanyId = companyId,
            UserId = targetUserId,
            EmployeeId = "EMP-001",
            Date = date,
            Status = "HD",
            SourceType = "ADMIN_MANUAL",
            Remarks = "Previous manual half day",
            Version = 3
        };

        var attendanceRepo = new Mock<IMongoDbRepository<AttendanceModel>>();
        attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(existingManualHd);

        var attendanceCollection = new Mock<IMongoCollection<AttendanceModel>>();
        attendanceCollection.Setup(x => x.ReplaceOneAsync(
                It.IsAny<FilterDefinition<AttendanceModel>>(),
                It.IsAny<AttendanceModel>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<AttendanceModel>, AttendanceModel, ReplaceOptions, CancellationToken>((_, row, _, _) => replacedRow = row)
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        attendanceRepo.Setup(x => x.GetCollection()).Returns(attendanceCollection.Object);

        var segmentRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        var segmentList = new List<AttendanceDaySegment>();
        segmentRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), true))
            .ReturnsAsync(segmentList);
        segmentRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<AttendanceDaySegment, bool>> expr, bool _, bool _) => segmentList.AsQueryable().FirstOrDefault(expr));
        segmentRepo.Setup(x => x.DeleteAll(It.IsAny<FilterDefinition<AttendanceDaySegment>>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        segmentRepo.Setup(x => x.AddOne(It.IsAny<AttendanceDaySegment>()))
            .Callback<AttendanceDaySegment>(s => segmentList.Add(s))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var segmentCollection = new Mock<IMongoCollection<AttendanceDaySegment>>();
        segmentCollection.Setup(x => x.InsertOneAsync(It.IsAny<AttendanceDaySegment>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceDaySegment, InsertOneOptions, CancellationToken>((s, _, _) => segmentList.Add(s))
            .Returns(Task.CompletedTask);
        segmentRepo.Setup(x => x.GetCollection()).Returns(segmentCollection.Object);

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            policyRepo.Object,
            requestRepo.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeOutboxEvent>>(),
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            empRepo.Object,
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            statusRepo.Object,
            attendanceRepo.Object,
            segmentRepo.Object,
            Mock.Of<IMongoDbRepository<AttendancePayrollException>>(),
            Mock.Of<IMongoDbRepository<MonthlyAttendanceSummary>>(),
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            Mock.Of<IAttendanceEditGuard>(),
            Mock.Of<IEffectiveOfficeScheduleService>(),
            Mock.Of<IAttendanceInitializationService>(),
            roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoClient>(),
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        var grantDto = new WorkFromHomeAdminGrantDto
        {
            TargetUserId = targetUserId,
            AttendanceDate = date,
            GrantType = "WfhHd",
            Remarks = "First half WFH, second half Half-Day leave",
            OfficeCheckInTime = null,
            OfficeCheckOutTime = null
        };

        var result = await service.AdminGrantAsync(grantDto);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(replacedRow);
        Assert.Equal("WFH-HD", replacedRow.Status);
        Assert.Equal("WFH_REQUEST", replacedRow.SourceType);
        Assert.Equal(2, segmentList.Count);
        Assert.Contains(segmentList, s => s.Segment == "FIRST_HALF" && s.Status == "WFH");
        Assert.Contains(segmentList, s => s.Segment == "SECOND_HALF" && s.Status == "HD");
    }

    [Fact]
    public async Task Create_HalfDayWfh_WithMultiDayRange_FailsValidationWithSingleDateError()
    {
        var companyId = "company-audit-test";
        var userId = "user-audit-emp-01";
        var date = new DateOnly(2026, 11, 2);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.company_id.ToString(), companyId),
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.user_id.ToString(), userId),
            new Claim(ClaimTypes.Role, "Employee")
        ], "TestAuth"));
        httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        var policyRepo = new Mock<IMongoDbRepository<WorkFromHomePolicy>>();
        policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(new WorkFromHomePolicy
            {
                CompanyId = companyId,
                IsEnabled = true,
                AllowHalfDay = true,
                FullDayMinimumHours = 8m,
                HalfDayMinimumHours = 4m,
                TimeZoneId = "India Standard Time"
            });

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var testEmp = new EmpUser
        {
            CompanyId = companyId,
            UserId = userId,
            FirstName = "Audit",
            LastName = "Employee",
            Email = "audit.employee@example.com",
            EmployeeId = "EMP-001",
            Status = true,
            DateOfJoining = "2025-01-01"
        };
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(testEmp);
        empRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { testEmp });

        var rolesMock = new Mock<IRoleService>();
        rolesMock.Setup(r => r.IsModuleAccessibleForCompany(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        rolesMock.Setup(r => r.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserCheckModel>())).ReturnsAsync(true);

        var requestRepo = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        requestRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Enumerable.Empty<WorkFromHomeRequest>());

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            policyRepo.Object,
            requestRepo.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeOutboxEvent>>(),
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            empRepo.Object,
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
            rolesMock.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoClient>(),
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        var dto = new WorkFromHomeRequestDto
        {
            FromDate = date,
            ToDate = date.AddDays(1), // Multi-day range with half-day duration
            DurationType = "FirstHalf",
            ReasonCode = "PERSONAL",
            ReasonText = "Doctor appointment across multiple days"
        };

        var result = await service.Create(dto, "idempotency-key-multi-halfday-test");

        Assert.False(result.Success);
        Assert.Contains("WFH_HALF_DAY_SINGLE_DATE", result.Message);
        Assert.Contains("Half-day Work From Home requests can only be submitted for a single day.", result.Message);
    }
    [Fact]
    public async Task AdminGrantAsync_WhenPriorWfhPunchesExist_CarriesPunchesForwardAndAvoidsRePromptingClockIn()
    {
        var companyId = "company-admin-carryover";
        var adminUserId = "admin-user-id";
        var targetUserId = "target-emp-id";
        var date = new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("company_id", companyId),
            new Claim("user_id", adminUserId),
            new Claim("CompanyId", companyId),
            new Claim("UserId", adminUserId),
            new Claim(ClaimTypes.Role, "Admin")
        ], "TestAuth"));
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var policyRepo = new Mock<IMongoDbRepository<WorkFromHomePolicy>>();
        policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new WorkFromHomePolicy
            {
                CompanyId = companyId,
                IsEnabled = true,
                AllowHalfDay = true,
                AllowMixedDay = true,
                FullDayAttendanceStatusCode = "WFH",
                HalfDayAttendanceStatusCode = "WFH-HD",
                WfhWfoAttendanceStatusCode = "WFH+WFO",
                FullDayMinimumHours = 8m,
                HalfDayMinimumHours = 4m,
                OfficeStartTime = new TimeSpan(9, 0, 0),
                OfficeEndTime = new TimeSpan(18, 0, 0)
            });

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var targetEmployee = new EmpUser
        {
            CompanyId = companyId,
            UserId = targetUserId,
            FirstName = "Aarav",
            LastName = "Sharma",
            Email = "aarav.sharma@example.invalid",
            EmployeeId = "EMP1001",
            Status = true,
            DateOfJoining = "2025-01-01"
        };
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(targetEmployee);
        empRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { targetEmployee });

        var roles = new Mock<IRoleService>();
        roles.Setup(r => r.IsModuleAccessibleForCompany(It.IsAny<string>(), companyId)).ReturnsAsync(true);
        roles.Setup(r => r.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserCheckModel>())).ReturnsAsync(true);

        var requestRepo = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        requestRepo.Setup(x => x.AddOne(It.IsAny<WorkFromHomeRequest>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        requestRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), true))
            .ReturnsAsync(new List<WorkFromHomeRequest>());
        var requestCollection = new Mock<IMongoCollection<WorkFromHomeRequest>>();
        requestCollection.Setup(x => x.InsertOneAsync(It.IsAny<WorkFromHomeRequest>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        requestRepo.Setup(x => x.GetCollection()).Returns(requestCollection.Object);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>()))
            .ReturnsAsync(true);

        AttendanceModel? replacedRow = null;
        var existingRow = new AttendanceModel
        {
            AttendanceId = "existing-row-id",
            CompanyId = companyId,
            UserId = targetUserId,
            EmployeeId = "EMP1001",
            Date = date,
            Status = "WFH+WFO",
            SourceType = "WFH_REQUEST",
            Version = 1
        };

        var attendanceRepo = new Mock<IMongoDbRepository<AttendanceModel>>();
        attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(existingRow);
        var attendanceCollection = new Mock<IMongoCollection<AttendanceModel>>();
        attendanceCollection.Setup(x => x.ReplaceOneAsync(
                It.IsAny<FilterDefinition<AttendanceModel>>(),
                It.IsAny<AttendanceModel>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<AttendanceModel>, AttendanceModel, ReplaceOptions, CancellationToken>((_, row, _, _) => replacedRow = row)
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        attendanceRepo.Setup(x => x.GetCollection()).Returns(attendanceCollection.Object);

        // Pre-existing completed punches from earlier today (e.g. 13:30 to 18:00 = 4.5h)
        var priorCheckIn = date.Add(new TimeSpan(13, 30, 0));
        var priorCheckOut = date.Add(new TimeSpan(18, 0, 0));
        var priorWfhSegment = new AttendanceDaySegment
        {
            AttendanceDaySegmentId = "existing-seg-1",
            CompanyId = companyId,
            UserId = targetUserId,
            EmployeeId = "EMP1001",
            Date = date,
            Segment = "SECOND_HALF",
            Status = "WFH",
            SourceType = "WFH_REQUEST",
            CheckInTime = priorCheckIn,
            CheckOutTime = priorCheckOut,
            TotalHours = 4.5m,
            Remarks = "Checked out through employee WFH portal (4.50 hr(s))"
        };

        var segmentList = new List<AttendanceDaySegment> { priorWfhSegment };
        var segmentRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        segmentRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(segmentList);
        segmentRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<AttendanceDaySegment, bool>> expr, bool _, bool _) => segmentList.AsQueryable().FirstOrDefault(expr));
        segmentRepo.Setup(x => x.DeleteAll(It.IsAny<FilterDefinition<AttendanceDaySegment>>()))
            .Callback(() => segmentList.Clear())
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        segmentRepo.Setup(x => x.AddOne(It.IsAny<AttendanceDaySegment>()))
            .Callback<AttendanceDaySegment>(s => segmentList.Add(s))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var segmentCollection = new Mock<IMongoCollection<AttendanceDaySegment>>();
        segmentCollection.Setup(x => x.InsertOneAsync(It.IsAny<AttendanceDaySegment>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceDaySegment, InsertOneOptions, CancellationToken>((s, _, _) => segmentList.Add(s))
            .Returns(Task.CompletedTask);
        segmentCollection.Setup(x => x.ReplaceOneAsync(
                It.IsAny<FilterDefinition<AttendanceDaySegment>>(),
                It.IsAny<AttendanceDaySegment>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        segmentRepo.Setup(x => x.GetCollection()).Returns(segmentCollection.Object);

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            policyRepo.Object,
            requestRepo.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeOutboxEvent>>(),
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            empRepo.Object,
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            statusRepo.Object,
            attendanceRepo.Object,
            segmentRepo.Object,
            Mock.Of<IMongoDbRepository<AttendancePayrollException>>(),
            Mock.Of<IMongoDbRepository<MonthlyAttendanceSummary>>(),
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            Mock.Of<IAttendanceEditGuard>(),
            Mock.Of<IEffectiveOfficeScheduleService>(),
            Mock.Of<IAttendanceInitializationService>(),
            roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoClient>(),
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        // HR overrides WFH+WFO on the calendar without passing new WFH timings
        var grantDto = new WorkFromHomeAdminGrantDto
        {
            TargetUserId = targetUserId,
            AttendanceDate = date,
            GrantType = "WfoWfh",
            Remarks = "Administrative override preserving verified punches",
            OfficeCheckInTime = new TimeSpan(9, 0, 0),
            OfficeCheckOutTime = new TimeSpan(13, 30, 0)
        };

        var result = await service.AdminGrantAsync(grantDto);

        Assert.True(result.Success, result.Message);
        var wfhSeg = segmentList.FirstOrDefault(s => s.Segment == "SECOND_HALF");
        Assert.NotNull(wfhSeg);
        Assert.Equal(priorCheckIn, wfhSeg.CheckInTime);
        Assert.Equal(priorCheckOut, wfhSeg.CheckOutTime);
        Assert.Equal(4.5m, wfhSeg.TotalHours);
        Assert.Equal("WFH", wfhSeg.Status);

        Assert.NotNull(replacedRow);
        Assert.Equal(9.0m, replacedRow.TotalHours);
    }

    [Fact]
    public async Task AdminGrantAsync_WhenLeaveExistsOnSameDate_RejectsHybridWfoWfh()
    {
        var companyId = "company-admin-overlap";
        var adminUserId = "admin-user-id";
        var targetUserId = "target-emp-id";
        var date = new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("company_id", companyId),
            new Claim("user_id", adminUserId),
            new Claim(ClaimTypes.Role, "Admin")
        ], "TestAuth"));
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var policyRepo = new Mock<IMongoDbRepository<WorkFromHomePolicy>>();
        policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new WorkFromHomePolicy
            {
                CompanyId = companyId,
                IsEnabled = true,
                AllowHalfDay = true,
                AllowMixedDay = true,
                FullDayAttendanceStatusCode = "WFH",
                HalfDayAttendanceStatusCode = "WFH-HD",
                WfhWfoAttendanceStatusCode = "WFH+WFO"
            });

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser>
            {
                new EmpUser
                {
                    CompanyId = companyId,
                    UserId = targetUserId,
                    FirstName = "Aarav",
                    LastName = "Sharma",
                    Email = "aarav@test.invalid",
                    Status = true,
                    DateOfJoining = "2025-01-01"
                }
            });
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new EmpUser
            {
                CompanyId = companyId,
                UserId = targetUserId,
                FirstName = "Aarav",
                LastName = "Sharma",
                Email = "aarav@test.invalid",
                Status = true,
                DateOfJoining = "2025-01-01"
            });

        var leaveRepo = new Mock<IMongoDbRepository<LeaveRequest>>();
        leaveRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<LeaveRequest>
            {
                new LeaveRequest
                {
                    CompanyId = companyId,
                    EmployeeId = targetUserId,
                    LeavePolicyId = "policy-1",
                    Reason = "Sick",
                    StartDate = date,
                    EndDate = date,
                    Status = EnumsHelper.LeaveRequestStatus.Accepted,
                    IsHalfDay = true,
                    HalfDayPeriod = "FIRST_HALF"
                }
            });

        var roles = new Mock<IRoleService>();
        roles.Setup(r => r.IsModuleAccessibleForCompany(It.IsAny<string>(), companyId)).ReturnsAsync(true);
        roles.Setup(r => r.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserCheckModel>())).ReturnsAsync(true);

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            policyRepo.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeRequest>>(),
            Mock.Of<IMongoDbRepository<WorkFromHomeOutboxEvent>>(),
            leaveRepo.Object,
            empRepo.Object,
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
            roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoClient>(),
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        var result = await service.AdminGrantAsync(new WorkFromHomeAdminGrantDto
        {
            TargetUserId = targetUserId,
            AttendanceDate = date,
            GrantType = "WfoWfh",
            Remarks = "Attempt hybrid when leave exists"
        });

        Assert.False(result.Success);
        Assert.Contains("WFH_LEAVE_OVERLAP", result.Message);
    }

    [Fact]
    public async Task AdminGrantAsync_WhenWfhHdWithSickLeave_CreatesHalfWfhAndHalfLeaveSegments()
    {
        var companyId = "company-admin-wfhhd";
        var adminUserId = "admin-user-id";
        var targetUserId = "target-emp-id";
        var date = new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("company_id", companyId),
            new Claim("user_id", adminUserId),
            new Claim(ClaimTypes.Role, "Admin")
        ], "TestAuth"));
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var policyRepo = new Mock<IMongoDbRepository<WorkFromHomePolicy>>();
        policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new WorkFromHomePolicy
            {
                CompanyId = companyId,
                IsEnabled = true,
                AllowHalfDay = true,
                AllowMixedDay = true,
                FullDayAttendanceStatusCode = "WFH",
                HalfDayAttendanceStatusCode = "WFH-HD",
                WfhWfoAttendanceStatusCode = "WFH+WFO"
            });

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser>
            {
                new EmpUser
                {
                    CompanyId = companyId,
                    UserId = targetUserId,
                    FirstName = "Aarav",
                    LastName = "Sharma",
                    Email = "aarav@test.invalid",
                    Status = true,
                    DateOfJoining = "2025-01-01"
                }
            });
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new EmpUser
            {
                CompanyId = companyId,
                UserId = targetUserId,
                FirstName = "Aarav",
                LastName = "Sharma",
                Email = "aarav@test.invalid",
                Status = true,
                DateOfJoining = "2025-01-01"
            });

        var leaveRepo = new Mock<IMongoDbRepository<LeaveRequest>>();
        leaveRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<LeaveRequest>());

        var requestRepo = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        requestRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<WorkFromHomeRequest>());
        var requestCollection = new Mock<IMongoCollection<WorkFromHomeRequest>>();
        requestRepo.Setup(x => x.GetCollection()).Returns(requestCollection.Object);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(true);
        statusRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceStatusSetting>
            {
                new AttendanceStatusSetting { CompanyId = companyId, Code = "WFH", Name = "Work From Home", IsActive = true },
                new AttendanceStatusSetting { CompanyId = companyId, Code = "WFH-HD", Name = "WFH Half Day", IsActive = true },
                new AttendanceStatusSetting { CompanyId = companyId, Code = "SL", Name = "Sick Leave", IsActive = true }
            });

        var segmentList = new List<AttendanceDaySegment>();
        var segmentRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        segmentRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(segmentList);
        segmentRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<AttendanceDaySegment, bool>> expr, bool _, bool _) => segmentList.AsQueryable().FirstOrDefault(expr));
        segmentRepo.Setup(x => x.DeleteAll(It.IsAny<FilterDefinition<AttendanceDaySegment>>()))
            .Callback(() => segmentList.Clear())
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        segmentRepo.Setup(x => x.AddOne(It.IsAny<AttendanceDaySegment>()))
            .Callback<AttendanceDaySegment>(s => segmentList.Add(s))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var segmentCollection = new Mock<IMongoCollection<AttendanceDaySegment>>();
        segmentCollection.Setup(x => x.InsertOneAsync(It.IsAny<AttendanceDaySegment>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceDaySegment, InsertOneOptions, CancellationToken>((s, _, _) => segmentList.Add(s))
            .Returns(Task.CompletedTask);
        segmentRepo.Setup(x => x.GetCollection()).Returns(segmentCollection.Object);

        var attendanceRepo = new Mock<IMongoDbRepository<AttendanceModel>>();
        var attendanceCollection = new Mock<IMongoCollection<AttendanceModel>>();
        attendanceRepo.Setup(x => x.GetCollection()).Returns(attendanceCollection.Object);

        var roles = new Mock<IRoleService>();
        roles.Setup(r => r.IsModuleAccessibleForCompany(It.IsAny<string>(), companyId)).ReturnsAsync(true);
        roles.Setup(r => r.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserCheckModel>())).ReturnsAsync(true);

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            policyRepo.Object,
            requestRepo.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeOutboxEvent>>(),
            leaveRepo.Object,
            empRepo.Object,
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            statusRepo.Object,
            attendanceRepo.Object,
            segmentRepo.Object,
            Mock.Of<IMongoDbRepository<AttendancePayrollException>>(),
            Mock.Of<IMongoDbRepository<MonthlyAttendanceSummary>>(),
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            Mock.Of<IAttendanceEditGuard>(),
            Mock.Of<IEffectiveOfficeScheduleService>(),
            Mock.Of<IAttendanceInitializationService>(),
            roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoClient>(),
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        var grantDto = new WorkFromHomeAdminGrantDto
        {
            TargetUserId = targetUserId,
            AttendanceDate = date,
            GrantType = "WfhHd",
            LeaveStatusCode = "SL",
            Remarks = "Half day WFH with afternoon Sick Leave",
            WfhCheckInTime = new TimeSpan(9, 0, 0),
            WfhCheckOutTime = new TimeSpan(13, 30, 0)
        };

        var result = await service.AdminGrantAsync(grantDto);

        Assert.True(result.Success, result.Message);
        var leaveSeg = segmentList.FirstOrDefault(s => s.Segment == "SECOND_HALF");
        Assert.NotNull(leaveSeg);
        Assert.Equal("SL", leaveSeg.Status);

        var wfhSeg = segmentList.FirstOrDefault(s => s.Segment == "FIRST_HALF");
        Assert.NotNull(wfhSeg);
        Assert.Equal("WFH", wfhSeg.Status);
        Assert.Equal(4.5m, wfhSeg.TotalHours);
    }

    [Fact]
    public async Task AdminGrantAsync_RejectsInvalidTimings_WhenCheckOutNotAfterCheckIn()
    {
        var companyId = "company-admin-grant-invalid-time";
        var adminUserId = "admin-user-id";
        var targetUserId = "employee-user-id";
        var date = new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        var claims = new[]
        {
            new System.Security.Claims.Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.company_id.ToString(), companyId),
            new System.Security.Claims.Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.user_id.ToString(), adminUserId)
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        var policy = new WorkFromHomePolicy
        {
            CompanyId = companyId,
            IsEnabled = true,
            ApplyToAllEmployees = true,
            AllowHalfDay = true,
            FullDayMinimumHours = 8.0m,
            HalfDayMinimumHours = 4.0m
        };
        var policyRepo = new Mock<IMongoDbRepository<WorkFromHomePolicy>>();
        policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var employee = new EmpUser
        {
            CompanyId = companyId,
            UserId = targetUserId,
            EmployeeId = "EMP1001",
            FirstName = "Aarav",
            LastName = "Sharma",
            Email = "aarav@test.invalid",
            Status = true,
            DateOfJoining = "2025-01-01"
        };
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([employee]);
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        var roles = new Mock<IRoleService>();
        roles.Setup(r => r.IsModuleAccessibleForCompany(It.IsAny<string>(), companyId)).ReturnsAsync(true);
        roles.Setup(r => r.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), adminUserId, companyId)).ReturnsAsync(true);

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            policyRepo.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeRequest>>(),
            Mock.Of<IMongoDbRepository<WorkFromHomeOutboxEvent>>(),
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            empRepo.Object,
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
            roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoClient>(),
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        var grantDto = new WorkFromHomeAdminGrantDto
        {
            TargetUserId = targetUserId,
            AttendanceDate = date,
            GrantType = "FullDay",
            Remarks = "Same punch in and out entered",
            WfhCheckInTime = new TimeSpan(17, 5, 0),
            WfhCheckOutTime = new TimeSpan(17, 5, 0)
        };

        var result = await service.AdminGrantAsync(grantDto);

        Assert.False(result.Success);
        Assert.Equal("WFH_INVALID_TIMINGS", result.ErrorCode);
    }

    [Fact]
    public async Task AdminGrantAsync_RejectsInsufficientHours_WhenFullDayPunchesUnder4Hours()
    {
        var companyId = "company-admin-grant-under-hours";
        var adminUserId = "admin-user-id";
        var targetUserId = "employee-user-id";
        var date = new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        var claims = new[]
        {
            new System.Security.Claims.Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.company_id.ToString(), companyId),
            new System.Security.Claims.Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.user_id.ToString(), adminUserId)
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        var policy = new WorkFromHomePolicy
        {
            CompanyId = companyId,
            IsEnabled = true,
            ApplyToAllEmployees = true,
            AllowHalfDay = true,
            FullDayMinimumHours = 8.0m,
            HalfDayMinimumHours = 4.0m
        };
        var policyRepo = new Mock<IMongoDbRepository<WorkFromHomePolicy>>();
        policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var employee = new EmpUser
        {
            CompanyId = companyId,
            UserId = targetUserId,
            EmployeeId = "EMP1001",
            FirstName = "Aarav",
            LastName = "Sharma",
            Email = "aarav@test.invalid",
            Status = true,
            DateOfJoining = "2025-01-01"
        };
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([employee]);
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        var roles = new Mock<IRoleService>();
        roles.Setup(r => r.IsModuleAccessibleForCompany(It.IsAny<string>(), companyId)).ReturnsAsync(true);
        roles.Setup(r => r.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), adminUserId, companyId)).ReturnsAsync(true);

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            policyRepo.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeRequest>>(),
            Mock.Of<IMongoDbRepository<WorkFromHomeOutboxEvent>>(),
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            empRepo.Object,
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
            roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoClient>(),
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        var grantDto = new WorkFromHomeAdminGrantDto
        {
            TargetUserId = targetUserId,
            AttendanceDate = date,
            GrantType = "FullDay",
            Remarks = "Only 2 hours worked",
            WfhCheckInTime = new TimeSpan(16, 0, 0),
            WfhCheckOutTime = new TimeSpan(18, 0, 0)
        };

        var result = await service.AdminGrantAsync(grantDto);

        Assert.False(result.Success);
        Assert.Equal("WFH_INSUFFICIENT_HOURS", result.ErrorCode);
    }

    [Fact]
    public async Task AdminGrantAsync_CalculatesLateAndEarlyExitInfractions_WhenPunchOutsideSchedule()
    {
        var companyId = "company-admin-grant-infractions";
        var adminUserId = "admin-user-id";
        var targetUserId = "employee-user-id";
        var date = new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        var claims = new[]
        {
            new System.Security.Claims.Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.company_id.ToString(), companyId),
            new System.Security.Claims.Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.user_id.ToString(), adminUserId)
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        var policy = new WorkFromHomePolicy
        {
            CompanyId = companyId,
            IsEnabled = true,
            ApplyToAllEmployees = true,
            AllowHalfDay = true,
            FullDayAttendanceStatusCode = "WFH",
            FullDayMinimumHours = 8.0m,
            HalfDayMinimumHours = 4.0m
        };
        var policyRepo = new Mock<IMongoDbRepository<WorkFromHomePolicy>>();
        policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var employee = new EmpUser
        {
            CompanyId = companyId,
            UserId = targetUserId,
            EmployeeId = "EMP1001",
            FirstName = "Aarav",
            LastName = "Sharma",
            Email = "aarav@test.invalid",
            Status = true,
            DateOfJoining = "2025-01-01"
        };
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([employee]);
        empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        var roles = new Mock<IRoleService>();
        roles.Setup(r => r.IsModuleAccessibleForCompany(It.IsAny<string>(), companyId)).ReturnsAsync(true);
        roles.Setup(r => r.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), adminUserId, companyId)).ReturnsAsync(true);

        var existingAttendance = new AttendanceModel
        {
            AttendanceId = "att-grant-1",
            CompanyId = companyId,
            UserId = targetUserId,
            EmployeeId = "EMP1001",
            Date = date,
            Status = "P",
            Version = 1
        };

        var attendanceRepo = new Mock<IMongoDbRepository<AttendanceModel>>();
        attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(existingAttendance);
        AttendanceModel? replacedRow = null;
        var attendanceCollection = new Mock<IMongoCollection<AttendanceModel>>();
        attendanceCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<AttendanceModel>, AttendanceModel, ReplaceOptions, CancellationToken>((_, r, _, _) => replacedRow = r)
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        attendanceRepo.Setup(x => x.GetCollection()).Returns(attendanceCollection.Object);

        var segmentRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        var segmentList = new List<AttendanceDaySegment>();
        segmentRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), true))
            .ReturnsAsync(segmentList);
        segmentRepo.Setup(x => x.DeleteAll(It.IsAny<FilterDefinition<AttendanceDaySegment>>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        segmentRepo.Setup(x => x.AddOne(It.IsAny<AttendanceDaySegment>()))
            .Callback<AttendanceDaySegment>(s => segmentList.Add(s))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        var segmentCollection = new Mock<IMongoCollection<AttendanceDaySegment>>();
        segmentCollection.Setup(x => x.InsertOneAsync(It.IsAny<AttendanceDaySegment>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceDaySegment, InsertOneOptions, CancellationToken>((s, _, _) => segmentList.Add(s))
            .Returns(Task.CompletedTask);
        segmentRepo.Setup(x => x.GetCollection()).Returns(segmentCollection.Object);

        var requestRepo = new Mock<IMongoDbRepository<WorkFromHomeRequest>>();
        requestRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), true))
            .ReturnsAsync([]);
        var requestCollection = new Mock<IMongoCollection<WorkFromHomeRequest>>();
        requestCollection.Setup(x => x.InsertOneAsync(It.IsAny<WorkFromHomeRequest>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        requestRepo.Setup(x => x.GetCollection()).Returns(requestCollection.Object);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>()))
            .ReturnsAsync(true);

        var scheduleService = new Mock<IEffectiveOfficeScheduleService>();
        var companySchedule = new CompanyOfficeSchedule
        {
            CompanyId = companyId,
            ScheduleId = "SCHED-1",
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(18, 0, 0),
            LateArrivalAfter = new TimeSpan(9, 15, 0),
            EarlyDepartureBefore = new TimeSpan(17, 45, 0),
            TimeZoneId = "Asia/Kolkata"
        };
        scheduleService.Setup(s => s.ResolveAsync(companyId, It.IsAny<EmpUser>(), date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(companySchedule);

        var service = new WorkFromHomeService(
            httpContextAccessor.Object,
            policyRepo.Object,
            requestRepo.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeOutboxEvent>>(),
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            empRepo.Object,
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            statusRepo.Object,
            attendanceRepo.Object,
            segmentRepo.Object,
            Mock.Of<IMongoDbRepository<AttendancePayrollException>>(),
            Mock.Of<IMongoDbRepository<MonthlyAttendanceSummary>>(),
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            Mock.Of<IAttendanceEditGuard>(),
            scheduleService.Object,
            Mock.Of<IAttendanceInitializationService>(),
            roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            Mock.Of<IMongoClient>(),
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );

        var grantDto = new WorkFromHomeAdminGrantDto
        {
            TargetUserId = targetUserId,
            AttendanceDate = date,
            GrantType = "FullDay",
            Remarks = "Late arrival and early departure punches entered",
            WfhCheckInTime = new TimeSpan(10, 0, 0),   // > 9:15 -> Late
            WfhCheckOutTime = new TimeSpan(17, 0, 0)   // < 17:45 -> Early exit
        };

        var result = await service.AdminGrantAsync(grantDto);

        Assert.True(result.Success);
        Assert.NotNull(replacedRow);
        Assert.Equal(1, replacedRow.LateCount);
        Assert.Equal(1, replacedRow.EarlyExitCount);
        Assert.NotNull(replacedRow.CheckInTime);
        Assert.NotNull(replacedRow.CheckOutTime);
        Assert.Equal(7.0m, replacedRow.TotalHours);
    }
}
