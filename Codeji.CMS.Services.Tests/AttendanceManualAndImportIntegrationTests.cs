using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Utility.Enums;
using Mapster;
using MapsterMapper;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class AttendanceManualAndImportIntegrationTests
{
    private const string TestCompanyId = "company-integration-test";
    private const string ActorUserId = "admin-actor";
    private const string TestUserId = "user-101";
    private const string TestEmpId = "EMP-101";

    private readonly Mock<IMongoDbRepository<EmpUser>> _empRepo = new();
    private readonly Mock<IMongoDbRepository<AttendanceModel>> _attendanceModelRepo = new();
    private readonly Mock<IAttendanceRepository> _attendanceRepo = new();
    private readonly Mock<IMongoDbRepository<LeaveRequest>> _leaveRepo = new();
    private readonly Mock<IMongoDbRepository<AttendanceDaySegment>> _segmentsRepo = new();
    private readonly Mock<IMongoDbRepository<MonthlyAttendanceSummary>> _summaryRepo = new();
    private readonly Mock<IMongoDbRepository<WeeklyOffSetting>> _weeklyOffRepo = new();
    private readonly Mock<IMongoDbRepository<CalendarEntity>> _calendarRepo = new();
    private readonly Mock<IMongoDbRepository<Department>> _deptRepo = new();
    private readonly Mock<IAttendanceStatusService> _statusService = new();
    private readonly Mock<IAttendancePenaltyService> _penaltyService = new();
    private readonly Mock<IEffectiveOfficeScheduleService> _scheduleService = new();
    private readonly Mock<IAttendanceEditGuard> _editGuard = new();
    private readonly Mock<IAttendanceAuditWriter> _auditWriter = new();
    private readonly Mock<IMongoDbRepository<AttendanceStatusSetting>> _statusRepo = new();

    private readonly AttendanceStatusCalculationService _statusCalcService = new();
    private readonly AttendanceMutationValidator _mutationValidator;
    private readonly AdminAttendanceService _manualAttendanceService;
    private readonly AttendanceImportService _importService;

    private readonly EmpUser _standardEmployee = new()
    {
        CompanyId = TestCompanyId,
        UserId = TestUserId,
        EmployeeId = TestEmpId,
        FirstName = "Satyam",
        LastName = "Dubey",
        Email = "satyam.dubey@company.com",
        DateOfJoining = "2026-01-01",
        Status = true,
        EmploymentStatus = "Active"
    };

    private readonly CompanyOfficeSchedule _standardSchedule = new()
    {
        ScheduleId = "sched-standard",
        CompanyId = TestCompanyId,
        Name = "General Shift",
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(18, 0, 0),
        CheckInAllowedFrom = new TimeSpan(8, 30, 0),
        CheckInAllowedUntil = new TimeSpan(9, 15, 0),
        CheckOutAllowedFrom = new TimeSpan(17, 45, 0),
        CheckOutAllowedUntil = new TimeSpan(19, 0, 0),
        BreakMinutes = 60,
        GraceMinutes = 15,
        TimeZoneId = "Asia/Kolkata",
        IsDefault = true,
        IsActive = true
    };

    public AttendanceManualAndImportIntegrationTests()
    {
        // 1. Employee Repo setup
        _empRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { _standardEmployee });

        // 2. Schedule resolution setup
        _scheduleService.Setup(s => s.ResolveAsync(TestCompanyId, It.IsAny<EmpUser>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_standardSchedule);

        // 3. Status list setup
        var statusSettings = new List<AttendanceStatusSetting>
        {
            new() { Id = "s-1", CompanyId = TestCompanyId, Code = "P", Name = "Present", RequiresTime = true, IsActive = true, PaidDayFraction = 1m },
            new() { Id = "s-2", CompanyId = TestCompanyId, Code = "A", Name = "Absent", RequiresTime = false, IsActive = true, UnpaidDayFraction = 1m },
            new() { Id = "s-3", CompanyId = TestCompanyId, Code = "HD", Name = "Half Day", RequiresTime = false, IsActive = true, PaidDayFraction = .5m, UnpaidDayFraction = .5m },
            new() { Id = "s-4", CompanyId = TestCompanyId, Code = "ED", Name = "Early Departure", RequiresTime = true, IsActive = true, PaidDayFraction = 1m },
            new() { Id = "s-5", CompanyId = TestCompanyId, Code = "LHD", Name = "Late Arrival-Half Day", RequiresTime = true, IsActive = true, PaidDayFraction = .5m, UnpaidDayFraction = .5m },
            new() { Id = "s-6", CompanyId = TestCompanyId, Code = "WFH", Name = "Work From Home", RequiresTime = true, IsActive = true, PaidDayFraction = 1m },
            new() { Id = "s-7", CompanyId = TestCompanyId, Code = "LC", Name = "Late Coming", RequiresTime = true, IsActive = true, PaidDayFraction = 1m },
        };
        _statusRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(statusSettings);

        _statusService.Setup(s => s.Get(TestCompanyId, true))
            .ReturnsAsync(statusSettings.Select(s => new AttendanceStatusSettingDto
            {
                Code = s.Code,
                Name = s.Name,
                RequiresTime = s.RequiresTime,
                IsActive = s.IsActive,
                PaidDayFraction = s.PaidDayFraction,
                UnpaidDayFraction = s.UnpaidDayFraction
            }).ToList());

        // 4. Segments setup
        _segmentsRepo.Setup(s => s.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceDaySegment>());

        // 5. Build MutationValidator
        _mutationValidator = new AttendanceMutationValidator(
            _attendanceRepo.Object,
            _editGuard.Object,
            _scheduleService.Object,
            _statusCalcService,
            _empRepo.Object,
            _statusRepo.Object,
            _segmentsRepo.Object);

        // 6. Mapster setup
        var config = new TypeAdapterConfig();
        config.NewConfig<AttendanceModel, AttendanceResponseDto>();
        var mapper = new Mapper(config);

        // 7. Build AdminAttendanceService
        _manualAttendanceService = new AdminAttendanceService(
            _attendanceRepo.Object,
            mapper,
            _mutationValidator,
            _auditWriter.Object,
            _segmentsRepo.Object,
            _empRepo.Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            _weeklyOffRepo.Object,
            _calendarRepo.Object,
            _statusRepo.Object,
            initializationService: null);

        // 8. Build AttendanceImportService
        _deptRepo.Setup(d => d.GetAll(It.IsAny<Expression<Func<Department, bool>>>(), false, true))
            .ReturnsAsync(new List<Department>
            {
                new() { DepartmentId = "dept-1", Titles = new() { new() { Label = "Engineering" } } }
            });
        _weeklyOffRepo.Setup(w => w.GetAll(It.IsAny<Expression<Func<WeeklyOffSetting, bool>>>(), false, false))
            .ReturnsAsync(new List<WeeklyOffSetting>
            {
                new() { CompanyId = TestCompanyId, OffDays = new() { (int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday } }
            });
        _calendarRepo.Setup(c => c.GetAll(It.IsAny<Expression<Func<CalendarEntity, bool>>>(), false, false))
            .ReturnsAsync(new List<CalendarEntity>());

        _importService = new AttendanceImportService(
            _empRepo.Object,
            _attendanceModelRepo.Object,
            _leaveRepo.Object,
            _segmentsRepo.Object,
            _summaryRepo.Object,
            _weeklyOffRepo.Object,
            _calendarRepo.Object,
            _deptRepo.Object,
            _attendanceRepo.Object,
            _statusService.Object,
            _penaltyService.Object);
    }

    // =========================================================================
    // SECTION 1: MANUAL ATTENDANCE MARKING INTEGRATION & REGRESSION TESTS
    // =========================================================================

    [Fact]
    public async Task AddManualAttendance_WithED_CustomPunchInAndPunchOut_PreservesStatusAndCalculatesHoursAndEarlyExit()
    {
        // Arrange: Admin marks ED on 2026-05-12 with in: 09:00, out: 14:00
        var date = new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc);
        _attendanceRepo.Setup(r => r.GetByUserAndDateAsync(TestCompanyId, TestUserId, date))
            .ReturnsAsync((AttendanceModel?)null);

        AttendanceModel? savedRow = null;
        _attendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceModel>()))
            .Callback<AttendanceModel>(r => savedRow = r)
            .ReturnsAsync((AttendanceModel r) => r);

        var dto = new AdminAttendanceCreateDto
        {
            UserId = TestUserId,
            EmployeeId = TestEmpId,
            Date = date,
            Status = "ED",
            CheckInTime = "09:00",
            CheckOutTime = "14:00",
            Remarks = "Left early for doctor appointment"
        };

        // Act
        var result = await _manualAttendanceService.AddManualAttendance(TestCompanyId, ActorUserId, dto);

        // Assert: Status must be preserved as ED, NOT downgraded to HD
        Assert.NotNull(result);
        Assert.NotNull(savedRow);
        Assert.Equal("ED", savedRow.Status);
        Assert.NotNull(savedRow.CheckInTime);
        Assert.NotNull(savedRow.CheckOutTime);
        // 14:00 - 09:00 = 5.0 hours (Break is 60m only if >= 6h)
        Assert.Equal(4.0m, savedRow.TotalHours);
        Assert.Equal(1, savedRow.EarlyExitCount);
        Assert.Equal(0, savedRow.LateCount);
    }

    [Fact]
    public async Task AddManualAttendance_WithLHD_CustomPunchInAndPunchOut_PreservesStatusAndCalculatesHoursAndLateCount()
    {
        // Arrange: Admin marks LHD on 2026-05-13 with in: 13:30, out: 18:00
        var date = new DateTime(2026, 5, 13, 0, 0, 0, DateTimeKind.Utc);
        _attendanceRepo.Setup(r => r.GetByUserAndDateAsync(TestCompanyId, TestUserId, date))
            .ReturnsAsync((AttendanceModel?)null);

        AttendanceModel? savedRow = null;
        _attendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceModel>()))
            .Callback<AttendanceModel>(r => savedRow = r)
            .ReturnsAsync((AttendanceModel r) => r);

        var dto = new AdminAttendanceCreateDto
        {
            UserId = TestUserId,
            EmployeeId = TestEmpId,
            Date = date,
            Status = "LHD",
            CheckInTime = "13:30",
            CheckOutTime = "18:00",
            Remarks = "Joined afternoon shift"
        };

        // Act
        var result = await _manualAttendanceService.AddManualAttendance(TestCompanyId, ActorUserId, dto);

        // Assert: Status must be preserved as LHD, NOT downgraded to HD
        Assert.NotNull(result);
        Assert.NotNull(savedRow);
        Assert.Equal("LHD", savedRow.Status);
        Assert.NotNull(savedRow.CheckInTime);
        Assert.NotNull(savedRow.CheckOutTime);
        // 18:00 - 13:30 = 4.5 hours
        Assert.Equal(3.5m, savedRow.TotalHours);
        Assert.Equal(1, savedRow.LateCount);
        Assert.Equal(0, savedRow.EarlyExitCount);
    }

    [Fact]
    public async Task AddManualAttendance_WithHalfDayAndTimes_PreservesPunchTimesAndRemarks()
    {
        var date = new DateTime(2026, 5, 18, 0, 0, 0, DateTimeKind.Utc);
        _attendanceRepo.Setup(r => r.GetByUserAndDateAsync(TestCompanyId, TestUserId, date))
            .ReturnsAsync((AttendanceModel?)null);

        AttendanceModel? savedRow = null;
        _attendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceModel>()))
            .Callback<AttendanceModel>(r => savedRow = r)
            .ReturnsAsync((AttendanceModel r) => r);

        var dto = new AdminAttendanceCreateDto
        {
            UserId = TestUserId,
            EmployeeId = TestEmpId,
            Date = date,
            Status = "HD",
            CheckInTime = "09:30",
            CheckOutTime = "13:30",
            Remarks = "First half doctor appointment"
        };

        var result = await _manualAttendanceService.AddManualAttendance(TestCompanyId, ActorUserId, dto);

        Assert.NotNull(result);
        Assert.NotNull(savedRow);
        Assert.Equal("HD", savedRow.Status);
        Assert.NotNull(savedRow.CheckInTime);
        Assert.NotNull(savedRow.CheckOutTime);
        Assert.Equal("First half doctor appointment", savedRow.Remarks);
    }

    [Fact]
    public async Task AddManualAttendance_WithOnlyCheckOutForED_AutomaticallyPairsShiftStartWithoutThrowing()
    {
        // Arrange: User provides only checkout 14:00 for ED (e.g. if punch in was previously left blank)
        var date = new DateTime(2026, 5, 14, 0, 0, 0, DateTimeKind.Utc);
        _attendanceRepo.Setup(r => r.GetByUserAndDateAsync(TestCompanyId, TestUserId, date))
            .ReturnsAsync((AttendanceModel?)null);

        AttendanceModel? savedRow = null;
        _attendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceModel>()))
            .Callback<AttendanceModel>(r => savedRow = r)
            .ReturnsAsync((AttendanceModel r) => r);

        var dto = new AdminAttendanceCreateDto
        {
            UserId = TestUserId,
            EmployeeId = TestEmpId,
            Date = date,
            Status = "ED",
            CheckInTime = null,
            CheckOutTime = "14:00",
            Remarks = "Early departure"
        };

        // Act
        var result = await _manualAttendanceService.AddManualAttendance(TestCompanyId, ActorUserId, dto);

        // Assert: Check-in automatically defaults to shift start 09:00 instead of crashing!
        Assert.NotNull(result);
        Assert.NotNull(savedRow);
        Assert.Equal("ED", savedRow.Status);
        Assert.NotNull(savedRow.CheckInTime);
        Assert.NotNull(savedRow.CheckOutTime);
        Assert.Equal(4.0m, savedRow.TotalHours);
        Assert.Equal(1, savedRow.EarlyExitCount);
    }

    [Fact]
    public async Task AddManualAttendance_WithOnlyCheckInForLHD_AutomaticallyPairsShiftEndWithoutThrowing()
    {
        // Arrange: User provides only checkin 13:30 for LHD
        var date = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        _attendanceRepo.Setup(r => r.GetByUserAndDateAsync(TestCompanyId, TestUserId, date))
            .ReturnsAsync((AttendanceModel?)null);

        AttendanceModel? savedRow = null;
        _attendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceModel>()))
            .Callback<AttendanceModel>(r => savedRow = r)
            .ReturnsAsync((AttendanceModel r) => r);

        var dto = new AdminAttendanceCreateDto
        {
            UserId = TestUserId,
            EmployeeId = TestEmpId,
            Date = date,
            Status = "LHD",
            CheckInTime = "13:30",
            CheckOutTime = null,
            Remarks = "Arrived afternoon"
        };

        // Act
        var result = await _manualAttendanceService.AddManualAttendance(TestCompanyId, ActorUserId, dto);

        // Assert: Check-out automatically defaults to shift end 18:00 instead of crashing!
        Assert.NotNull(result);
        Assert.NotNull(savedRow);
        Assert.Equal("LHD", savedRow.Status);
        Assert.NotNull(savedRow.CheckInTime);
        Assert.NotNull(savedRow.CheckOutTime);
        Assert.Equal(3.5m, savedRow.TotalHours);
        Assert.Equal(1, savedRow.LateCount);
    }

    [Fact]
    public async Task AddManualAttendance_WithNoTimesForLHD_PopulatesOneHourLateCheckInAndPreservesStatus()
    {
        // Arrange: HR/Admin marks LHD without typing custom punch times (Auto mode)
        var date = new DateTime(2026, 5, 16, 0, 0, 0, DateTimeKind.Utc);
        _attendanceRepo.Setup(r => r.GetByUserAndDateAsync(TestCompanyId, TestUserId, date))
            .ReturnsAsync((AttendanceModel?)null);

        AttendanceModel? savedRow = null;
        _attendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceModel>()))
            .Callback<AttendanceModel>(r => savedRow = r)
            .ReturnsAsync((AttendanceModel r) => r);

        var dto = new AdminAttendanceCreateDto
        {
            UserId = TestUserId,
            EmployeeId = TestEmpId,
            Date = date,
            Status = "LHD",
            CheckInTime = null,
            CheckOutTime = null,
            Remarks = "Admin marked LHD via modal"
        };

        // Act
        var result = await _manualAttendanceService.AddManualAttendance(TestCompanyId, ActorUserId, dto);

        // Assert: System must NOT crash, check-in must default to 10:00 (1 hour after shift start 09:00), check-out to 18:00
        Assert.NotNull(result);
        Assert.NotNull(savedRow);
        Assert.Equal("LHD", savedRow.Status);
        Assert.NotNull(savedRow.CheckInTime);
        Assert.NotNull(savedRow.CheckOutTime);
        Assert.Equal(1, savedRow.LateCount);
        Assert.Equal(0, savedRow.EarlyExitCount);
    }

    [Fact]
    public async Task AddManualAttendance_WithNoTimesForED_PopulatesOneHourEarlyCheckOutAndPreservesStatus()
    {
        // Arrange: HR/Admin marks ED without typing custom punch times (Auto mode)
        var date = new DateTime(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
        _attendanceRepo.Setup(r => r.GetByUserAndDateAsync(TestCompanyId, TestUserId, date))
            .ReturnsAsync((AttendanceModel?)null);

        AttendanceModel? savedRow = null;
        _attendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceModel>()))
            .Callback<AttendanceModel>(r => savedRow = r)
            .ReturnsAsync((AttendanceModel r) => r);

        var dto = new AdminAttendanceCreateDto
        {
            UserId = TestUserId,
            EmployeeId = TestEmpId,
            Date = date,
            Status = "ED",
            CheckInTime = null,
            CheckOutTime = null,
            Remarks = "Admin marked ED via modal"
        };

        // Act
        var result = await _manualAttendanceService.AddManualAttendance(TestCompanyId, ActorUserId, dto);

        // Assert: System must NOT crash, check-in must default to 09:00, check-out to 17:00 (1 hour before shift end 18:00)
        Assert.NotNull(result);
        Assert.NotNull(savedRow);
        Assert.Equal("ED", savedRow.Status);
        Assert.NotNull(savedRow.CheckInTime);
        Assert.NotNull(savedRow.CheckOutTime);
        Assert.Equal(0, savedRow.LateCount);
        Assert.Equal(1, savedRow.EarlyExitCount);
    }

    [Fact]
    public async Task AddManualAttendance_AdminExplicitlyMarksLHDOutsideScheduleBoundary_PreservesAdminStatusWithoutThrowing()
    {
        // Arrange: Admin explicitly marks LHD for an employee who arrived at 09:30 (within standard shift window)
        var date = new DateTime(2026, 5, 18, 0, 0, 0, DateTimeKind.Utc);
        _attendanceRepo.Setup(r => r.GetByUserAndDateAsync(TestCompanyId, TestUserId, date))
            .ReturnsAsync((AttendanceModel?)null);

        AttendanceModel? savedRow = null;
        _attendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceModel>()))
            .Callback<AttendanceModel>(r => savedRow = r)
            .ReturnsAsync((AttendanceModel r) => r);

        var dto = new AdminAttendanceCreateDto
        {
            UserId = TestUserId,
            EmployeeId = TestEmpId,
            Date = date,
            Status = "LHD",
            CheckInTime = "09:30",
            CheckOutTime = "18:00",
            Remarks = "Late arrival permitted by manager"
        };

        // Act: Should NOT throw "Check-in time must be later than configured Late Arrival boundary..."
        var result = await _manualAttendanceService.AddManualAttendance(TestCompanyId, ActorUserId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(savedRow);
        Assert.Equal("LHD", savedRow.Status);
        Assert.Equal(1, savedRow.LateCount);
    }

    [Fact]
    public async Task AddManualAttendance_WithHD_AutoMode_PopulatesHalfDayTimesAndPreservesRemarks()
    {
        var date = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc);
        _attendanceRepo.Setup(r => r.GetByUserAndDateAsync(TestCompanyId, TestUserId, date))
            .ReturnsAsync((AttendanceModel?)null);

        AttendanceModel? savedRow = null;
        _attendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceModel>()))
            .Callback<AttendanceModel>(r => savedRow = r)
            .ReturnsAsync((AttendanceModel r) => r);

        var dto = new AdminAttendanceCreateDto
        {
            UserId = TestUserId,
            EmployeeId = TestEmpId,
            Date = date,
            Status = "HD",
            Remarks = "First half half-day attendance"
        };

        var result = await _manualAttendanceService.AddManualAttendance(TestCompanyId, ActorUserId, dto);

        Assert.NotNull(result);
        Assert.NotNull(savedRow);
        Assert.Equal("HD", savedRow.Status);
        Assert.NotNull(savedRow.CheckInTime);
        Assert.NotNull(savedRow.CheckOutTime);
        Assert.True(savedRow.CheckOutTime > savedRow.CheckInTime);
        Assert.Equal("First half half-day attendance", savedRow.Remarks);
    }

    [Fact]
    public async Task AddManualAttendance_WithHD_CustomTimes_PreservesTimesAndRemarks()
    {
        var date = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc);
        _attendanceRepo.Setup(r => r.GetByUserAndDateAsync(TestCompanyId, TestUserId, date))
            .ReturnsAsync((AttendanceModel?)null);

        AttendanceModel? savedRow = null;
        _attendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceModel>()))
            .Callback<AttendanceModel>(r => savedRow = r)
            .ReturnsAsync((AttendanceModel r) => r);

        var dto = new AdminAttendanceCreateDto
        {
            UserId = TestUserId,
            EmployeeId = TestEmpId,
            Date = date,
            Status = "HD",
            CheckInTime = "09:30",
            CheckOutTime = "13:30",
            Remarks = "etretyretretrelt"
        };

        var result = await _manualAttendanceService.AddManualAttendance(TestCompanyId, ActorUserId, dto);

        Assert.NotNull(result);
        Assert.NotNull(savedRow);
        Assert.Equal("HD", savedRow.Status);
        Assert.NotNull(savedRow.CheckInTime);
        Assert.NotNull(savedRow.CheckOutTime);
        Assert.Equal("etretyretretrelt", savedRow.Remarks);
    }

    // =========================================================================
    // SECTION 2: BULK ATTENDANCE IMPORT END-TO-END REGRESSION TESTS
    // =========================================================================

    [Fact]
    public async Task ImportAttendance_FullMusterSheet_WithAllPunchFormats_ValidatesAndCommitsAccurately()
    {
        // Arrange
        _leaveRepo.Setup(l => l.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest>());

        _attendanceModelRepo.Setup(a => a.GetAll(It.IsAny<Expression<Func<AttendanceModel, bool>>>(), false, true))
            .ReturnsAsync(new List<AttendanceModel>());

        List<AttendanceModel> committed = new();
        _attendanceRepo.Setup(r => r.BulkUpsertAsync(It.IsAny<IEnumerable<AttendanceModel>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<AttendanceModel>, CancellationToken>((rows, ct) => committed.AddRange(rows))
            .ReturnsAsync((IEnumerable<AttendanceModel> rows, CancellationToken ct) => rows.Count());

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Attendance Muster");
        ws.Cell(1, 1).Value = "Employee ID";
        ws.Cell(1, 2).Value = "Employee Name";

        // Days 1 through 10 of May 2026
        ws.Cell(1, 3).Value = "01 May"; // ED (09:00-14:00)
        ws.Cell(1, 4).Value = "02 May"; // ED 14:00
        ws.Cell(1, 5).Value = "03 May"; // ED (plain)
        ws.Cell(1, 6).Value = "04 May"; // LHD (13:30-18:00)
        ws.Cell(1, 7).Value = "05 May"; // LHD 13:30
        ws.Cell(1, 8).Value = "06 May"; // LHD (plain)
        ws.Cell(1, 9).Value = "07 May"; // HD (09:00-13:30)
        ws.Cell(1, 10).Value = "08 May"; // P (09:15-18:15)
        ws.Cell(1, 11).Value = "09 May"; // 09:00 - 18:00 (pure punch range)
        ws.Cell(1, 12).Value = "10 May"; // 09:00 - 14:00 (pure punch range ED)

        ws.Cell(2, 1).Value = TestEmpId;
        ws.Cell(2, 2).Value = "Satyam Dubey";
        ws.Cell(2, 3).Value = "ED (09:00-14:00)";
        ws.Cell(2, 4).Value = "ED 14:00";
        ws.Cell(2, 5).Value = "ED";
        ws.Cell(2, 6).Value = "LHD (13:30-18:00)";
        ws.Cell(2, 7).Value = "LHD 13:30";
        ws.Cell(2, 8).Value = "LHD";
        ws.Cell(2, 9).Value = "HD (09:00-13:30)";
        ws.Cell(2, 10).Value = "P (09:15-18:15)";
        ws.Cell(2, 11).Value = "09:00 - 18:00";
        ws.Cell(2, 12).Value = "09:00 - 14:00";

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        // Act 1: Validation
        var validation = await _importService.ValidateAsync(TestCompanyId, 2026, 5, ms);

        // Assert 1: Must validate with ZERO errors
        Assert.True(validation.IsValid);
        Assert.Equal(0, validation.ErrorCount);
        Assert.Equal(10, validation.ValidEntriesCount);

        // Act 2: Commit
        ms.Position = 0;
        var commit = await _importService.CommitAsync(TestCompanyId, ActorUserId, 2026, 5, ms);

        // Assert 2: Must commit cleanly
        Assert.True(commit.Success);
        Assert.Equal(10, committed.Count);

        // Day 1: ED (09:00-14:00)
        var d1 = committed.First(r => r.Date.Day == 1);
        Assert.Equal("ED", d1.Status);
        Assert.Equal(5.0m, d1.TotalHours);
        Assert.Equal(1, d1.EarlyExitCount);
        Assert.Equal(0, d1.LateCount);

        // Day 2: ED 14:00 -> 09:00 to 14:00
        var d2 = committed.First(r => r.Date.Day == 2);
        Assert.Equal("ED", d2.Status);
        Assert.Equal(5.0m, d2.TotalHours);
        Assert.Equal(1, d2.EarlyExitCount);

        // Day 3: Plain ED -> defaults to 09:00 to 14:00 (NOT full day 18:00!)
        var d3 = committed.First(r => r.Date.Day == 3);
        Assert.Equal("ED", d3.Status);
        Assert.Equal(5.0m, d3.TotalHours);
        Assert.Equal(1, d3.EarlyExitCount);

        // Day 4: LHD (13:30-18:00)
        var d4 = committed.First(r => r.Date.Day == 4);
        Assert.Equal("LHD", d4.Status);
        Assert.Equal(4.5m, d4.TotalHours);
        Assert.Equal(1, d4.LateCount);
        Assert.Equal(0, d4.EarlyExitCount);

        // Day 5: LHD 13:30 -> 13:30 to 18:00
        var d5 = committed.First(r => r.Date.Day == 5);
        Assert.Equal("LHD", d5.Status);
        Assert.Equal(4.5m, d5.TotalHours);
        Assert.Equal(1, d5.LateCount);

        // Day 6: Plain LHD -> defaults to 13:30 to 18:00 (NOT 09:00 start!)
        var d6 = committed.First(r => r.Date.Day == 6);
        Assert.Equal("LHD", d6.Status);
        Assert.Equal(4.5m, d6.TotalHours);
        Assert.Equal(1, d6.LateCount);

        // Day 7: HD (09:00-13:30) -> retained times
        var d7 = committed.First(r => r.Date.Day == 7);
        Assert.Equal("HD", d7.Status);
        Assert.Equal(4.5m, d7.TotalHours);

        // Day 8: P (09:15-18:15) -> 9.0h - 1.0h break = 8.0h
        var d8 = committed.First(r => r.Date.Day == 8);
        Assert.Equal("P", d8.Status);
        Assert.Equal(8.0m, d8.TotalHours);

        // Day 9: 09:00 - 18:00 -> auto-classified to P
        var d9 = committed.First(r => r.Date.Day == 9);
        Assert.Equal("P", d9.Status);
        Assert.Equal(8.0m, d9.TotalHours);

        // Day 10: 09:00 - 14:00 -> auto-classified to ED
        var d10 = committed.First(r => r.Date.Day == 10);
        Assert.Equal("ED", d10.Status);
        Assert.Equal(5.0m, d10.TotalHours);
        Assert.Equal(1, d10.EarlyExitCount);
    }

    [Fact]
    public async Task ImportAttendance_InvalidCheckOutEarlierThanCheckIn_FailsValidationWithClearError()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Attendance Muster");
        ws.Cell(1, 1).Value = "Employee ID";
        ws.Cell(1, 2).Value = "Employee Name";
        ws.Cell(1, 3).Value = "01 May";

        ws.Cell(2, 1).Value = TestEmpId;
        ws.Cell(2, 2).Value = "Satyam Dubey";
        ws.Cell(2, 3).Value = "ED (18:00-09:00)"; // Invalid: checkout earlier than checkin

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var result = await _importService.ValidateAsync(TestCompanyId, 2026, 5, ms);

        Assert.False(result.IsValid);
        Assert.True(result.ErrorCount >= 1);
        Assert.Contains(result.Issues, i => i.Message.Contains("Check-out time must be later than check-in time"));
    }
}

