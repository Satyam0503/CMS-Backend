using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Domain.Models;
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public class WorkFromHomeAttendanceTimingTests
{
    private static BsonDocument Render<T>(FilterDefinition<T> filter) =>
        filter.Render(new RenderArgs<T>(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry));

    [Fact]
    public void OfficeConversionUsesSharedScheduleClassifierForVerifiedPunches()
    {
        var schedule = new CompanyOfficeSchedule
        {
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(18),
            RequiredWorkingMinutes = 480,
            GraceMinutes = 15,
            HalfDayCheckInAfter = TimeSpan.FromHours(10),
            HalfDayCheckOutBefore = TimeSpan.FromHours(17)
        };

        var status = new AttendanceStatusCalculationService().CalculateOfficeStatus(
            schedule, new TimeOnly(11, 0), new TimeOnly(18, 0), 7m);

        Assert.Equal("HD", status);
    }

    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IMongoDbRepository<WorkFromHomePolicy>> _policyRepo = new();
    private readonly Mock<IMongoDbRepository<WorkFromHomeRequest>> _requestRepo = new();
    private readonly Mock<IMongoDbRepository<WorkFromHomeOutboxEvent>> _outboxRepo = new();
    private readonly Mock<IMongoDbRepository<AttendanceModel>> _attendanceRepo = new();
    private readonly Mock<IMongoDbRepository<AttendanceDaySegment>> _segmentRepo = new();
    private readonly Mock<IMongoDbRepository<AttendancePayrollException>> _exceptionRepo = new();
    private readonly Mock<IMongoDbRepository<MonthlyAttendanceSummary>> _summaryRepo = new();
    private readonly Mock<IRoleService> _roles = new();
    private readonly Mock<IMongoDbRepository<EmpUser>> _empRepo = new();
    private readonly Mock<IMongoDbRepository<AttendanceStatusSetting>> _statusRepo = new();
    private readonly Mock<IAttendanceEditGuard> _editGuard = new();
    private readonly Mock<IEffectiveOfficeScheduleService> _schedules = new();
    private readonly Mock<IMongoClient> _mongoClient = new();
    private Mock<IMongoCollection<AttendanceModel>>? _transactionalAttendanceCollection;
    private Mock<IMongoCollection<MonthlyAttendanceSummary>>? _transactionalSummaryCollection;
    private Mock<IMongoCollection<WorkFromHomeOutboxEvent>>? _transactionalOutboxCollection;

    private static Mock<IMongoCollection<T>> SetupTransactionalCollection<T>(Mock<IMongoDbRepository<T>> repository, IClientSessionHandle session)
    {
        var collection = new Mock<IMongoCollection<T>>();
        collection.Setup(x => x.ReplaceOneAsync(session, It.IsAny<FilterDefinition<T>>(), It.IsAny<T>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        collection.Setup(x => x.InsertOneAsync(session, It.IsAny<T>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        collection.Setup(x => x.InsertManyAsync(session, It.IsAny<IEnumerable<T>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.GetCollection()).Returns(collection.Object);
        return collection;
    }

    private WorkFromHomeService CreateService(string companyId = "c1", string userId = "u1")
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.company_id.ToString(), companyId),
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.user_id.ToString(), userId),
            new Claim(ClaimTypes.Role, "Administrator")
        ], "TestAuth"));
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        _roles.Setup(x => x.IsModuleAccessibleForCompany("Work_From_Home", companyId))
            .ReturnsAsync(true);
        _outboxRepo.Setup(x => x.AddOne(It.IsAny<WorkFromHomeOutboxEvent>())).ReturnsAsync(new Result { Success = true });
        var session = new Mock<IClientSessionHandle>();
        session.Setup(x => x.WithTransactionAsync(It.IsAny<Func<IClientSessionHandle, CancellationToken, Task<bool>>>(), It.IsAny<TransactionOptions>(), It.IsAny<CancellationToken>()))
            .Returns((Func<IClientSessionHandle, CancellationToken, Task<bool>> callback, TransactionOptions _, CancellationToken token) => callback(session.Object, token));
        _mongoClient.Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(session.Object);
        _transactionalAttendanceCollection = SetupTransactionalCollection(_attendanceRepo, session.Object);
        SetupTransactionalCollection(_segmentRepo, session.Object);
        SetupTransactionalCollection(_exceptionRepo, session.Object);
        _transactionalSummaryCollection = new Mock<IMongoCollection<MonthlyAttendanceSummary>>();
        _transactionalSummaryCollection.Setup(x => x.DeleteManyAsync(
                session.Object, It.IsAny<FilterDefinition<MonthlyAttendanceSummary>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(0));
        _summaryRepo.Setup(x => x.GetCollection()).Returns(_transactionalSummaryCollection.Object);
        _transactionalOutboxCollection = SetupTransactionalCollection(_outboxRepo, session.Object);
        _exceptionRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendancePayrollException, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Enumerable.Empty<AttendancePayrollException>());

        return new WorkFromHomeService(
            _httpContextAccessor.Object,
            _policyRepo.Object,
            _requestRepo.Object,
            _outboxRepo.Object,
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            _empRepo.Object,
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            _statusRepo.Object,
            _attendanceRepo.Object,
            _segmentRepo.Object,
            _exceptionRepo.Object,
            _summaryRepo.Object,
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            _editGuard.Object,
            _schedules.Object,
            Mock.Of<IAttendanceInitializationService>(),
            _roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            _mongoClient.Object,
            Mock.Of<ILeaveManagementService>(),
            Mock.Of<ILeaveAttendanceReconciliationService>(),
            new AttendanceStatusCalculationService()
        );
    }

    [Fact]
    public async Task Cancel_WhenPayloadIsNull_ReturnsBadRequestBeforeDatabaseAccess()
    {
        var service = CreateService();

        var result = await service.Cancel("request-1", null!);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.StartsWith("WFH_CANCELLATION_INVALID:", result.Message);
        _requestRepo.Verify(x => x.GetCollection(), Times.Never);
    }

    [Fact]
    public async Task CorrectInsufficientHours_WhenPayloadIsNull_ReturnsBadRequestBeforeDatabaseAccess()
    {
        var service = CreateService();

        var result = await service.CorrectInsufficientHours("exception-1", null!);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.StartsWith("WFH_CORRECTION_INVALID:", result.Message);
        _exceptionRepo.Verify(x => x.GetCollection(), Times.Never);
    }

    [Fact]
    public async Task RegularizeAttendance_WhenPayloadIsNull_ReturnsBadRequestBeforeDatabaseAccess()
    {
        var service = CreateService();

        var result = await service.RegularizeAttendance("request-1", null!);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.StartsWith("WFH_REGULARIZATION_INVALID:", result.Message);
        _requestRepo.Verify(x => x.GetCollection(), Times.Never);
    }

    [Theory]
    [InlineData("allocation")]
    [InlineData("decision")]
    [InlineData("office-conversion")]
    [InlineData("remark-option")]
    public async Task WfhAdminMutations_WhenPayloadIsNull_ReturnBadRequest(string action)
    {
        var service = CreateService();

        var result = action switch
        {
            "allocation" => await service.SaveEmployeeAllocation(null!),
            "decision" => await service.Decide("request-1", "Approved", null!),
            "office-conversion" => await service.ConvertApprovedWfhToOffice("request-1", null!),
            "remark-option" => await service.SaveRemarkOption(null!),
            _ => throw new InvalidOperationException("Unknown test action.")
        };

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        _requestRepo.Verify(x => x.GetCollection(), Times.Never);
    }

    [Fact]
    public async Task SaveRemarkOption_WhenApplicableStatusListIsNull_ReturnsBadRequest()
    {
        var service = CreateService();

        var result = await service.SaveRemarkOption(new AttendanceRemarkOptionDto
        {
            Code = "WFH-TEST",
            DisplayName = "WFH test",
            ApplicableStatusCodes = null!
        });

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.StartsWith("ATTENDANCE_REMARK_INVALID:", result.Message);
    }

    [Fact]
    public async Task GetEmployeeContext_ReturnsTheTenantScopedApprovedRequestForToday()
    {
        var service = CreateService("c1", "u1");
        var today = DateTime.UtcNow.Date;
        var policy = new WorkFromHomePolicy { CompanyId = "c1", IsEnabled = true, TimeZoneId = "UTC", ApplyToAllEmployees = true };
        var employee = new EmpUser { CompanyId = "c1", UserId = "u1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.invalid", Status = true };
        var request = new WorkFromHomeRequest
        {
            CompanyId = "c1", UserId = "u1", EmployeeId = "e1", RequestId = "approved-today",
            FromDate = today, ToDate = today, Status = "Approved", CreatedDate = today
        };
        Expression<Func<WorkFromHomeRequest, bool>>? requestScope = null;

        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);
        _schedules.Setup(x => x.ResolveAsync("c1", employee, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanyOfficeSchedule?)null);
        _requestRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Callback<Expression<Func<WorkFromHomeRequest, bool>>, bool, bool>((filter, _, _) => requestScope = filter)
            .ReturnsAsync([request]);

        var result = await service.GetEmployeeContext();

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal("approved-today", result.MethodResult.TodayApprovedRequest?.RequestId);
        Assert.Null(result.MethodResult.TodayRequestIssueCode);
        Assert.NotNull(requestScope);
        var scoped = requestScope!.Compile();
        Assert.True(scoped(request));
        Assert.False(scoped(new WorkFromHomeRequest
        {
            CompanyId = "other-company", UserId = "u1", FromDate = today, ToDate = today, Status = "Approved"
        }));
        Assert.False(scoped(new WorkFromHomeRequest
        {
            CompanyId = "c1", UserId = "other-user", FromDate = today, ToDate = today, Status = "Approved"
        }));
    }

    [Fact]
    public async Task GetEmployeeContext_FailsClosedWhenMoreThanOneApprovedRequestCoversToday()
    {
        var service = CreateService("c1", "u1");
        var today = DateTime.UtcNow.Date;
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(new WorkFromHomePolicy { CompanyId = "c1", IsEnabled = true, TimeZoneId = "UTC", ApplyToAllEmployees = true });
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(new EmpUser { CompanyId = "c1", UserId = "u1", EmployeeId = "e1", FirstName = "Test", LastName = "Employee", Email = "test@example.invalid", Status = true });
        _schedules.Setup(x => x.ResolveAsync("c1", It.IsAny<EmpUser>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanyOfficeSchedule?)null);
        _requestRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([
                new WorkFromHomeRequest { CompanyId = "c1", UserId = "u1", EmployeeId = "e1", RequestId = "approved-1", FromDate = today, ToDate = today, Status = "Approved", CreatedDate = today },
                new WorkFromHomeRequest { CompanyId = "c1", UserId = "u1", EmployeeId = "e1", RequestId = "approved-2", FromDate = today, ToDate = today, Status = "Approved", CreatedDate = today.AddMinutes(-1) }
            ]);

        var result = await service.GetEmployeeContext();

        Assert.True(result.Success);
        Assert.Null(result.MethodResult?.TodayApprovedRequest);
        Assert.Equal("WFH_MULTIPLE_APPROVED_REQUESTS_FOR_TODAY", result.MethodResult?.TodayRequestIssueCode);
    }

    [Fact]
    public async Task GetTiming_WhenAttendanceHasResidualTimesFromAutoPresent_IgnoresPlaceholdersWithoutWriting()
    {
        var service = CreateService("c1", "u1");
        var today = DateTime.UtcNow.Date;
        var requestId = "req-1";

        var policy = new WorkFromHomePolicy
        {
            CompanyId = "c1",
            IsEnabled = true,
            TimeZoneId = "UTC",
            OfficeStartTime = new TimeSpan(23, 59, 0),
            FullDayMinimumHours = 8m
        };
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var request = new WorkFromHomeRequest
        {
            CompanyId = "c1",
            UserId = "u1",
            RequestId = requestId,
            Status = "Approved",
            FromDate = today,
            ToDate = today,
            DurationType = "FullDay"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(request);


        var row = new AttendanceModel
        {
            AttendanceId = "att-1",
            CompanyId = "c1",
            UserId = "u1",
            SourceType = "WFH_REQUEST",
            SourceId = requestId,
            Date = today,
            CheckInTime = today.AddHours(9).AddMinutes(30),
            CheckOutTime = today.AddHours(18).AddMinutes(30),
            TotalHours = 8m,
            Remarks = $"Generated from approved WFH request {requestId}"
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(row);
        _attendanceRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()))
            .ReturnsAsync(new Result { Success = true });

        var result = await service.GetTiming(requestId);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.True(result.MethodResult.IsApprovedForToday);
        // Residual times inherited from auto-present are hidden; only the check-in command may clear them.
        Assert.Null(result.MethodResult.CheckInTime);
        Assert.Null(result.MethodResult.CheckOutTime);
        Assert.Null(result.MethodResult.TotalHours);
        Assert.Equal(today.AddHours(9).AddMinutes(30), row.CheckInTime);
        Assert.Equal(today.AddHours(18).AddMinutes(30), row.CheckOutTime);
        Assert.Equal(8m, row.TotalHours);
        _attendanceRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()), Times.Never);
    }

    [Fact]
    public async Task GetTiming_WhenEmployeeIsAlreadyClockedIn_ReturnsCheckInForClockOutUiWithoutWriting()
    {
        var service = CreateService("c1", "u1");
        var today = DateTime.UtcNow.Date;
        const string requestId = "clocked-in-request";
        var checkIn = today.AddHours(9).AddMinutes(56);
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(new WorkFromHomePolicy { CompanyId = "c1", IsEnabled = true, TimeZoneId = "UTC" });
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(new WorkFromHomeRequest
            {
                CompanyId = "c1", UserId = "u1", RequestId = requestId, Status = "Approved",
                FromDate = today, ToDate = today, DurationType = "FullDay"
            });
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(new AttendanceModel
            {
                CompanyId = "c1", UserId = "u1", AttendanceId = "attendance-1", Date = today,
                SourceType = "WFH_REQUEST", SourceId = requestId, CheckInTime = checkIn,
                Remarks = $"Checked in through employee WFH portal ({requestId})"
            });

        var result = await service.GetTiming(requestId);

        Assert.True(result.Success);
        Assert.True(result.MethodResult?.IsApprovedForToday);
        Assert.Equal(checkIn, result.MethodResult?.CheckInTime);
        Assert.Null(result.MethodResult?.CheckOutTime);
        _attendanceRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_WhenMongoTransactionsAreUnavailable_FailsClosedWithoutRepositoryDeletes()
    {
        var service = CreateService("c1", "u1");
        var today = DateTime.UtcNow.Date;
        var requestId = "req-1";

        var policy = new WorkFromHomePolicy
        {
            CompanyId = "c1",
            IsEnabled = true,
            TimeZoneId = "UTC",
            OfficeStartTime = new TimeSpan(23, 59, 0),
            FullDayMinimumHours = 8m
        };
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var request = new WorkFromHomeRequest
        {
            CompanyId = "c1",
            UserId = "u1",
            RequestId = requestId,
            Status = "Approved",
            FromDate = today,
            ToDate = today,
            DurationType = "FullDay",
            Version = 1
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(request);
        _requestRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<WorkFromHomeRequest>>(), It.IsAny<WorkFromHomeRequest>()))
            .ReturnsAsync(new Result { Success = true });

        var employee = new EmpUser
        {
            CompanyId = "c1",
            UserId = "u1",
            RoleId = "r1",
            FirstName = "A",
            LastName = "B",
            Email = "a@example.com",
            Password = "x",
            Gender = "",
            EmployeeId = "E1",
            JobRole = "",
            Department = "d",
            ReportingManager = "m",
            PhoneNumber = "",
            BloodGroup = "",
            PersonalEmail = "",
            EmergencyContact = "",
            DateOfJoining = "2026-01-01",
            Address = "",
            ProfileUrl = "",
            Status = true
        };
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        var row = new AttendanceModel
        {
            AttendanceId = "att-1",
            CompanyId = "c1",
            UserId = "u1",
            SourceType = "WFH_REQUEST",
            SourceId = requestId,
            Date = today,
            CheckInTime = today.AddHours(9).AddMinutes(30),
            CheckOutTime = today.AddHours(18).AddMinutes(30),
            Remarks = $"Generated from approved WFH request {requestId}"
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(row);
        _attendanceRepo.Setup(x => x.Delete(It.IsAny<FilterDefinition<AttendanceModel>>()))
            .ReturnsAsync(new Result { Success = true });
        _mongoClient.Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("Transactions are unavailable."));

        var cancelResult = await service.Cancel(requestId, new WorkFromHomeCancelDto { Version = 1 });

        Assert.False(cancelResult.Success);
        Assert.StartsWith("WFH_CANCELLATION_TRANSACTION_REQUIRED:", cancelResult.Message);
        _attendanceRepo.Verify(x => x.Delete(It.IsAny<FilterDefinition<AttendanceModel>>()), Times.Never);
        _requestRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<WorkFromHomeRequest>>(), It.IsAny<WorkFromHomeRequest>()), Times.Never);
    }

    [Fact]
    public async Task CheckIn_WhenAttendanceHasResidualTimesFromAutoPresent_AllowsCheckInWithoutDuplicateError()
    {
        var service = CreateService("c1", "u1");
        var today = DateTime.UtcNow.Date;
        var requestId = "req-1";

        var policy = new WorkFromHomePolicy
        {
            CompanyId = "c1",
            IsEnabled = true,
            TimeZoneId = "UTC",
            AllowedCheckInFrom = new TimeSpan(0, 0, 0),
            AllowedCheckInUntil = new TimeSpan(23, 59, 59),
            FullDayMinimumHours = 8m
        };
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var request = new WorkFromHomeRequest
        {
            CompanyId = "c1",
            UserId = "u1",
            RequestId = requestId,
            Status = "Approved",
            FromDate = today,
            ToDate = today,
            DurationType = "FullDay"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(request);

        var employee = new EmpUser
        {
            CompanyId = "c1",
            UserId = "u1",
            RoleId = "r1",
            FirstName = "A",
            LastName = "B",
            Email = "a@example.com",
            Password = "x",
            Gender = "",
            EmployeeId = "E1",
            JobRole = "",
            Department = "d",
            ReportingManager = "m",
            PhoneNumber = "",
            BloodGroup = "",
            PersonalEmail = "",
            EmergencyContact = "",
            DateOfJoining = "2026-01-01",
            Address = "",
            ProfileUrl = "",
            Status = true
        };
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        var row = new AttendanceModel
        {
            AttendanceId = "att-1",
            CompanyId = "c1",
            UserId = "u1",
            SourceType = "WFH_REQUEST",
            SourceId = requestId,
            Date = today,
            CheckInTime = today.AddHours(9).AddMinutes(30),
            CheckOutTime = today.AddHours(18).AddMinutes(30),
            Remarks = $"Generated from approved WFH request {requestId}"
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(row);
        _attendanceRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()))
            .ReturnsAsync(new Result { Success = true });

        var checkInResult = await service.CheckIn(requestId);

        Assert.True(checkInResult.Success);
        Assert.NotNull(row.CheckInTime);
        Assert.Null(row.CheckOutTime);
        Assert.Contains("Checked in through employee WFH portal", row.Remarks);
        _transactionalOutboxCollection!.Verify(x => x.InsertManyAsync(
            It.IsAny<IClientSessionHandle>(),
            It.Is<IEnumerable<WorkFromHomeOutboxEvent>>(events => events.Any(evt => evt.CompanyId == "c1" && evt.RequestId == requestId && evt.EventType == "AUDIT" && evt.Action == "CheckIn")),
            It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckIn_WhenTransactionsAreUnavailable_FailsClosedWithoutAttendanceOrOutboxWrite()
    {
        var service = CreateService("company-a", "employee-a");
        var today = DateTime.UtcNow.Date;
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(new WorkFromHomePolicy { CompanyId = "company-a", IsEnabled = true, TimeZoneId = "UTC", AllowedCheckInFrom = TimeSpan.Zero, AllowedCheckInUntil = TimeSpan.FromHours(23.99) });
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(new WorkFromHomeRequest { CompanyId = "company-a", UserId = "employee-a", RequestId = "request-a", Status = "Approved", FromDate = today, ToDate = today, DurationType = "FullDay" });
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(new EmpUser { CompanyId = "company-a", UserId = "employee-a", EmployeeId = "emp-a", FirstName = "Office", LastName = "Employee", Email = "office@example.com", Status = true });
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(new AttendanceModel { CompanyId = "company-a", UserId = "employee-a", AttendanceId = "attendance-a", Date = today, SourceType = "WFH_REQUEST", SourceId = "request-a" });
        _mongoClient.Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("Transactions are unavailable."));

        var result = await service.CheckIn("request-a");

        Assert.False(result.Success);
        Assert.StartsWith("WFH_TRANSACTION_REQUIRED:", result.Message);
        _attendanceRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()), Times.Never);
        _transactionalOutboxCollection!.Verify(x => x.InsertManyAsync(It.IsAny<IClientSessionHandle>(), It.IsAny<IEnumerable<WorkFromHomeOutboxEvent>>(), It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckOut_WhenTransactionsAreUnavailable_DoesNotWriteOrInvalidateAttendanceSummary()
    {
        var service = CreateService("company-a", "employee-a");
        var now = DateTime.UtcNow;
        var today = now.Date;
        const string requestId = "request-clockout-transaction-required";
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(new WorkFromHomePolicy
            {
                CompanyId = "company-a", IsEnabled = true, TimeZoneId = "UTC",
                FullDayMinimumHours = 8m, HalfDayMinimumHours = 4m,
                FullDayAttendanceStatusCode = "WFH", HalfDayAttendanceStatusCode = "WFH-HD"
            });
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(new WorkFromHomeRequest
            {
                CompanyId = "company-a", UserId = "employee-a", RequestId = requestId,
                Status = "Approved", FromDate = today, ToDate = today, DurationType = "FullDay"
            });
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(new EmpUser
            {
                CompanyId = "company-a", UserId = "employee-a", EmployeeId = "EMP-A",
                FirstName = "Test", LastName = "Employee", Email = "employee@example.com", Status = true
            });
        var row = new AttendanceModel
        {
            AttendanceId = "attendance-clockout-transaction-required", CompanyId = "company-a",
            UserId = "employee-a", SourceType = "WFH_REQUEST", SourceId = requestId,
            Date = today, CheckInTime = now.AddHours(-1), Status = "WFH",
            Remarks = $"Checked in through employee WFH portal ({requestId})"
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(row);
        _mongoClient.Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("Transactions are unavailable."));

        var result = await service.CheckOut(requestId);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.StartsWith("WFH_TRANSACTION_REQUIRED:", result.Message);
        // The service stages changes on the loaded object before opening the session;
        // persistence must still be absent when session creation fails.
        _transactionalAttendanceCollection!.Verify(x => x.ReplaceOneAsync(
            It.IsAny<IClientSessionHandle>(), It.IsAny<FilterDefinition<AttendanceModel>>(),
            It.IsAny<AttendanceModel>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        _attendanceRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()), Times.Never);
        _exceptionRepo.Verify(x => x.AddOne(It.IsAny<AttendancePayrollException>()), Times.Never);
        _summaryRepo.Verify(x => x.DeleteAll(It.IsAny<FilterDefinition<MonthlyAttendanceSummary>>()), Times.Never);
        _transactionalSummaryCollection!.Verify(x => x.DeleteManyAsync(
            It.IsAny<IClientSessionHandle>(), It.IsAny<FilterDefinition<MonthlyAttendanceSummary>>(), null,
            It.IsAny<CancellationToken>()), Times.Never);
        _transactionalOutboxCollection!.Verify(x => x.InsertManyAsync(
            It.IsAny<IClientSessionHandle>(), It.IsAny<IEnumerable<WorkFromHomeOutboxEvent>>(),
            It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckOut_WhenEmployeeWorksHalfDay_AllowsClockOutAndSetsHalfDayStatus()
    {
        var service = CreateService("c1", "u1");
        var now = DateTime.UtcNow;
        var today = now.Date;
        var requestId = "req-1";

        var policy = new WorkFromHomePolicy
        {
            CompanyId = "c1",
            IsEnabled = true,
            TimeZoneId = "UTC",
            FullDayMinimumHours = 8m,
            HalfDayMinimumHours = 4m,
            FullDayAttendanceStatusCode = "WFH",
            HalfDayAttendanceStatusCode = "WFH-HD"
        };
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var request = new WorkFromHomeRequest
        {
            CompanyId = "c1",
            UserId = "u1",
            RequestId = requestId,
            Status = "Approved",
            FromDate = today,
            ToDate = today,
            DurationType = "FullDay"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(request);

        var employee = new EmpUser
        {
            CompanyId = "c1",
            UserId = "u1",
            EmployeeId = "E1",
            FirstName = "A",
            LastName = "B",
            Email = "a@example.com",
            Status = true
        };
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        _statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>()))
            .ReturnsAsync(true);

        // Employee clocked in 4 hours and 15 minutes ago (half-day completed)
        var row = new AttendanceModel
        {
            AttendanceId = "att-1",
            CompanyId = "c1",
            UserId = "u1",
            SourceType = "WFH_REQUEST",
            SourceId = requestId,
            Date = today,
            CheckInTime = now.AddHours(-4).AddMinutes(-15),
            CheckOutTime = null,
            Status = "WFH",
            Remarks = $"Checked in through employee WFH portal ({requestId})"
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(row);
        _attendanceRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()))
            .ReturnsAsync(new Result { Success = true });

        var checkOutResult = await service.CheckOut(requestId);

        Assert.True(checkOutResult.Success);
        Assert.NotNull(row.CheckOutTime);
        Assert.Equal("WFH-HD", row.Status);
        Assert.Equal("WFH_HALF_DAY", row.RemarkCode);
        Assert.Contains("half-day", row.Remarks);
        Assert.True(row.TotalHours >= 4m);
        _transactionalSummaryCollection!.Verify(x => x.DeleteManyAsync(
            It.IsAny<IClientSessionHandle>(), It.IsAny<FilterDefinition<MonthlyAttendanceSummary>>(), null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckOut_UsesPolicyHalfDayMinimumWhenItDiffersFromScheduleHalf()
    {
        var service = CreateService("company-a", "employee-a");
        var now = DateTime.UtcNow;
        var today = now.Date;
        var requestId = "request-policy-threshold";
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(new WorkFromHomePolicy
            {
                CompanyId = "company-a", IsEnabled = true, TimeZoneId = "UTC",
                FullDayMinimumHours = 8m, HalfDayMinimumHours = 5m,
                FullDayAttendanceStatusCode = "WFH", HalfDayAttendanceStatusCode = "WFH-HD"
            });
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(new WorkFromHomeRequest { CompanyId = "company-a", UserId = "employee-a", RequestId = requestId, Status = "Approved", FromDate = today, ToDate = today, DurationType = "FullDay" });
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(new EmpUser { CompanyId = "company-a", UserId = "employee-a", EmployeeId = "emp-a", FirstName = "A", LastName = "B", Email = "a@example.com", Status = true });
        _schedules.Setup(x => x.ResolveAsync("company-a", It.IsAny<EmpUser>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new CompanyOfficeSchedule { RequiredWorkingMinutes = 480, BreakMinutes = 0, TimeZoneId = "UTC" });
        var row = new AttendanceModel
        {
            CompanyId = "company-a", UserId = "employee-a", EmployeeId = "emp-a", AttendanceId = "attendance-policy-threshold",
            Date = today, SourceType = "WFH_REQUEST", SourceId = requestId, Status = "WFH",
            CheckInTime = now.AddHours(-4.5), Remarks = "Checked in through employee WFH portal"
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>())).ReturnsAsync(row);

        var result = await service.CheckOut(requestId);

        Assert.True(result.Success);
        Assert.Equal("A", row.Status);
        Assert.Equal("WFH_EARLY_EXIT", row.RemarkCode);
        Assert.Contains("below 5h minimum threshold", row.Remarks);
    }

    [Fact]
    public async Task CheckOut_WhenEmployeeWorksFullDay_SetsFullDayStatus()
    {
        var service = CreateService("c1", "u1");
        var now = DateTime.UtcNow;
        var today = now.Date;
        var requestId = "req-1";

        var policy = new WorkFromHomePolicy
        {
            CompanyId = "c1",
            IsEnabled = true,
            TimeZoneId = "UTC",
            FullDayMinimumHours = 8m,
            HalfDayMinimumHours = 4m,
            FullDayAttendanceStatusCode = "WFH",
            HalfDayAttendanceStatusCode = "WFH-HD"
        };
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var request = new WorkFromHomeRequest
        {
            CompanyId = "c1",
            UserId = "u1",
            RequestId = requestId,
            Status = "Approved",
            FromDate = today,
            ToDate = today,
            DurationType = "FullDay"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(request);

        var employee = new EmpUser
        {
            CompanyId = "c1",
            UserId = "u1",
            EmployeeId = "E1",
            FirstName = "A",
            LastName = "B",
            Email = "a@example.com",
            Status = true
        };
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        // Employee clocked in 8 hours and 30 minutes ago
        var row = new AttendanceModel
        {
            AttendanceId = "att-1",
            CompanyId = "c1",
            UserId = "u1",
            SourceType = "WFH_REQUEST",
            SourceId = requestId,
            Date = today,
            CheckInTime = now.AddHours(-8).AddMinutes(-30),
            CheckOutTime = null,
            Status = "WFH",
            Remarks = $"Checked in through employee WFH portal ({requestId})"
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(row);
        _attendanceRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()))
            .ReturnsAsync(new Result { Success = true });

        var checkOutResult = await service.CheckOut(requestId);

        Assert.True(checkOutResult.Success);
        Assert.NotNull(row.CheckOutTime);
        Assert.Equal("WFH", row.Status);
        Assert.Equal("WFH_FULL_DAY", row.RemarkCode);
        Assert.True(row.TotalHours >= 8m);
    }

    [Fact]
    public async Task CheckOut_WhenEmployeeWorksLessThanHalfDay_AllowsClockOutAsEarlyExit()
    {
        var service = CreateService("c1", "u1");
        var now = DateTime.UtcNow;
        var today = now.Date;
        var requestId = "req-1";

        var policy = new WorkFromHomePolicy
        {
            CompanyId = "c1",
            IsEnabled = true,
            TimeZoneId = "UTC",
            FullDayMinimumHours = 8m,
            HalfDayMinimumHours = 4m,
            FullDayAttendanceStatusCode = "WFH",
            HalfDayAttendanceStatusCode = "WFH-HD"
        };
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var request = new WorkFromHomeRequest
        {
            CompanyId = "c1",
            UserId = "u1",
            RequestId = requestId,
            Status = "Approved",
            FromDate = today,
            ToDate = today,
            DurationType = "FullDay"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(request);

        var employee = new EmpUser
        {
            CompanyId = "c1",
            UserId = "u1",
            EmployeeId = "E1",
            FirstName = "A",
            LastName = "B",
            Email = "a@example.com",
            Status = true
        };
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        // Employee clocked in 2 hours ago (emergency exit before half-day)
        var row = new AttendanceModel
        {
            AttendanceId = "att-1",
            CompanyId = "c1",
            UserId = "u1",
            SourceType = "WFH_REQUEST",
            SourceId = requestId,
            Date = today,
            CheckInTime = now.AddHours(-2),
            CheckOutTime = null,
            Status = "WFH",
            Remarks = $"Checked in through employee WFH portal ({requestId})"
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(row);
        _attendanceRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()))
            .ReturnsAsync(new Result { Success = true });

        var checkOutResult = await service.CheckOut(requestId);

        Assert.True(checkOutResult.Success);
        Assert.NotNull(row.CheckOutTime);
        Assert.Equal("WFH_EARLY_EXIT", row.RemarkCode);
        Assert.Contains("early exit", row.Remarks);
        Assert.True(row.TotalHours >= 2m);
    }

    [Fact]
    public async Task CheckOut_WithExplicitConfirmation_MarksAttendanceAsHalfDayWithUserRemarks()
    {
        var service = CreateService("c1", "u1");
        var requestId = "wfh-req-confirm-hd";
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var policy = new WorkFromHomePolicy
        {
            CompanyId = "c1",
            IsEnabled = true,
            TimeZoneId = "UTC",
            FullDayMinimumHours = 8m,
            HalfDayMinimumHours = 4m,
            FullDayAttendanceStatusCode = "WFH",
            HalfDayAttendanceStatusCode = "WFH-HD"
        };
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var employee = new EmpUser
        {
            CompanyId = "c1",
            UserId = "u1",
            EmployeeId = "EMP1",
            FirstName = "A",
            LastName = "B",
            Email = "a@example.com",
            Status = true
        };
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        _statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>()))
            .ReturnsAsync(true);

        var request = new WorkFromHomeRequest
        {
            RequestId = requestId,
            CompanyId = "c1",
            UserId = "u1",
            EmployeeId = "EMP1",
            FromDate = today,
            ToDate = today,
            DurationType = "FullDay",
            Status = "Approved"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(request);

        var row = new AttendanceModel
        {
            AttendanceId = "att-hd",
            CompanyId = "c1",
            UserId = "u1",
            SourceType = "WFH_REQUEST",
            SourceId = requestId,
            Date = today,
            CheckInTime = now.AddHours(-4.5),
            CheckOutTime = null,
            Status = "WFH",
            Remarks = $"Checked in through employee WFH portal ({requestId})"
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(row);
        _attendanceRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()))
            .ReturnsAsync(new Result { Success = true });

        var checkOutDto = new WorkFromHomeCheckOutDto
        {
            AttendanceType = "HalfDay",
            Remarks = "Doctor appointment afternoon"
        };

        var checkOutResult = await service.CheckOut(requestId, checkOutDto);

        Assert.True(checkOutResult.Success);
        Assert.NotNull(row.CheckOutTime);
        Assert.Equal("WFH-HD", row.Status);
        Assert.Equal("WFH_HALF_DAY", row.RemarkCode);
        Assert.Contains("half-day", row.Remarks);
        Assert.Contains("Doctor appointment afternoon", row.Remarks);
    }

    [Fact]
    public async Task CheckOut_WithExplicitConfirmation_MarksAttendanceAsFullDayWithUserRemarks()
    {
        var service = CreateService("c1", "u1");
        var requestId = "wfh-req-confirm-fd";
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var policy = new WorkFromHomePolicy
        {
            CompanyId = "c1",
            IsEnabled = true,
            TimeZoneId = "UTC",
            FullDayMinimumHours = 8m,
            HalfDayMinimumHours = 4m,
            FullDayAttendanceStatusCode = "WFH",
            HalfDayAttendanceStatusCode = "WFH-HD"
        };
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var employee = new EmpUser
        {
            CompanyId = "c1",
            UserId = "u1",
            EmployeeId = "EMP1",
            FirstName = "A",
            LastName = "B",
            Email = "a@example.com",
            Status = true
        };
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        _statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>()))
            .ReturnsAsync(true);

        var request = new WorkFromHomeRequest
        {
            RequestId = requestId,
            CompanyId = "c1",
            UserId = "u1",
            EmployeeId = "EMP1",
            FromDate = today,
            ToDate = today,
            DurationType = "FullDay",
            Status = "Approved"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(request);

        var row = new AttendanceModel
        {
            AttendanceId = "att-fd",
            CompanyId = "c1",
            UserId = "u1",
            SourceType = "WFH_REQUEST",
            SourceId = requestId,
            Date = today,
            CheckInTime = now.AddHours(-8.5),
            CheckOutTime = null,
            Status = "WFH",
            Remarks = $"Checked in through employee WFH portal ({requestId})"
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(row);
        _attendanceRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()))
            .ReturnsAsync(new Result { Success = true });

        var checkOutDto = new WorkFromHomeCheckOutDto
        {
            AttendanceType = "FullDay",
            Remarks = "Shift completed normally"
        };

        var checkOutResult = await service.CheckOut(requestId, checkOutDto);

        Assert.True(checkOutResult.Success);
        Assert.NotNull(row.CheckOutTime);
        Assert.Equal("WFH", row.Status);
        Assert.Equal("WFH_FULL_DAY", row.RemarkCode);
        Assert.Contains("Full-Day", row.Remarks);
        Assert.Contains("Shift completed normally", row.Remarks);
    }

    [Fact]
    public async Task CheckOut_WithUnderHours_MarksAsInsufficientHoursAndDoesNotAllowHalfDay()
    {
        var service = CreateService("c1", "u1");
        var requestId = "wfh-req-underhours-54min";
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var policy = new WorkFromHomePolicy
        {
            CompanyId = "c1",
            IsEnabled = true,
            TimeZoneId = "UTC",
            FullDayMinimumHours = 8m,
            HalfDayMinimumHours = 4m,
            FullDayAttendanceStatusCode = "WFH",
            HalfDayAttendanceStatusCode = "WFH-HD"
        };
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);

        var employee = new EmpUser
        {
            CompanyId = "c1",
            UserId = "u1",
            EmployeeId = "EMP1",
            FirstName = "A",
            LastName = "B",
            Email = "a@example.com",
            Status = true
        };
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        _statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>()))
            .ReturnsAsync(true);

        var request = new WorkFromHomeRequest
        {
            RequestId = requestId,
            CompanyId = "c1",
            UserId = "u1",
            EmployeeId = "EMP1",
            FromDate = today,
            ToDate = today,
            DurationType = "FullDay",
            Status = "Approved"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(request);

        var row = new AttendanceModel
        {
            AttendanceId = "att-54min",
            CompanyId = "c1",
            UserId = "u1",
            SourceType = "WFH_REQUEST",
            SourceId = requestId,
            Date = today,
            CheckInTime = now.AddMinutes(-54), // Only 54 minutes worked!
            CheckOutTime = null,
            Status = "WFH",
            Remarks = $"Checked in through employee WFH portal ({requestId})"
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(row);
        _attendanceRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()))
            .ReturnsAsync(new Result { Success = true });

        var checkOutDto = new WorkFromHomeCheckOutDto
        {
            Remarks = "Leaving for urgent personal matter"
        };

        var checkOutResult = await service.CheckOut(requestId, checkOutDto);

        Assert.True(checkOutResult.Success);
        Assert.NotNull(row.CheckOutTime);
        // An employee who worked 54 minutes CANNOT mark themselves as Half-Day or Full-Day!
        Assert.Equal("A", row.Status);
        Assert.Equal("WFH_EARLY_EXIT", row.RemarkCode);
        Assert.Contains("below 4h minimum threshold", row.Remarks);
        Assert.Contains("Pending HR/Admin regularisation", row.Remarks);
        Assert.Contains("Leaving for urgent personal matter", row.Remarks);
        Assert.True(row.TotalHours < 1.0m);
    }

    [Fact]
    public async Task RegularizeAttendance_ByHrAdmin_UpdatesAttendanceStatusAndResolvesException()
    {
        var service = CreateService("c1", "admin1");
        var requestId = "wfh-req-regularize";
        var today = DateTime.UtcNow.Date;

        _roles.Setup(x => x.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.WorkFromHome, It.IsAny<string[]>(), "admin1", "c1"))
            .ReturnsAsync(true);

        _statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>()))
            .ReturnsAsync(true);

        var request = new WorkFromHomeRequest
        {
            RequestId = requestId,
            CompanyId = "c1",
            UserId = "u1",
            EmployeeId = "EMP1",
            FromDate = today,
            ToDate = today,
            DurationType = "FullDay",
            Status = "Approved"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(request);

        var employee = new EmpUser
        {
            CompanyId = "c1",
            UserId = "u1",
            EmployeeId = "EMP1",
            FirstName = "A",
            LastName = "B",
            Email = "a@example.com",
            Status = true
        };
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(employee);

        var row = new AttendanceModel
        {
            AttendanceId = "att-reg",
            CompanyId = "c1",
            UserId = "u1",
            SourceType = "WFH_REQUEST",
            SourceId = requestId,
            Date = today,
            Status = "A",
            RemarkCode = "WFH_INSUFFICIENT_HOURS",
            TotalHours = 0.9m
        };
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(row);
        _attendanceRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()))
            .ReturnsAsync(new Result { Success = true });

        var regDto = new WorkFromHomeRegularizeAttendanceDto
        {
            AttendanceStatus = "WFH-HD",
            Remarks = "Approved half-day concession by HR"
        };

        var regResult = await service.RegularizeAttendance(requestId, regDto);

        Assert.True(regResult.Success);
        Assert.Equal("WFH-HD", row.Status);
        Assert.Equal("WFH_REGULARIZED_BY_ADMIN", row.RemarkCode);
        Assert.Contains("Approved half-day concession by HR", row.Remarks);
    }

    [Fact]
    public async Task RegularizeAttendance_RejectsPendingRequestWithoutWritingAttendance()
    {
        var service = CreateService("company-a", "admin-a");
        _roles.Setup(x => x.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.WorkFromHome, It.IsAny<string[]>(), "admin-a", "company-a"))
            .ReturnsAsync(true);
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(new WorkFromHomeRequest { CompanyId = "company-a", RequestId = "req-pending", UserId = "employee-a", Status = "Pending" });

        var result = await service.RegularizeAttendance("req-pending", new WorkFromHomeRegularizeAttendanceDto
        {
            AttendanceStatus = "P",
            Remarks = "Employee attended office"
        });

        Assert.False(result.Success);
        _attendanceRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()), Times.Never);
        _segmentRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<AttendanceDaySegment>>(), It.IsAny<AttendanceDaySegment>()), Times.Never);
    }

    [Fact]
    public async Task RegularizeAttendance_UpdateFilterIsTenantAndSourceScoped()
    {
        var service = CreateService("company-a", "admin-a");
        _roles.Setup(x => x.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.WorkFromHome, It.IsAny<string[]>(), "admin-a", "company-a"))
            .ReturnsAsync(true);
        _statusRepo.Setup(x => x.Exist(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>())).ReturnsAsync(true);
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(new WorkFromHomeRequest { CompanyId = "company-a", RequestId = "req-a", UserId = "employee-a", FromDate = DateTime.UtcNow.Date, Status = "Approved" });
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(new EmpUser { CompanyId = "company-a", UserId = "employee-a", EmployeeId = "emp-a", FirstName = "Office", LastName = "Employee", Email = "office@example.com", Status = true });
        _attendanceRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(new AttendanceModel { CompanyId = "company-a", AttendanceId = "507f1f77bcf86cd799439011", UserId = "employee-a", SourceType = "WFH_REQUEST", SourceId = "req-a", Date = DateTime.UtcNow.Date, Version = 5 });

        FilterDefinition<AttendanceModel>? capturedFilter = null;
        var collection = new Mock<IMongoCollection<AttendanceModel>>();
        collection.Setup(x => x.ReplaceOneAsync(It.IsAny<IClientSessionHandle>(), It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .Callback<IClientSessionHandle, FilterDefinition<AttendanceModel>, AttendanceModel, ReplaceOptions, CancellationToken>((_, filter, _, _, _) => capturedFilter = filter)
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        _attendanceRepo.Setup(x => x.GetCollection()).Returns(collection.Object);

        var result = await service.RegularizeAttendance("req-a", new WorkFromHomeRegularizeAttendanceDto
        {
            AttendanceStatus = "P",
            Remarks = "Verified office attendance"
        });

        Assert.True(result.Success);
        Assert.NotNull(capturedFilter);
        var rendered = Render(capturedFilter!).ToJson();
        Assert.Contains("company-a", rendered, StringComparison.Ordinal);
        Assert.Contains("employee-a", rendered, StringComparison.Ordinal);
        Assert.Contains("req-a", rendered, StringComparison.Ordinal);
        Assert.Contains("Version", rendered, StringComparison.Ordinal);
        _transactionalOutboxCollection!.Verify(x => x.InsertManyAsync(
            It.IsAny<IClientSessionHandle>(),
            It.Is<IEnumerable<WorkFromHomeOutboxEvent>>(events => events.Any(evt => evt.CompanyId == "company-a" && evt.RequestId == "req-a" && evt.EventType == "AUDIT" && evt.Action == "AttendanceRegularized")),
            It.IsAny<InsertManyOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_MultiDayRequestWithClockedLaterDay_PerformsNoAttendanceDeletes()
    {
        var service = CreateService("company-a", "employee-a");
        var firstDay = DateTime.UtcNow.Date.AddDays(2);
        var request = new WorkFromHomeRequest
        {
            CompanyId = "company-a", RequestId = "req-multi", UserId = "employee-a", EmployeeId = "emp-a",
            FromDate = firstDay, ToDate = firstDay.AddDays(1), DurationType = "FullDay", Status = "Approved", Version = 3
        };
        _requestRepo.SetupSequence(x => x.FirstOrDefault(It.IsAny<Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(request);
        _empRepo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>()))
            .ReturnsAsync(new EmpUser { CompanyId = "company-a", UserId = "employee-a", EmployeeId = "emp-a", FirstName = "Office", LastName = "Employee", Email = "office@example.com", Status = true });
        _schedules.Setup(x => x.ResolveAsync("company-a", It.IsAny<EmpUser>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanyOfficeSchedule?)null);
        _editGuard.Setup(x => x.EnsureEditableWorkingDayAsync("company-a", "employee-a", It.IsAny<DateTime>(), false))
            .Returns(Task.CompletedTask);

        _attendanceRepo.SetupSequence(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>()))
            .ReturnsAsync(new AttendanceModel
            {
                CompanyId = "company-a", UserId = "employee-a", SourceType = "WFH_REQUEST", SourceId = "req-multi",
                Date = firstDay, Remarks = "Generated from approved WFH request req-multi"
            })
            .ReturnsAsync(new AttendanceModel
            {
                CompanyId = "company-a", UserId = "employee-a", SourceType = "WFH_REQUEST", SourceId = "req-multi",
                Date = firstDay.AddDays(1), CheckInTime = DateTime.UtcNow.AddHours(-1), Remarks = "Checked in through employee WFH portal"
            });

        var result = await service.Cancel("req-multi", new WorkFromHomeCancelDto { Version = 3 });

        Assert.False(result.Success);
        Assert.Contains("WFH_CANNOT_CANCEL_AFTER_CLOCKING", result.Message, StringComparison.Ordinal);
        _attendanceRepo.Verify(x => x.Delete(It.IsAny<FilterDefinition<AttendanceModel>>()), Times.Never);
        _segmentRepo.Verify(x => x.Delete(It.IsAny<FilterDefinition<AttendanceDaySegment>>()), Times.Never);
    }
}
