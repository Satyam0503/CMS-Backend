using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Utility.Helpers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public class ShiftAttendanceReconciliationServiceTests
{
    private readonly Mock<IMongoDbRepository<AttendanceModel>> _attendanceRepo = new();
    private readonly Mock<IMongoDbRepository<EmpUser>> _empRepo = new();
    private readonly Mock<IEffectiveOfficeScheduleService> _scheduleService = new();
    private readonly Mock<ILogger<ShiftAttendanceReconciliationService>> _logger = new();

    private ShiftAttendanceReconciliationService CreateService() =>
        new(_attendanceRepo.Object, _empRepo.Object, _scheduleService.Object, _logger.Object);

    [Fact]
    public async Task ReconcileTodayShiftAttendanceAsync_WhenNoRecords_ReturnsZero()
    {
        _attendanceRepo
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceModel, bool>>>(), false, false))
            .ReturnsAsync(new List<AttendanceModel>());

        var service = CreateService();
        var result = await service.ReconcileTodayShiftAttendanceAsync("company-1");

        Assert.Equal(0, result);
        _attendanceRepo.Verify(r => r.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()), Times.Never);
    }

    [Fact]
    public async Task ReconcileTodayShiftAttendanceAsync_WhenSystemPresentHasOldTimings_RealignsToNewShift()
    {
        var companyId = "company-1";
        var userId = "user-123";
        var today = IndiaTime.Now.Date;
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

        // Old Shift 1 (09:00 - 18:00 IST)
        var oldCheckIn = AttendanceMutationValidator.ToScheduleUtc(today, new TimeSpan(9, 0, 0), "Asia/Kolkata");
        var oldCheckOut = AttendanceMutationValidator.ToScheduleUtc(today, new TimeSpan(18, 0, 0), "Asia/Kolkata");

        var existingRecord = new AttendanceModel
        {
            AttendanceId = "att-1",
            CompanyId = companyId,
            UserId = userId,
            Date = todayUtc,
            Status = "P",
            SourceType = "SYSTEM_OFFICE_START_PRESENT",
            CheckInTime = oldCheckIn,
            CheckOutTime = oldCheckOut,
            ScheduleId = "shift-1",
            Remarks = "System Present created at the effective office start time."
        };

        _attendanceRepo
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceModel, bool>>>(), false, false))
            .ReturnsAsync(new List<AttendanceModel> { existingRecord });

        var employee = new EmpUser
        {
            UserId = userId,
            CompanyId = companyId,
            FirstName = "Test",
            LastName = "Employee",
            Email = "test@company.com",
            DateOfJoining = today.AddYears(-1).ToString("yyyy-MM-dd"),
            Status = true
        };

        _empRepo
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, false))
            .ReturnsAsync(new List<EmpUser> { employee });

        // New Shift 2 (09:30 - 18:30 IST)
        var newShift = new CompanyOfficeSchedule
        {
            ScheduleId = "shift-2",
            Name = "Shift 2",
            StartTime = new TimeSpan(9, 30, 0),
            EndTime = new TimeSpan(18, 30, 0),
            GraceMinutes = 60,
            BreakMinutes = 60,
            RequiredWorkingMinutes = 480,
            TimeZoneId = "Asia/Kolkata",
            Version = 2
        };

        _scheduleService
            .Setup(s => s.ResolveAsync(companyId, employee, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newShift);

        _attendanceRepo
            .Setup(r => r.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });

        var service = CreateService();
        var updated = await service.ReconcileTodayShiftAttendanceAsync(companyId);

        Assert.Equal(1, updated);

        var expectedCheckIn = AttendanceMutationValidator.ToScheduleUtc(today, new TimeSpan(9, 30, 0), "Asia/Kolkata");
        var expectedCheckOut = AttendanceMutationValidator.ToScheduleUtc(today, new TimeSpan(18, 30, 0), "Asia/Kolkata");

        Assert.Equal(expectedCheckIn, existingRecord.CheckInTime);
        Assert.Equal(expectedCheckOut, existingRecord.CheckOutTime);
        Assert.Equal("shift-2", existingRecord.ScheduleId);
        Assert.Equal(0, existingRecord.LateCount);
        Assert.Equal(0, existingRecord.EarlyExitCount);
        Assert.Contains("Shift 2", existingRecord.Remarks);
        Assert.Contains("09:30–18:30", existingRecord.Remarks);
    }

    [Fact]
    public async Task ReconcileTodayShiftAttendanceAsync_WhenTimingsAlreadyMatch_DoesNotUpdate()
    {
        var companyId = "company-1";
        var userId = "user-123";
        var today = IndiaTime.Now.Date;
        var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);

        var expectedCheckIn = AttendanceMutationValidator.ToScheduleUtc(today, new TimeSpan(9, 30, 0), "Asia/Kolkata");
        var expectedCheckOut = AttendanceMutationValidator.ToScheduleUtc(today, new TimeSpan(18, 30, 0), "Asia/Kolkata");

        var existingRecord = new AttendanceModel
        {
            AttendanceId = "att-1",
            CompanyId = companyId,
            UserId = userId,
            Date = todayUtc,
            Status = "P",
            SourceType = "SYSTEM_OFFICE_START_PRESENT",
            CheckInTime = expectedCheckIn,
            CheckOutTime = expectedCheckOut,
            ScheduleId = "shift-2"
        };

        _attendanceRepo
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceModel, bool>>>(), false, false))
            .ReturnsAsync(new List<AttendanceModel> { existingRecord });

        var employee = new EmpUser
        {
            UserId = userId,
            CompanyId = companyId,
            FirstName = "Test",
            LastName = "Employee",
            Email = "test@company.com",
            DateOfJoining = today.AddYears(-1).ToString("yyyy-MM-dd"),
            Status = true
        };

        _empRepo
            .Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, false))
            .ReturnsAsync(new List<EmpUser> { employee });

        var shift = new CompanyOfficeSchedule
        {
            ScheduleId = "shift-2",
            Name = "Shift 2",
            StartTime = new TimeSpan(9, 30, 0),
            EndTime = new TimeSpan(18, 30, 0),
            TimeZoneId = "Asia/Kolkata"
        };

        _scheduleService
            .Setup(s => s.ResolveAsync(companyId, employee, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);

        var service = CreateService();
        var updated = await service.ReconcileTodayShiftAttendanceAsync(companyId);

        Assert.Equal(0, updated);
        _attendanceRepo.Verify(r => r.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()), Times.Never);
    }
}

