using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using MapsterMapper;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class AttendanceExportReportTests
{
    [Fact]
    public async Task ExportMonthlyAttendance_GeneratesMultiSheetWorkbookWithMusterAndSummaries()
    {
        var attendanceRepo = new Mock<IAttendanceRepository>();
        var mapper = new Mock<IMapper>();
        var mutationValidator = new Mock<IAttendanceMutationValidator>();
        var auditWriter = new Mock<IAttendanceAuditWriter>();
        var segmentRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        var weeklyOffs = new Mock<IMongoDbRepository<WeeklyOffSetting>>();
        var calendar = new Mock<IMongoDbRepository<CalendarEntity>>();
        var statuses = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();

        var employees = new List<EmpUser>
        {
            new()
            {
                CompanyId = "company-1",
                UserId = "user-1",
                EmployeeId = "EMP-001",
                FirstName = "Aarav",
                LastName = "Sharma",
                Department = "Engineering",
                JobRole = "Senior Engineer",
                Email = "aarav@codeji.com",
                DateOfJoining = "2026-01-01",
                Status = true
            }
        };

        empRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(employees);

        var monthStart = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var attendanceList = new List<AttendanceModel>
        {
            new()
            {
                CompanyId = "company-1",
                UserId = "user-1",
                EmployeeId = "EMP-001",
                Date = monthStart,
                Status = "P",
                CheckInTime = monthStart.AddHours(3).AddMinutes(45), // 03:45 UTC = 09:15 IST
                CheckOutTime = monthStart.AddHours(13), // 13:00 UTC = 18:30 IST
                TotalHours = 9.25m
            }
        };

        attendanceRepo.Setup(r => r.GetAllByDateRangeAsync("company-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string[]>()))
            .ReturnsAsync(attendanceList);

        segmentRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceDaySegment>());

        workingCalendar.Setup(w => w.GetWorkingDatesAsync("company-1", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), default))
            .ReturnsAsync(new List<DateOnly> { new(2026, 10, 1), new(2026, 10, 2) });

        weeklyOffs.Setup(w => w.FirstOrDefault(It.IsAny<Expression<Func<WeeklyOffSetting, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new WeeklyOffSetting { CompanyId = "company-1", OffDays = [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday] });

        calendar.Setup(c => c.GetAll(It.IsAny<Expression<Func<CalendarEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<CalendarEntity>());

        statuses.Setup(s => s.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceStatusSetting>
            {
                new() { CompanyId = "company-1", Code = "P", Name = "Present", ColorHex = "#2E7D32", PaidDayFraction = 1m },
                new() { CompanyId = "company-1", Code = "A", Name = "Absent", ColorHex = "#C62828", PaidDayFraction = 0m, UnpaidDayFraction = 1m }
            });

        var service = new AdminAttendanceService(
            attendanceRepo.Object,
            mapper.Object,
            mutationValidator.Object,
            auditWriter.Object,
            segmentRepo.Object,
            empRepo.Object,
            workingCalendar.Object,
            weeklyOffs.Object,
            calendar.Object,
            statuses.Object);

        var result = await service.ExportMonthlyAttendanceAsync("company-1", 2026, 10);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.Equal("attendance-report-2026-10.xlsx", result.FileName);

        using var memoryStream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(memoryStream);

        Assert.Single(workbook.Worksheets);
        Assert.NotNull(workbook.Worksheet("Attendance report"));

        var muster = workbook.Worksheet("Attendance report");
        Assert.Equal("Employee ID", muster.Cell(1, 1).GetString());
        Assert.Equal("Employee Name", muster.Cell(1, 2).GetString());
        Assert.Equal("Department", muster.Cell(1, 3).GetString());
        Assert.Equal("Designation", muster.Cell(1, 4).GetString());
        Assert.Equal("EMP-001", muster.Cell(2, 1).GetString());
        Assert.Equal("Aarav Sharma", muster.Cell(2, 2).GetString());
        Assert.Equal("Engineering", muster.Cell(2, 3).GetString());
        Assert.Equal("Senior Engineer", muster.Cell(2, 4).GetString());

        var day1Text = muster.Cell(2, 5).GetString();
        Assert.Contains("P", day1Text);
        Assert.Contains("09:15", day1Text);

        // Verify summary columns
        var presentHeader = muster.Cell(1, 36).GetString();
        Assert.Contains("Present", presentHeader);
        var paidDaysHeader = muster.Cell(1, 42).GetString();
        Assert.Contains("Total Paid", paidDaysHeader);
    }

    [Fact]
    public async Task ExportMonthlyAttendance_ExcludesFormerResignedTerminatedAndFutureEmployees_WhenOnlyEmployedIsTrue()
    {
        var attendanceRepo = new Mock<IAttendanceRepository>();
        var mapper = new Mock<IMapper>();
        var mutationValidator = new Mock<IAttendanceMutationValidator>();
        var auditWriter = new Mock<IAttendanceAuditWriter>();
        var segmentRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        var weeklyOffs = new Mock<IMongoDbRepository<WeeklyOffSetting>>();
        var calendar = new Mock<IMongoDbRepository<CalendarEntity>>();
        var statuses = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();

        var employees = new List<EmpUser>
        {
            // Active current employee (should be included)
            new()
            {
                CompanyId = "c1",
                UserId = "u1",
                EmployeeId = "EMP-001",
                FirstName = "Active",
                LastName = "Employee",
                Email = "active@codeji.com",
                DateOfJoining = "2026-01-01",
                Status = true,
                EmploymentStatus = "Active"
            },
            // Resigned employee (exit completed in past) - should be EXCLUDED
            new()
            {
                CompanyId = "c1",
                UserId = "u2",
                EmployeeId = "CD-0133",
                FirstName = "Ramu",
                LastName = "Negi",
                Email = "ramu@codeji.com",
                DateOfJoining = "2025-01-01",
                Status = false,
                EmploymentStatus = "Resigned",
                LastWorkingDate = new DateTime(2026, 8, 31)
            },
            // Terminated employee - should be EXCLUDED
            new()
            {
                CompanyId = "c1",
                UserId = "u3",
                EmployeeId = "EMP1016",
                FirstName = "Riya",
                LastName = "Joshi",
                Email = "riya@codeji.com",
                DateOfJoining = "2025-05-01",
                Status = false,
                EmploymentStatus = "Terminated",
                LastWorkingDate = new DateTime(2026, 9, 15)
            },
            // Future joiner (joining November 2026, report is for October 2026) - should be EXCLUDED
            new()
            {
                CompanyId = "c1",
                UserId = "u4",
                EmployeeId = "EMP-004",
                FirstName = "Future",
                LastName = "Joiner",
                Email = "future@codeji.com",
                DateOfJoining = "2026-11-01",
                Status = true,
                EmploymentStatus = "Active"
            }
        };

        empRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(employees);

        attendanceRepo.Setup(r => r.GetAllByDateRangeAsync("c1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string[]>()))
            .ReturnsAsync(new List<AttendanceModel>());
        segmentRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceDaySegment>());
        workingCalendar.Setup(w => w.GetWorkingDatesAsync("c1", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), default))
            .ReturnsAsync(new List<DateOnly>());
        weeklyOffs.Setup(w => w.FirstOrDefault(It.IsAny<Expression<Func<WeeklyOffSetting, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new WeeklyOffSetting { CompanyId = "c1", OffDays = [0, 6] });
        calendar.Setup(c => c.GetAll(It.IsAny<Expression<Func<CalendarEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<CalendarEntity>());
        statuses.Setup(s => s.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceStatusSetting>());

        var service = new AdminAttendanceService(
            attendanceRepo.Object, mapper.Object, mutationValidator.Object, auditWriter.Object,
            segmentRepo.Object, empRepo.Object, workingCalendar.Object, weeklyOffs.Object, calendar.Object, statuses.Object);

        // Export for October 2026 with onlyEmployed = true
        var result = await service.ExportMonthlyAttendanceAsync("c1", 2026, 10, onlyEmployed: true);

        using var memoryStream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(memoryStream);
        var muster = workbook.Worksheet("Attendance report");

        // Row 1 is header. Row 2 should be the only employee: EMP-001 (Active Employee)
        Assert.Equal("EMP-001", muster.Cell(2, 1).GetString());
        Assert.Equal("Active Employee", muster.Cell(2, 2).GetString());

        // Row 3 should be empty - Ramu Negi, Riya Joshi, and Future Joiner were filtered out!
        Assert.Empty(muster.Cell(3, 1).GetString());
    }

    [Fact]
    public async Task ExportMonthlyAttendance_FiltersByDepartmentAndSearch()
    {
        var attendanceRepo = new Mock<IAttendanceRepository>();
        var mapper = new Mock<IMapper>();
        var mutationValidator = new Mock<IAttendanceMutationValidator>();
        var auditWriter = new Mock<IAttendanceAuditWriter>();
        var segmentRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        var weeklyOffs = new Mock<IMongoDbRepository<WeeklyOffSetting>>();
        var calendar = new Mock<IMongoDbRepository<CalendarEntity>>();
        var statuses = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();

        var employees = new List<EmpUser>
        {
            new() { CompanyId = "c1", UserId = "u1", EmployeeId = "ENG-01", FirstName = "Dev", LastName = "One", Department = "EngDept", DateOfJoining = "2026-01-01", Status = true, Email = "dev1@codeji.com" },
            new() { CompanyId = "c1", UserId = "u2", EmployeeId = "ENG-02", FirstName = "Dev", LastName = "Two", Department = "EngDept", DateOfJoining = "2026-01-01", Status = true, Email = "dev2@codeji.com" },
            new() { CompanyId = "c1", UserId = "u3", EmployeeId = "HR-01", FirstName = "Hr", LastName = "Specialist", Department = "HrDept", DateOfJoining = "2026-01-01", Status = true, Email = "hr1@codeji.com" }
        };

        empRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(employees);

        attendanceRepo.Setup(r => r.GetAllByDateRangeAsync("c1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string[]>()))
            .ReturnsAsync(new List<AttendanceModel>());
        segmentRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceDaySegment>());
        workingCalendar.Setup(w => w.GetWorkingDatesAsync("c1", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), default))
            .ReturnsAsync(new List<DateOnly>());
        weeklyOffs.Setup(w => w.FirstOrDefault(It.IsAny<Expression<Func<WeeklyOffSetting, bool>>>(), It.IsAny<bool>()))
            .ReturnsAsync(new WeeklyOffSetting { CompanyId = "c1", OffDays = [0, 6] });
        calendar.Setup(c => c.GetAll(It.IsAny<Expression<Func<CalendarEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<CalendarEntity>());
        statuses.Setup(s => s.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceStatusSetting>());

        var service = new AdminAttendanceService(
            attendanceRepo.Object, mapper.Object, mutationValidator.Object, auditWriter.Object,
            segmentRepo.Object, empRepo.Object, workingCalendar.Object, weeklyOffs.Object, calendar.Object, statuses.Object);

        // Filter by Department "EngDept" and search "ENG-02"
        var result = await service.ExportMonthlyAttendanceAsync("c1", 2026, 10, departmentId: "EngDept", search: "ENG-02");

        using var memoryStream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(memoryStream);
        var muster = workbook.Worksheet("Attendance report");

        Assert.Equal("ENG-02", muster.Cell(2, 1).GetString());
        Assert.Equal("Dev Two", muster.Cell(2, 2).GetString());
        Assert.Empty(muster.Cell(3, 1).GetString());
    }
}

