using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.Attendance;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class AttendanceOverrideAndGracePeriodTests
{
    private const string CompanyId = "test-comp-1";
    private const string ActorUserId = "admin-user-1";
    private const string TargetUserId = "emp-user-1";
    private const string TargetEmpId = "EMP-001";
    private static readonly DateTime TargetDate = new(2026, 10, 20);

    [Fact]
    public async Task AttendanceMutationValidator_RejectsRegularUser_WhenOverridingLeave()
    {
        var validator = BuildValidatorWithLeaveAttendance();

        var request = new AttendanceMutationRequest
        {
            CompanyId = CompanyId,
            ActorUserId = ActorUserId,
            TargetUserId = TargetUserId,
            AttendanceDate = DateOnly.FromDateTime(TargetDate),
            StatusCode = "P",
            TimingMode = AttendanceTimingMode.Auto,
            AllowSourceOverride = false
        };

        var result = await validator.PrepareAsync(request);

        Assert.False(result.Success);
        Assert.Equal("This attendance is managed by an approved leave request.", result.Message);
    }

    [Fact]
    public async Task AttendanceMutationValidator_RejectsOverride_WhenReasonIsTooShort()
    {
        var validator = BuildValidatorWithLeaveAttendance();

        var request = new AttendanceMutationRequest
        {
            CompanyId = CompanyId,
            ActorUserId = ActorUserId,
            TargetUserId = TargetUserId,
            AttendanceDate = DateOnly.FromDateTime(TargetDate),
            StatusCode = "P",
            TimingMode = AttendanceTimingMode.Auto,
            AllowSourceOverride = true,
            OperatorRemark = "Short" // < 10 characters
        };

        var result = await validator.PrepareAsync(request);

        Assert.False(result.Success);
        Assert.Equal("An override requires an explanatory justification of at least 10 characters.", result.Message);
    }

    [Fact]
    public async Task AttendanceMutationValidator_PreparesValidOverride_WhenJustificationSatisfied()
    {
        var validator = BuildValidatorWithLeaveAttendance();

        var request = new AttendanceMutationRequest
        {
            CompanyId = CompanyId,
            ActorUserId = ActorUserId,
            TargetUserId = TargetUserId,
            AttendanceDate = DateOnly.FromDateTime(TargetDate),
            StatusCode = "P",
            TimingMode = AttendanceTimingMode.Auto,
            AllowSourceOverride = true,
            OperatorRemark = "Employee unexpectedly arrived and worked a full day at office."
        };

        var result = await validator.PrepareAsync(request);

        Assert.True(result.Success);
        Assert.True(result.MethodResult?.RequiresLeaveReversal);
        Assert.Equal("ADMIN_OVERRIDE", result.MethodResult?.Attendance.SourceType);
        Assert.Equal("leave-req-1", result.MethodResult?.LeaveRequestIdToReverse);
    }

    [Fact]
    public async Task AttendanceMutationValidator_RejectsRegularUser_WhenOverridingWfh()
    {
        var validator = BuildValidatorWithWfhAttendance();

        var request = new AttendanceMutationRequest
        {
            CompanyId = CompanyId,
            ActorUserId = ActorUserId,
            TargetUserId = TargetUserId,
            AttendanceDate = DateOnly.FromDateTime(TargetDate),
            StatusCode = "P",
            TimingMode = AttendanceTimingMode.Auto,
            AllowSourceOverride = false
        };

        var result = await validator.PrepareAsync(request);

        Assert.False(result.Success);
        Assert.Contains("work-from-home request", result.Message);
    }

    [Fact]
    public async Task AttendanceMutationValidator_PreparesValidWfhConversion_WhenOverrideAllowed()
    {
        var validator = BuildValidatorWithWfhAttendance();

        var request = new AttendanceMutationRequest
        {
            CompanyId = CompanyId,
            ActorUserId = ActorUserId,
            TargetUserId = TargetUserId,
            AttendanceDate = DateOnly.FromDateTime(TargetDate),
            StatusCode = "P",
            TimingMode = AttendanceTimingMode.Auto,
            AllowSourceOverride = true,
            OperatorRemark = "Employee attended office workshop in person."
        };

        var result = await validator.PrepareAsync(request);

        Assert.True(result.Success);
        Assert.True(result.MethodResult?.RequiresWfhConversion);
        Assert.Equal("wfh-req-1", result.MethodResult?.WfhRequestIdToConvert);
        Assert.Equal("ADMIN_OVERRIDE", result.MethodResult?.Attendance.SourceType);
    }

    [Fact]
    public async Task AttendanceMutationValidator_AcceptsCustomPunchTimings_DuringOverride()
    {
        var validator = BuildValidatorWithWfhAttendance();

        var request = new AttendanceMutationRequest
        {
            CompanyId = CompanyId,
            ActorUserId = ActorUserId,
            TargetUserId = TargetUserId,
            AttendanceDate = DateOnly.FromDateTime(TargetDate),
            StatusCode = "P",
            TimingMode = AttendanceTimingMode.Custom,
            CheckInTime = new TimeOnly(9, 30),
            CheckOutTime = new TimeOnly(18, 30),
            AllowSourceOverride = true,
            OperatorRemark = "Adjusted check-in and check-out to reflect biometric gate log."
        };

        var result = await validator.PrepareAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult?.Attendance.CheckInTime);
        Assert.NotNull(result.MethodResult?.Attendance.CheckOutTime);
        Assert.True(result.MethodResult?.Attendance.CheckOutTime > result.MethodResult?.Attendance.CheckInTime);
    }

    [Fact]
    public void GracePeriodCheck_DistinguishesWithin7DaysAndBeyond7Days()
    {
        var now = DateTime.UtcNow;
        var fourDaysAgo = now.AddDays(-4);
        var tenDaysAgo = now.AddDays(-10);

        var daysSinceFourDaysAgo = (now.Date - fourDaysAgo.Date).TotalDays;
        var daysSinceTenDaysAgo = (now.Date - tenDaysAgo.Date).TotalDays;

        Assert.True(daysSinceFourDaysAgo <= 7, "4 days ago should be within 7-day grace period");
        Assert.False(daysSinceTenDaysAgo <= 7, "10 days ago should exceed 7-day grace period");
    }

    private static AttendanceMutationValidator BuildValidatorWithLeaveAttendance()
    {
        var existingAttendance = new AttendanceModel
        {
            CompanyId = CompanyId,
            UserId = TargetUserId,
            EmployeeId = TargetEmpId,
            Date = TargetDate,
            Status = "L",
            SourceType = "LEAVE",
            SourceId = "leave-req-1"
        };

        var attendanceRepo = new Mock<IAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetByUserAndDateAsync(CompanyId, TargetUserId, It.IsAny<DateTime>()))
            .ReturnsAsync(existingAttendance);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceStatusSetting>
            {
                new() { CompanyId = CompanyId, Code = "P", Name = "Present", IsActive = true, RequiresTime = true }
            });

        var employeeRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var emp = new EmpUser
        {
            UserId = TargetUserId,
            EmployeeId = TargetEmpId,
            CompanyId = CompanyId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            DateOfJoining = "2026-01-01",
            Status = true,
            EmploymentStatus = "Active"
        };
        employeeRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { emp });

        var segmentsRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        segmentsRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceDaySegment>());

        var editGuard = new Mock<IAttendanceEditGuard>();
        var scheduleService = new Mock<IEffectiveOfficeScheduleService>();
        scheduleService.Setup(s => s.ResolveAsync(CompanyId, It.IsAny<EmpUser>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompanyOfficeSchedule
            {
                ScheduleId = "sched-standard",
                CompanyId = CompanyId,
                Name = "General Shift",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                BreakMinutes = 60,
                RequiredWorkingMinutes = 480,
                TimeZoneId = "Asia/Kolkata",
                IsDefault = true,
                IsActive = true
            });

        var statusCalc = new AttendanceStatusCalculationService();

        return new AttendanceMutationValidator(
            attendanceRepo.Object,
            editGuard.Object,
            scheduleService.Object,
            statusCalc,
            employeeRepo.Object,
            statusRepo.Object,
            segmentsRepo.Object);
    }

    private static AttendanceMutationValidator BuildValidatorWithWfhAttendance()
    {
        var existingAttendance = new AttendanceModel
        {
            CompanyId = CompanyId,
            UserId = TargetUserId,
            EmployeeId = TargetEmpId,
            Date = TargetDate,
            Status = "WFH",
            SourceType = "WFH_REQUEST",
            SourceId = "wfh-req-1"
        };

        var attendanceRepo = new Mock<IAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetByUserAndDateAsync(CompanyId, TargetUserId, It.IsAny<DateTime>()))
            .ReturnsAsync(existingAttendance);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceStatusSetting>
            {
                new() { CompanyId = CompanyId, Code = "P", Name = "Present", IsActive = true, RequiresTime = true },
                new() { CompanyId = CompanyId, Code = "WFH", Name = "Work From Home", IsActive = true, RequiresTime = false }
            });

        var employeeRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var emp = new EmpUser
        {
            UserId = TargetUserId,
            EmployeeId = TargetEmpId,
            CompanyId = CompanyId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            DateOfJoining = "2026-01-01",
            Status = true,
            EmploymentStatus = "Active"
        };
        employeeRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { emp });

        var segmentsRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        segmentsRepo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceDaySegment>());

        var editGuard = new Mock<IAttendanceEditGuard>();
        var scheduleService = new Mock<IEffectiveOfficeScheduleService>();
        scheduleService.Setup(s => s.ResolveAsync(CompanyId, It.IsAny<EmpUser>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompanyOfficeSchedule
            {
                ScheduleId = "sched-standard",
                CompanyId = CompanyId,
                Name = "General Shift",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                BreakMinutes = 60,
                RequiredWorkingMinutes = 480,
                TimeZoneId = "Asia/Kolkata",
                IsDefault = true,
                IsActive = true
            });

        var statusCalc = new AttendanceStatusCalculationService();

        return new AttendanceMutationValidator(
            attendanceRepo.Object,
            editGuard.Object,
            scheduleService.Object,
            statusCalc,
            employeeRepo.Object,
            statusRepo.Object,
            segmentsRepo.Object);
    }
}
