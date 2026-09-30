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
                DateOfJoining = "2026-01-01"
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
        Assert.Equal("EMP-001", muster.Cell(2, 1).GetString());
        Assert.Equal("Aarav Sharma", muster.Cell(2, 2).GetString());

        var day1Text = muster.Cell(2, 3).GetString();
        Assert.Contains("P", day1Text);
        Assert.Contains("09:15", day1Text);

    }
}

