using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using MapsterMapper;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class AttendanceServiceDateRangeTests
{
    private const string TenantId = "tenant-1";

    [Fact]
    public async Task GetAttendanceByDateRange_FiltersOutRowsPriorToDateOfJoining()
    {
        // Arrange
        var employee = new EmpUser
        {
            CompanyId = TenantId,
            UserId = "user-1",
            EmployeeId = "EMP-01",
            FirstName = "Mohit",
            LastName = "Negi",
            Email = "mohit@example.com",
            DateOfJoining = "2026-10-15", // Joined Oct 15
            Status = true
        };

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { employee });

        var attendanceRepo = new Mock<IAttendanceRepository>();
        var dbRows = new List<AttendanceModel>
        {
            new() { CompanyId = TenantId, UserId = employee.UserId, Date = new DateTime(2026, 10, 1), Status = "P" }, // Prior to DOJ
            new() { CompanyId = TenantId, UserId = employee.UserId, Date = new DateTime(2026, 10, 10), Status = "P" }, // Prior to DOJ
            new() { CompanyId = TenantId, UserId = employee.UserId, Date = new DateTime(2026, 10, 15), Status = "P" }, // On DOJ
            new() { CompanyId = TenantId, UserId = employee.UserId, Date = new DateTime(2026, 10, 16), Status = "P" }  // After DOJ
        };
        attendanceRepo.Setup(r => r.GetAllByDateRangeAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string[]?>()))
            .ReturnsAsync(dbRows);

        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<IEnumerable<AttendanceResponseDto>>(It.IsAny<object>()))
            .Returns((object src) => ((IEnumerable<AttendanceModel>)src).Select(x => new AttendanceResponseDto
            {
                UserId = x.UserId,
                Date = x.Date,
                Status = x.Status
            }));

        var segmentsRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        segmentsRepo.Setup(s => s.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceDaySegment>());

        var service = new AdminAttendanceService(
            attendanceRepo.Object,
            mapper.Object,
            new Mock<IAttendanceMutationValidator>().Object,
            new Mock<IAttendanceAuditWriter>().Object,
            segmentsRepo.Object,
            empRepo.Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            new Mock<IMongoDbRepository<WeeklyOffSetting>>().Object,
            new Mock<IMongoDbRepository<CalendarEntity>>().Object,
            new Mock<IMongoDbRepository<AttendanceStatusSetting>>().Object,
            initializationService: null);

        // Act - Query October
        var results = (await service.GetAttendanceByDateRange(
            TenantId,
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31),
            [employee.UserId])).ToList();

        // Assert - Only rows on or after Oct 15 should be returned
        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, r => r.Date.Day < 15);
        Assert.Contains(results, r => r.Date.Day == 15);
        Assert.Contains(results, r => r.Date.Day == 16);
    }

    [Fact]
    public async Task GetAttendanceByDateRange_FiltersOutRowsAfterLastWorkingDate()
    {
        // Arrange: Joined 1 Oct, Exited 10 Oct
        var employee = new EmpUser
        {
            CompanyId = TenantId,
            UserId = "user-2",
            EmployeeId = "EMP-02",
            FirstName = "Exited",
            LastName = "Employee",
            Email = "exited@example.com",
            DateOfJoining = "2026-10-01",
            LastWorkingDate = new DateTime(2026, 10, 10),
            Status = false
        };

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<EmpUser> { employee });

        var attendanceRepo = new Mock<IAttendanceRepository>();
        var dbRows = new List<AttendanceModel>
        {
            new() { CompanyId = TenantId, UserId = employee.UserId, Date = new DateTime(2026, 10, 5), Status = "P" },
            new() { CompanyId = TenantId, UserId = employee.UserId, Date = new DateTime(2026, 10, 10), Status = "P" },
            new() { CompanyId = TenantId, UserId = employee.UserId, Date = new DateTime(2026, 10, 15), Status = "P" } // After exit
        };
        attendanceRepo.Setup(r => r.GetAllByDateRangeAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string[]?>()))
            .ReturnsAsync(dbRows);

        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<IEnumerable<AttendanceResponseDto>>(It.IsAny<object>()))
            .Returns((object src) => ((IEnumerable<AttendanceModel>)src).Select(x => new AttendanceResponseDto
            {
                UserId = x.UserId,
                Date = x.Date,
                Status = x.Status
            }));

        var segmentsRepo = new Mock<IMongoDbRepository<AttendanceDaySegment>>();
        segmentsRepo.Setup(s => s.GetAll(It.IsAny<Expression<Func<AttendanceDaySegment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceDaySegment>());

        var service = new AdminAttendanceService(
            attendanceRepo.Object,
            mapper.Object,
            new Mock<IAttendanceMutationValidator>().Object,
            new Mock<IAttendanceAuditWriter>().Object,
            segmentsRepo.Object,
            empRepo.Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            new Mock<IMongoDbRepository<WeeklyOffSetting>>().Object,
            new Mock<IMongoDbRepository<CalendarEntity>>().Object,
            new Mock<IMongoDbRepository<AttendanceStatusSetting>>().Object,
            initializationService: null);

        // Act
        var results = (await service.GetAttendanceByDateRange(
            TenantId,
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31),
            [employee.UserId])).ToList();

        // Assert - Only rows up to Oct 10 should be returned
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Date.Day == 5);
        Assert.Contains(results, r => r.Date.Day == 10);
        Assert.DoesNotContain(results, r => r.Date.Day == 15);
    }
}

