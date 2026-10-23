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
using MongoDB.Driver;
using Moq;

namespace Codeji.CMS.Services.Tests;

public class WorkFromHomeAttendanceTimingTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IMongoDbRepository<WorkFromHomePolicy>> _policyRepo = new();
    private readonly Mock<IMongoDbRepository<WorkFromHomeRequest>> _requestRepo = new();
    private readonly Mock<IMongoDbRepository<AttendanceModel>> _attendanceRepo = new();
    private readonly Mock<IMongoDbRepository<AttendanceDaySegment>> _segmentRepo = new();
    private readonly Mock<IRoleService> _roles = new();
    private readonly Mock<IMongoDbRepository<EmpUser>> _empRepo = new();
    private readonly Mock<IMongoDbRepository<AttendanceStatusSetting>> _statusRepo = new();
    private readonly Mock<IAttendanceEditGuard> _editGuard = new();
    private readonly Mock<IEffectiveOfficeScheduleService> _schedules = new();

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

        return new WorkFromHomeService(
            _httpContextAccessor.Object,
            _policyRepo.Object,
            _requestRepo.Object,
            Mock.Of<IMongoDbRepository<WorkFromHomeRequestLog>>(),
            Mock.Of<IMongoDbRepository<LeaveRequest>>(),
            _empRepo.Object,
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            _statusRepo.Object,
            _attendanceRepo.Object,
            _segmentRepo.Object,
            Mock.Of<IMongoDbRepository<AttendancePayrollException>>(),
            Mock.Of<IMongoDbRepository<MonthlyAttendanceSummary>>(),
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            _editGuard.Object,
            _schedules.Object,
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
    public async Task GetTiming_WhenAttendanceHasResidualTimesFromAutoPresent_ClearsTimesAndAutoHeals()
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
        // Residual times inherited from auto-present must be cleared so employee can clock in
        Assert.Null(result.MethodResult.CheckInTime);
        Assert.Null(result.MethodResult.CheckOutTime);
        Assert.Null(result.MethodResult.TotalHours);
        Assert.Null(row.CheckInTime);
        Assert.Null(row.CheckOutTime);
        Assert.Null(row.TotalHours);
        _attendanceRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), row), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenAttendanceHasResidualTimesFromAutoPresent_AllowsCancellation()
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

        var cancelResult = await service.Cancel(requestId, new WorkFromHomeCancelDto { Version = 1 });

        if (!cancelResult.Success) throw new Exception(cancelResult.Message);
        Assert.True(cancelResult.Success);
        Assert.Equal("Cancelled", request.Status);
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
}
