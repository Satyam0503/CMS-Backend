using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Utility.Enums;
using Moq;
using MongoDB.Driver;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class AttendanceImportServiceTests
{
    private readonly Mock<IMongoDbRepository<EmpUser>> _employees = new();
    private readonly Mock<IMongoDbRepository<AttendanceModel>> _attendance = new();
    private readonly Mock<IMongoDbRepository<LeaveRequest>> _leaves = new();
    private readonly Mock<IMongoDbRepository<AttendanceDaySegment>> _segments = new();
    private readonly Mock<IMongoDbRepository<MonthlyAttendanceSummary>> _summaries = new();
    private readonly Mock<IMongoDbRepository<WeeklyOffSetting>> _weeklyOffs = new();
    private readonly Mock<IMongoDbRepository<CalendarEntity>> _calendar = new();
    private readonly Mock<IAttendanceRepository> _attendanceRepository = new();
    private readonly Mock<IAttendanceStatusService> _statusService = new();
    private readonly Mock<IAttendancePenaltyService> _penaltyService = new();

    private readonly AttendanceImportService _service;

    public AttendanceImportServiceTests()
    {
        _service = new AttendanceImportService(
            _employees.Object,
            _attendance.Object,
            _leaves.Object,
            _segments.Object,
            _summaries.Object,
            _weeklyOffs.Object,
            _calendar.Object,
            _attendanceRepository.Object,
            _statusService.Object,
            _penaltyService.Object);

        // Standard default setups
        _statusService.Setup(s => s.Get("company-1", true))
            .ReturnsAsync(new List<AttendanceStatusSettingDto>
            {
                new() { Code = "P", Name = "Present", PaidDayFraction = 1m, UnpaidDayFraction = 0m, IsActive = true },
                new() { Code = "A", Name = "Absent", PaidDayFraction = 0m, UnpaidDayFraction = 1m, IsActive = true },
                new() { Code = "HD", Name = "Half Day", PaidDayFraction = 0.5m, UnpaidDayFraction = 0.5m, IsActive = true },
                new() { Code = "WFH", Name = "Work From Home", PaidDayFraction = 1m, UnpaidDayFraction = 0m, IsActive = true }
            });

        _weeklyOffs.Setup(w => w.GetAll(It.IsAny<Expression<Func<WeeklyOffSetting, bool>>>(), false, false))
            .ReturnsAsync(new List<WeeklyOffSetting>
            {
                new() { CompanyId = "company-1", OffDays = new List<int> { (int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday } }
            });

        _calendar.Setup(c => c.GetAll(It.IsAny<Expression<Func<CalendarEntity, bool>>>(), false, false))
            .ReturnsAsync(new List<CalendarEntity>
            {
                new() { CompanyId = "company-1", Type = EnumsHelper.CalendarItem.Holiday, Date = new DateTime(2026, 10, 2), Name = "Gandhi Jayanti" }
            });

        _summaries.Setup(s => s.Exist(It.IsAny<Expression<Func<MonthlyAttendanceSummary, bool>>>(), false))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task GenerateTemplateAsync_CreatesWorkbookWithMusterAndStatusCodesSheets()
    {
        // Arrange
        var emp1 = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Department = "IT",
            Status = true,
            Email = "john.doe@company.com",
            DateOfJoining = "2026-10-01"
        };
        var emp2 = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-2",
            EmployeeId = "EMP-002",
            FirstName = "Jane",
            LastName = "Smith",
            Department = "HR",
            Status = true,
            Email = "jane.smith@company.com",
            DateOfJoining = "2026-10-05"
        };

        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser> { emp1, emp2 });

        // Act
        var (fileBytes, fileName) = await _service.GenerateTemplateAsync("company-1", 2026, 10);

        // Assert
        Assert.NotNull(fileBytes);
        Assert.Contains("2026_10", fileName);

        using var ms = new MemoryStream(fileBytes);
        using var workbook = new XLWorkbook(ms);

        Assert.Equal(2, workbook.Worksheets.Count);
        var musterSheet = workbook.Worksheet("Attendance Muster");
        var codesSheet = workbook.Worksheet("Allowed Status Codes");

        Assert.NotNull(musterSheet);
        Assert.NotNull(codesSheet);

        // Headers
        Assert.Equal("Employee ID", musterSheet.Cell(1, 1).GetString());
        Assert.Equal("Employee Name", musterSheet.Cell(1, 2).GetString());
        Assert.Equal("Date of Joining", musterSheet.Cell(1, 3).GetString());

        // Employee 1: Joined Oct 1
        Assert.Equal("EMP-001", musterSheet.Cell(2, 1).GetString());
        Assert.Equal("John Doe", musterSheet.Cell(2, 2).GetString());
        // Day 1 (Oct 1, Thursday) -> "P"
        Assert.Equal("P", musterSheet.Cell(2, 5).GetString());
        // Day 2 (Oct 2, Friday) Gandhi Jayanti -> "H"
        Assert.Equal("H", musterSheet.Cell(2, 6).GetString());
        // Day 3 (Oct 3, Saturday) -> "WO"
        Assert.Equal("WO", musterSheet.Cell(2, 7).GetString());

        // Employee 2: Joined Oct 5
        Assert.Equal("EMP-002", musterSheet.Cell(3, 1).GetString());
        // Day 1 (Oct 1) is before joining date (Oct 5) -> must be "N/A"
        Assert.Equal("N/A", musterSheet.Cell(3, 5).GetString());
        // Day 2 (Oct 2) is before joining date (Oct 5) -> must be "N/A"
        Assert.Equal("N/A", musterSheet.Cell(3, 6).GetString());
        // Day 5 (Oct 5, Monday, joining date) -> "P"
        Assert.Equal("P", musterSheet.Cell(3, 9).GetString());
    }

    [Fact]
    public async Task ValidateAsync_FlagsErrorsForPreJoiningDateAndInvalidCodes()
    {
        // Arrange: Generate workbook
        var emp = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Status = true,
            Email = "john.doe@company.com",
            DateOfJoining = "2026-10-10"
        };

        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser> { emp });

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Muster");
        sheet.Cell(1, 1).Value = "Employee ID";
        sheet.Cell(1, 2).Value = "Employee Name";
        sheet.Cell(1, 3).Value = "Date of Joining";
        sheet.Cell(1, 4).Value = "Department";
        sheet.Cell(1, 5).Value = "01 Oct (Thu)";
        sheet.Cell(1, 6).Value = "02 Oct (Fri)";
        sheet.Cell(1, 7).Value = "10 Oct (Sat)";

        // Row 2: Invalid "P" before joining date (Oct 1 < Oct 10), and invalid status "XYZ"
        sheet.Cell(2, 1).Value = "EMP-001";
        sheet.Cell(2, 2).Value = "John Doe";
        sheet.Cell(2, 3).Value = "2026-10-10";
        sheet.Cell(2, 4).Value = "IT";
        sheet.Cell(2, 5).Value = "P";    // Error: cannot be P before joining date
        sheet.Cell(2, 6).Value = "N/A";  // Valid: N/A before joining date
        sheet.Cell(2, 7).Value = "INVALID_CODE"; // Error: Invalid status code

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        _leaves.Setup(l => l.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest>());

        // Act
        var result = await _service.ValidateAsync("company-1", 2026, 10, ms);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.ErrorCount >= 2);
        Assert.Contains(result.Issues, i => i.Message.Contains("prior to joining date"));
        Assert.Contains(result.Issues, i => i.Message.Contains("Invalid status code"));
    }

    [Fact]
    public async Task ValidateAsync_PreservesApprovedLeavesAndWarns()
    {
        // Arrange
        var emp = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Status = true,
            Email = "john.doe@company.com",
            DateOfJoining = "2026-10-01"
        };

        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser> { emp });

        // Approved leave on Oct 5
        var leave = new LeaveRequest
        {
            LeaveRequestId = "leave-1",
            CompanyId = "company-1",
            EmployeeId = "user-1",
            LeavePolicyId = "policy-1",
            Reason = "Fever",
            Status = EnumsHelper.LeaveRequestStatus.Accepted,
            StartDate = new DateTime(2026, 10, 5),
            EndDate = new DateTime(2026, 10, 5)
        };

        _leaves.Setup(l => l.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest> { leave });

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Muster");
        sheet.Cell(1, 1).Value = "Employee ID";
        sheet.Cell(1, 2).Value = "Employee Name";
        sheet.Cell(1, 3).Value = "Date of Joining";
        sheet.Cell(1, 4).Value = "Department";
        sheet.Cell(1, 5).Value = "05 Oct (Mon)";

        sheet.Cell(2, 1).Value = "EMP-001";
        sheet.Cell(2, 2).Value = "John Doe";
        sheet.Cell(2, 3).Value = "2026-10-01";
        sheet.Cell(2, 4).Value = "IT";
        sheet.Cell(2, 5).Value = "P"; // Will collide with approved leave

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        // Act
        var result = await _service.ValidateAsync("company-1", 2026, 10, ms, preserveApprovedLeaves: true);

        // Assert
        Assert.True(result.IsValid); // Warnings do not make validation invalid
        Assert.Equal(1, result.PreservedLeavesCount);
        Assert.Contains(result.Issues, i => i.Severity == "Warning" && i.Message.Contains("Existing leave will be preserved"));
    }

    [Fact]
    public async Task CommitAsync_SuccessfullyUpsertsRecordsAndInvalidatesSummaries()
    {
        // Arrange
        var emp = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Status = true,
            Email = "john.doe@company.com",
            DateOfJoining = "2026-10-01"
        };

        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser> { emp });

        _leaves.Setup(l => l.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest>());

        _attendance.Setup(a => a.GetAll(It.IsAny<Expression<Func<AttendanceModel, bool>>>(), false, true))
            .ReturnsAsync(new List<AttendanceModel>());

        _attendanceRepository.Setup(r => r.BulkUpsertAsync(It.IsAny<IEnumerable<AttendanceModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Muster");
        sheet.Cell(1, 1).Value = "Employee ID";
        sheet.Cell(1, 2).Value = "Employee Name";
        sheet.Cell(1, 3).Value = "Date of Joining";
        sheet.Cell(1, 4).Value = "Department";
        sheet.Cell(1, 5).Value = "01 Oct (Thu)";
        sheet.Cell(1, 6).Value = "02 Oct (Fri)";

        sheet.Cell(2, 1).Value = "EMP-001";
        sheet.Cell(2, 2).Value = "John Doe";
        sheet.Cell(2, 3).Value = "2026-10-01";
        sheet.Cell(2, 4).Value = "IT";
        sheet.Cell(2, 5).Value = "P";
        sheet.Cell(2, 6).Value = "WFH";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        // Act
        var result = await _service.CommitAsync("company-1", "admin-1", 2026, 10, ms);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.EmployeesAffected);
        Assert.Equal(2, result.RecordsUpserted);

        // Verifications
        _attendanceRepository.Verify(r => r.BulkUpsertAsync(
            It.Is<IEnumerable<AttendanceModel>>(list => list.Count() == 2),
            It.IsAny<CancellationToken>()), Times.Once);

        _summaries.Verify(s => s.DeleteAll(It.IsAny<FilterDefinition<MonthlyAttendanceSummary>>()), Times.Once);
        _penaltyService.Verify(p => p.Recalculate("company-1", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenPayrollMonthIsLocked()
    {
        // Arrange
        _summaries.Setup(s => s.Exist(It.IsAny<Expression<Func<MonthlyAttendanceSummary, bool>>>(), false))
            .ReturnsAsync(true);

        using var ms = new MemoryStream();

        // Act
        var result = await _service.ValidateAsync("company-1", 2026, 10, ms);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(1, result.ErrorCount);
        Assert.Contains(result.Issues, i => i.Message.Contains("locked"));
    }

    [Fact]
    public async Task ValidateAsync_FlagsError_WhenMarkingAttendanceAfterExitDate()
    {
        // Arrange
        var emp = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Status = false,
            Email = "john.doe@company.com",
            DateOfJoining = "2026-10-01",
            LastWorkingDate = new DateTime(2026, 10, 15)
        };

        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser> { emp });

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Muster");
        sheet.Cell(1, 1).Value = "Employee ID";
        sheet.Cell(1, 2).Value = "Employee Name";
        sheet.Cell(1, 3).Value = "Date of Joining";
        sheet.Cell(1, 4).Value = "Department";
        sheet.Cell(1, 5).Value = "10 Oct (Sat)";
        sheet.Cell(1, 6).Value = "20 Oct (Tue)";

        sheet.Cell(2, 1).Value = "EMP-001";
        sheet.Cell(2, 2).Value = "John Doe";
        sheet.Cell(2, 3).Value = "2026-10-01";
        sheet.Cell(2, 4).Value = "IT";
        sheet.Cell(2, 5).Value = "P";    // Before exit: valid
        sheet.Cell(2, 6).Value = "P";    // After exit (Oct 20 > Oct 15): Error!

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        _leaves.Setup(l => l.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest>());

        // Act
        var result = await _service.ValidateAsync("company-1", 2026, 10, ms);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(1, result.ErrorCount);
        Assert.Contains(result.Issues, i => i.Message.Contains("after exit date"));
    }

    [Fact]
    public async Task ValidateAsync_FlagsError_ForFutureDateWorkedAttendance_AllowsCalendarCodes()
    {
        // Arrange: Use a month in the distant future (e.g. year 2029)
        var emp = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Status = true,
            Email = "john.doe@company.com",
            DateOfJoining = "2029-01-01"
        };

        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser> { emp });

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Muster");
        sheet.Cell(1, 1).Value = "Employee ID";
        sheet.Cell(1, 2).Value = "Employee Name";
        sheet.Cell(1, 3).Value = "Date of Joining";
        sheet.Cell(1, 4).Value = "Department";
        sheet.Cell(1, 5).Value = "01 Jan";
        sheet.Cell(1, 6).Value = "02 Jan";

        sheet.Cell(2, 1).Value = "EMP-001";
        sheet.Cell(2, 2).Value = "John Doe";
        sheet.Cell(2, 3).Value = "2029-01-01";
        sheet.Cell(2, 4).Value = "IT";
        sheet.Cell(2, 5).Value = "P";  // Future date with 'P' -> Error!
        sheet.Cell(2, 6).Value = "WO"; // Future date with 'WO' -> Valid (calendar setup)

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        _leaves.Setup(l => l.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest>());

        // Act
        var result = await _service.ValidateAsync("company-1", 2029, 1, ms);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(1, result.ErrorCount);
        Assert.Contains(result.Issues, i => i.Message.Contains("future date"));
    }

    [Fact]
    public async Task ValidateAsync_HandlesMidMonthJoinedAndExitedEmployee()
    {
        // Arrange: joined Oct 5, exited Oct 20
        var emp = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Status = false,
            Email = "john.doe@company.com",
            DateOfJoining = "2026-10-05",
            LastWorkingDate = new DateTime(2026, 10, 20)
        };

        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser> { emp });

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Muster");
        sheet.Cell(1, 1).Value = "Employee ID";
        sheet.Cell(1, 2).Value = "Employee Name";
        sheet.Cell(1, 3).Value = "Date of Joining";
        sheet.Cell(1, 4).Value = "Department";
        sheet.Cell(1, 5).Value = "02 Oct (Fri)"; // Pre-joining -> N/A
        sheet.Cell(1, 6).Value = "10 Oct (Sat)"; // During tenure -> P
        sheet.Cell(1, 7).Value = "25 Oct (Sun)"; // Post-exit -> N/A

        sheet.Cell(2, 1).Value = "EMP-001";
        sheet.Cell(2, 2).Value = "John Doe";
        sheet.Cell(2, 3).Value = "2026-10-05";
        sheet.Cell(2, 4).Value = "IT";
        sheet.Cell(2, 5).Value = "N/A";
        sheet.Cell(2, 6).Value = "P";
        sheet.Cell(2, 7).Value = "N/A";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        _leaves.Setup(l => l.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest>());

        // Act
        var result = await _service.ValidateAsync("company-1", 2026, 10, ms);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(3, result.ValidEntriesCount);
    }

    [Fact]
    public async Task ValidateAsync_NormalizesStatusAliasesAndTrimsWhitespace()
    {
        // Arrange
        var emp = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Status = true,
            Email = "john.doe@company.com",
            DateOfJoining = "2026-10-01"
        };

        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser> { emp });

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Muster");
        sheet.Cell(1, 1).Value = "Employee ID";
        sheet.Cell(1, 2).Value = "Employee Name";
        sheet.Cell(1, 3).Value = "Date of Joining";
        sheet.Cell(1, 4).Value = "Department";
        sheet.Cell(1, 5).Value = "01 Oct (Thu)";
        sheet.Cell(1, 6).Value = "02 Oct (Fri)";
        sheet.Cell(1, 7).Value = "03 Oct (Sat)";
        sheet.Cell(1, 8).Value = "04 Oct (Sun)";

        sheet.Cell(2, 1).Value = "EMP-001";
        sheet.Cell(2, 2).Value = "John Doe";
        sheet.Cell(2, 3).Value = "2026-10-01";
        sheet.Cell(2, 4).Value = "IT";
        sheet.Cell(2, 5).Value = "  PRESENT  "; // alias -> P
        sheet.Cell(2, 6).Value = "  absent  ";  // lowercase -> A
        sheet.Cell(2, 7).Value = "  HALF DAY "; // alias -> HD
        sheet.Cell(2, 8).Value = "  weekend  ";  // alias -> WO

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        _leaves.Setup(l => l.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest>());

        // Act
        var result = await _service.ValidateAsync("company-1", 2026, 10, ms);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(4, result.ValidEntriesCount);
    }

    [Fact]
    public async Task ValidateAsync_WarnsOnBlankWorkingDayCells()
    {
        // Arrange
        var emp = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Status = true,
            Email = "john.doe@company.com",
            DateOfJoining = "2026-10-01"
        };

        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser> { emp });

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Muster");
        sheet.Cell(1, 1).Value = "Employee ID";
        sheet.Cell(1, 2).Value = "Employee Name";
        sheet.Cell(1, 3).Value = "Date of Joining";
        sheet.Cell(1, 4).Value = "Department";
        sheet.Cell(1, 5).Value = "01 Oct (Thu)";

        sheet.Cell(2, 1).Value = "EMP-001";
        sheet.Cell(2, 2).Value = "John Doe";
        sheet.Cell(2, 3).Value = "2026-10-01";
        sheet.Cell(2, 4).Value = "IT";
        sheet.Cell(2, 5).Value = ""; // Blank cell

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        _leaves.Setup(l => l.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest>());

        // Act
        var result = await _service.ValidateAsync("company-1", 2026, 10, ms);

        // Assert
        Assert.True(result.IsValid); // Warnings do not invalidate
        Assert.Equal(1, result.WarningCount);
        Assert.Contains(result.Issues, i => i.Severity == "Warning" && i.Message.Contains("Empty attendance status"));
    }

    [Fact]
    public async Task ValidateAsync_ReportsError_WhenEmployeeNotFound()
    {
        // Arrange
        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser>());

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Muster");
        sheet.Cell(1, 1).Value = "Employee ID";
        sheet.Cell(1, 2).Value = "Employee Name";
        sheet.Cell(1, 3).Value = "Date of Joining";
        sheet.Cell(1, 4).Value = "Department";
        sheet.Cell(1, 5).Value = "01 Oct (Thu)";

        sheet.Cell(2, 1).Value = "UNKNOWN-999";
        sheet.Cell(2, 2).Value = "Unknown Person";
        sheet.Cell(2, 3).Value = "2026-10-01";
        sheet.Cell(2, 4).Value = "IT";
        sheet.Cell(2, 5).Value = "P";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        // Act
        var result = await _service.ValidateAsync("company-1", 2026, 10, ms);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(1, result.ErrorCount);
        Assert.Contains(result.Issues, i => i.Message.Contains("not found in company directory"));
    }

    [Fact]
    public async Task ValidateAsync_HandlesFlexibleDateHeaderFormats()
    {
        // Arrange
        var emp = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Status = true,
            Email = "john.doe@company.com",
            DateOfJoining = "2026-10-01"
        };

        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser> { emp });

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Muster");
        sheet.Cell(1, 1).Value = "Employee ID";
        sheet.Cell(1, 2).Value = "Employee Name";
        sheet.Cell(1, 3).Value = "Date of Joining";
        sheet.Cell(1, 4).Value = "Department";
        // Flexible date headers: "01", "2", "2026-10-03"
        sheet.Cell(1, 5).Value = "01";
        sheet.Cell(1, 6).Value = "2";
        sheet.Cell(1, 7).Value = "2026-10-03";

        sheet.Cell(2, 1).Value = "EMP-001";
        sheet.Cell(2, 2).Value = "John Doe";
        sheet.Cell(2, 3).Value = "2026-10-01";
        sheet.Cell(2, 4).Value = "IT";
        sheet.Cell(2, 5).Value = "P";
        sheet.Cell(2, 6).Value = "P";
        sheet.Cell(2, 7).Value = "P";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        _leaves.Setup(l => l.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest>());

        // Act
        var result = await _service.ValidateAsync("company-1", 2026, 10, ms);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(3, result.ValidEntriesCount);
    }

    [Fact]
    public async Task CommitAsync_OverwritesLeaves_WhenPreserveApprovedLeavesIsFalse()
    {
        // Arrange
        var emp = new EmpUser
        {
            CompanyId = "company-1",
            UserId = "user-1",
            EmployeeId = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Status = true,
            Email = "john.doe@company.com",
            DateOfJoining = "2026-10-01"
        };

        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser> { emp });

        var leave = new LeaveRequest
        {
            LeaveRequestId = "leave-1",
            CompanyId = "company-1",
            EmployeeId = "user-1",
            LeavePolicyId = "policy-1",
            Reason = "Medical",
            Status = EnumsHelper.LeaveRequestStatus.Accepted,
            StartDate = new DateTime(2026, 10, 5),
            EndDate = new DateTime(2026, 10, 5)
        };

        _leaves.Setup(l => l.GetAll(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), false, true))
            .ReturnsAsync(new List<LeaveRequest> { leave });

        _attendance.Setup(a => a.GetAll(It.IsAny<Expression<Func<AttendanceModel, bool>>>(), false, true))
            .ReturnsAsync(new List<AttendanceModel>());

        _attendanceRepository.Setup(r => r.BulkUpsertAsync(It.IsAny<IEnumerable<AttendanceModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance Muster");
        sheet.Cell(1, 1).Value = "Employee ID";
        sheet.Cell(1, 2).Value = "Employee Name";
        sheet.Cell(1, 3).Value = "Date of Joining";
        sheet.Cell(1, 4).Value = "Department";
        sheet.Cell(1, 5).Value = "05 Oct (Mon)";

        sheet.Cell(2, 1).Value = "EMP-001";
        sheet.Cell(2, 2).Value = "John Doe";
        sheet.Cell(2, 3).Value = "2026-10-01";
        sheet.Cell(2, 4).Value = "IT";
        sheet.Cell(2, 5).Value = "P"; // Overwrite leave with P

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        // Act: preserveApprovedLeaves = false
        var result = await _service.CommitAsync("company-1", "admin-1", 2026, 10, ms, preserveApprovedLeaves: false);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.RecordsUpserted);
        Assert.Equal(0, result.PreservedLeavesCount);

        _attendanceRepository.Verify(r => r.BulkUpsertAsync(
            It.Is<IEnumerable<AttendanceModel>>(list => list.Count() == 1 && list.First().Status == "P"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTemplateAsync_LeapYearFebruary_Generates29Days()
    {
        // Arrange
        _employees.Setup(e => e.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), false, true))
            .ReturnsAsync(new List<EmpUser>());

        // Act: 2024 is a leap year
        var (fileBytes2024, _) = await _service.GenerateTemplateAsync("company-1", 2024, 2);
        // 2026 is not a leap year
        var (fileBytes2026, _) = await _service.GenerateTemplateAsync("company-1", 2026, 2);

        // Assert
        using var ms2024 = new MemoryStream(fileBytes2024);
        using var wb2024 = new XLWorkbook(ms2024);
        var muster2024 = wb2024.Worksheet("Attendance Muster");
        // Column 4 + 29 = 33
        Assert.Equal("29 Feb (Thu)", muster2024.Cell(1, 33).GetString());

        using var ms2026 = new MemoryStream(fileBytes2026);
        using var wb2026 = new XLWorkbook(ms2026);
        var muster2026 = wb2026.Worksheet("Attendance Muster");
        // Column 4 + 28 = 32
        Assert.Equal("28 Feb (Sat)", muster2026.Cell(1, 32).GetString());
        Assert.True(string.IsNullOrEmpty(muster2026.Cell(1, 33).GetString()));
    }

    [Theory]
    [InlineData(1999, 10)]
    [InlineData(2101, 10)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task GenerateTemplateAsync_ThrowsArgumentOutOfRangeException_ForInvalidYearOrMonth(int year, int month)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.GenerateTemplateAsync("company-1", year, month));
    }
}
