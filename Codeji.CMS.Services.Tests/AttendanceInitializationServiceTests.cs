using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class AttendanceInitializationServiceTests
{
    private const string TestCompanyId = "comp-1";
    private const string ActorUserId = "admin-1";

    [Fact]
    public async Task InitializeEmployeeAttendanceAsync_ValidDateOfJoining_CreatesDefaultPresentStartingFromDoj()
    {
        // Arrange
        var employee = new EmpUser
        {
            CompanyId = TestCompanyId,
            UserId = "user-101",
            EmployeeId = "EMP-101",
            FirstName = "Mohit",
            LastName = "Negi",
            Email = "mohit.negi@example.com",
            DateOfJoining = "2026-10-01",
            Status = true
        };

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(r => r.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(employee);

        var statusSetting = new AttendanceStatusSetting
        {
            CompanyId = TestCompanyId,
            Code = "P",
            Name = "Present",
            IsActive = true,
            SortOrder = 1
        };
        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceStatusSetting> { statusSetting });

        var summariesRepo = new Mock<IMongoDbRepository<MonthlyAttendanceSummary>>();
        summariesRepo.Setup(r => r.DeleteAll(It.IsAny<FilterDefinition<MonthlyAttendanceSummary>>()))
            .ReturnsAsync(new Result { Success = true });

        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        // Working days: Oct 1, Oct 2, Oct 5 (Oct 3 & 4 are weekend)
        var workingDays = new List<DateOnly>
        {
            new(2026, 10, 1),
            new(2026, 10, 2),
            new(2026, 10, 5)
        };
        workingCalendar.Setup(c => c.GetWorkingDatesAsync(TestCompanyId, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workingDays);

        var validator = new Mock<IAttendanceMutationValidator>();
        validator.Setup(v => v.PrepareAsync(It.IsAny<AttendanceMutationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttendanceMutationRequest req, CancellationToken _) =>
                new Result<PreparedAttendanceMutation>
                {
                    Success = true,
                    MethodResult = new PreparedAttendanceMutation
                    {
                        Employee = employee,
                        ShouldWrite = true,
                        Attendance = new AttendanceModel
                        {
                            CompanyId = req.CompanyId,
                            UserId = req.TargetUserId,
                            Date = req.AttendanceDate.ToDateTime(TimeOnly.MinValue),
                            Status = req.StatusCode
                        }
                    }
                });

        var createdRecords = new List<AttendanceModel>();
        var attendanceRepo = new Mock<IAttendanceRepository>();
        attendanceRepo.Setup(a => a.AddAsync(It.IsAny<AttendanceModel>()))
            .ReturnsAsync((AttendanceModel m) =>
            {
                createdRecords.Add(m);
                return m;
            });

        var service = new AttendanceInitializationService(
            empRepo.Object,
            statusRepo.Object,
            summariesRepo.Object,
            workingCalendar.Object,
            validator.Object,
            attendanceRepo.Object);

        // Act - initialize for month 2026-10 up to 2026-10-05
        var result = await service.InitializeEmployeeAttendanceAsync(
            TestCompanyId,
            ActorUserId,
            employee.UserId,
            month: new DateTime(2026, 10, 1),
            throughDate: new DateTime(2026, 10, 5));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.MethodResult.Created);
        Assert.Equal(3, createdRecords.Count);

        // Crucial test requirement: Day 1 (1 Oct) MUST be marked as Present!
        Assert.Contains(createdRecords, r => r.Date.Day == 1 && r.Date.Month == 10 && r.Status == "P");
        Assert.Contains(createdRecords, r => r.Date.Day == 2 && r.Date.Month == 10 && r.Status == "P");
        Assert.Contains(createdRecords, r => r.Date.Day == 5 && r.Date.Month == 10 && r.Status == "P");
    }

    [Fact]
    public async Task InitializeEmployeeAttendanceAsync_FallbackToCreatedDate_WhenDateOfJoiningMissing()
    {
        // Arrange
        var employee = new EmpUser
        {
            CompanyId = TestCompanyId,
            UserId = "user-102",
            EmployeeId = "EMP-102",
            FirstName = "Fallback",
            LastName = "User",
            Email = "fallback@example.com",
            DateOfJoining = null, // Missing DoJ
            CreatedDate = new DateTime(2026, 10, 2, 8, 30, 0, DateTimeKind.Utc),
            Status = true
        };

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(r => r.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(employee);

        var statusSetting = new AttendanceStatusSetting
        {
            CompanyId = TestCompanyId,
            Code = "P",
            Name = "Present",
            IsActive = true
        };
        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceStatusSetting> { statusSetting });

        var summariesRepo = new Mock<IMongoDbRepository<MonthlyAttendanceSummary>>();
        summariesRepo.Setup(r => r.DeleteAll(It.IsAny<FilterDefinition<MonthlyAttendanceSummary>>()))
            .ReturnsAsync(new Result { Success = true });

        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        // Range should start at 2026-10-02 (fallback from CreatedDate)
        var workingDays = new List<DateOnly>
        {
            new(2026, 10, 2),
            new(2026, 10, 5)
        };
        workingCalendar.Setup(c => c.GetWorkingDatesAsync(TestCompanyId, new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workingDays);

        var validator = new Mock<IAttendanceMutationValidator>();
        validator.Setup(v => v.PrepareAsync(It.IsAny<AttendanceMutationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttendanceMutationRequest req, CancellationToken _) =>
                new Result<PreparedAttendanceMutation>
                {
                    Success = true,
                    MethodResult = new PreparedAttendanceMutation
                    {
                        Employee = employee,
                        ShouldWrite = true,
                        Attendance = new AttendanceModel
                        {
                            CompanyId = req.CompanyId,
                            UserId = req.TargetUserId,
                            Date = req.AttendanceDate.ToDateTime(TimeOnly.MinValue),
                            Status = req.StatusCode
                        }
                    }
                });

        var createdRecords = new List<AttendanceModel>();
        var attendanceRepo = new Mock<IAttendanceRepository>();
        attendanceRepo.Setup(a => a.AddAsync(It.IsAny<AttendanceModel>()))
            .ReturnsAsync((AttendanceModel m) =>
            {
                createdRecords.Add(m);
                return m;
            });

        var service = new AttendanceInitializationService(
            empRepo.Object,
            statusRepo.Object,
            summariesRepo.Object,
            workingCalendar.Object,
            validator.Object,
            attendanceRepo.Object);

        // Act
        var result = await service.InitializeEmployeeAttendanceAsync(
            TestCompanyId,
            ActorUserId,
            employee.UserId,
            month: new DateTime(2026, 10, 1),
            throughDate: new DateTime(2026, 10, 5));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.MethodResult.Created);
        // Created date was Oct 2, so Oct 2 and Oct 5 are created
        Assert.Contains(createdRecords, r => r.Date.Day == 2 && r.Date.Month == 10 && r.Status == "P");
        Assert.Contains(createdRecords, r => r.Date.Day == 5 && r.Date.Month == 10 && r.Status == "P");
        Assert.DoesNotContain(createdRecords, r => r.Date.Day == 1);
    }

    [Fact]
    public async Task InitializeEmployeeAttendanceAsync_DateOfJoiningTakesPrecedenceOverCreatedDate()
    {
        // Arrange: DateOfJoining is Oct 1, CreatedDate is Oct 4 (e.g. entered late)
        var employee = new EmpUser
        {
            CompanyId = TestCompanyId,
            UserId = "user-103",
            EmployeeId = "EMP-103",
            FirstName = "Precedence",
            LastName = "User",
            Email = "precedence@example.com",
            DateOfJoining = "2026-10-01",
            CreatedDate = new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc),
            Status = true
        };

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(r => r.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(employee);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceStatusSetting> { new() { CompanyId = TestCompanyId, Code = "P", Name = "Present", IsActive = true } });

        var summariesRepo = new Mock<IMongoDbRepository<MonthlyAttendanceSummary>>();
        summariesRepo.Setup(r => r.DeleteAll(It.IsAny<FilterDefinition<MonthlyAttendanceSummary>>()))
            .ReturnsAsync(new Result { Success = true });

        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        var workingDays = new List<DateOnly> { new(2026, 10, 1), new(2026, 10, 2) };
        workingCalendar.Setup(c => c.GetWorkingDatesAsync(TestCompanyId, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workingDays);

        var validator = new Mock<IAttendanceMutationValidator>();
        validator.Setup(v => v.PrepareAsync(It.IsAny<AttendanceMutationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttendanceMutationRequest req, CancellationToken _) =>
                new Result<PreparedAttendanceMutation>
                {
                    Success = true,
                    MethodResult = new PreparedAttendanceMutation
                    {
                        Employee = employee,
                        ShouldWrite = true,
                        Attendance = new AttendanceModel
                        {
                            CompanyId = req.CompanyId,
                            UserId = req.TargetUserId,
                            Date = req.AttendanceDate.ToDateTime(TimeOnly.MinValue),
                            Status = req.StatusCode
                        }
                    }
                });

        var createdRecords = new List<AttendanceModel>();
        var attendanceRepo = new Mock<IAttendanceRepository>();
        attendanceRepo.Setup(a => a.AddAsync(It.IsAny<AttendanceModel>()))
            .ReturnsAsync((AttendanceModel m) =>
            {
                createdRecords.Add(m);
                return m;
            });

        var service = new AttendanceInitializationService(
            empRepo.Object,
            statusRepo.Object,
            summariesRepo.Object,
            workingCalendar.Object,
            validator.Object,
            attendanceRepo.Object);

        // Act
        var result = await service.InitializeEmployeeAttendanceAsync(
            TestCompanyId,
            ActorUserId,
            employee.UserId,
            month: new DateTime(2026, 10, 1),
            throughDate: new DateTime(2026, 10, 2));

        // Assert: Started from DateOfJoining (Oct 1), not CreatedDate (Oct 4)
        Assert.True(result.Success);
        Assert.Equal(2, result.MethodResult.Created);
        Assert.Contains(createdRecords, r => r.Date.Day == 1);
    }

    [Fact]
    public async Task InitializeEmployeeAttendanceAsync_ExistingAttendanceRecord_IsNotOverwritten()
    {
        // Arrange
        var employee = new EmpUser
        {
            CompanyId = TestCompanyId,
            UserId = "user-104",
            EmployeeId = "EMP-104",
            FirstName = "Existing",
            LastName = "User",
            Email = "existing@example.com",
            DateOfJoining = "2026-10-01",
            Status = true
        };

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(r => r.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(employee);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceStatusSetting> { new() { CompanyId = TestCompanyId, Code = "P", Name = "Present", IsActive = true } });

        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        workingCalendar.Setup(c => c.GetWorkingDatesAsync(TestCompanyId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DateOnly> { new(2026, 10, 1) });

        var validator = new Mock<IAttendanceMutationValidator>();
        // Return ShouldWrite = false because record already exists
        validator.Setup(v => v.PrepareAsync(It.IsAny<AttendanceMutationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<PreparedAttendanceMutation>
            {
                Success = true,
                MethodResult = new PreparedAttendanceMutation
                {
                    Employee = employee,
                    Attendance = new AttendanceModel { CompanyId = TestCompanyId, UserId = employee.UserId, Date = new DateTime(2026, 10, 1), Status = "P" },
                    ShouldWrite = false
                }
            });

        var attendanceRepo = new Mock<IAttendanceRepository>();

        var service = new AttendanceInitializationService(
            empRepo.Object,
            statusRepo.Object,
            new Mock<IMongoDbRepository<MonthlyAttendanceSummary>>().Object,
            workingCalendar.Object,
            validator.Object,
            attendanceRepo.Object);

        // Act
        var result = await service.InitializeEmployeeAttendanceAsync(
            TestCompanyId,
            ActorUserId,
            employee.UserId,
            month: new DateTime(2026, 10, 1),
            throughDate: new DateTime(2026, 10, 1));

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.MethodResult.Created);
        Assert.Equal(1, result.MethodResult.AlreadyExisting);
        attendanceRepo.Verify(a => a.AddAsync(It.IsAny<AttendanceModel>()), Times.Never);
    }

    [Theory]
    [InlineData("2026-10-01", 2026, 10, 1)]
    [InlineData("01-10-2026", 2026, 10, 1)]
    [InlineData("01/10/2026", 2026, 10, 1)]
    [InlineData("10/25/2026", 2026, 10, 25)]
    [InlineData("1 October 2026", 2026, 10, 1)]
    [InlineData("2026-10-01T00:00:00Z", 2026, 10, 1)]
    public void EmploymentPeriod_TryGetJoiningDate_ParsesVariousFormats(string dateStr, int expYear, int expMonth, int expDay)
    {
        var emp = new EmpUser
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            DateOfJoining = dateStr
        };
        var ok = EmploymentPeriod.TryGetJoiningDate(emp, out var parsed);

        Assert.True(ok);
        Assert.Equal(new DateTime(expYear, expMonth, expDay), parsed);
    }

    [Fact]
    public void EmploymentPeriod_TryGetJoiningDate_FallsBackToCreatedDate_WhenDateOfJoiningMissing()
    {
        var created = new DateTime(2026, 9, 25, 14, 0, 0, DateTimeKind.Utc);
        var emp = new EmpUser
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            DateOfJoining = null,
            CreatedDate = created
        };

        var ok = EmploymentPeriod.TryGetJoiningDate(emp, out var parsed);

        Assert.True(ok);
        Assert.Equal(new DateTime(2026, 9, 25), parsed);
    }

    [Fact]
    public async Task InitializeEmployeeAttendanceAsync_RepeatedJoiningDateModifications_MaintainsFlawlessConsistency()
    {
        // Arrange
        var employee = new EmpUser
        {
            CompanyId = TestCompanyId,
            UserId = "user-repeated",
            EmployeeId = "EMP-REP",
            FirstName = "Mohit",
            LastName = "Negi",
            Email = "mohit@example.com",
            DateOfJoining = "2026-10-01",
            Status = true
        };

        var empRepo = new Mock<IMongoDbRepository<EmpUser>>();
        empRepo.Setup(r => r.FirstOrDefault(It.IsAny<Expression<Func<EmpUser, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(employee);

        var statusRepo = new Mock<IMongoDbRepository<AttendanceStatusSetting>>();
        statusRepo.Setup(r => r.GetAll(It.IsAny<Expression<Func<AttendanceStatusSetting, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<AttendanceStatusSetting> { new() { CompanyId = TestCompanyId, Code = "P", Name = "Present", IsActive = true } });

        var summariesRepo = new Mock<IMongoDbRepository<MonthlyAttendanceSummary>>();
        summariesRepo.Setup(r => r.DeleteAll(It.IsAny<FilterDefinition<MonthlyAttendanceSummary>>()))
            .ReturnsAsync(new Result { Success = true });

        // Calendar working days in October 2026 (Mon-Fri)
        var allOctoberWorkingDays = new List<DateOnly>
        {
            new(2026, 10, 1), new(2026, 10, 2),
            new(2026, 10, 5), new(2026, 10, 6), new(2026, 10, 7), new(2026, 10, 8), new(2026, 10, 9),
            new(2026, 10, 12), new(2026, 10, 13), new(2026, 10, 14), new(2026, 10, 15), new(2026, 10, 16),
            new(2026, 10, 19), new(2026, 10, 20)
        };

        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        workingCalendar.Setup(c => c.GetWorkingDatesAsync(TestCompanyId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, DateOnly from, DateOnly to, CancellationToken _) =>
                allOctoberWorkingDays.Where(d => d >= from && d <= to).ToList());

        var attendanceStore = new List<AttendanceModel>();

        var validator = new Mock<IAttendanceMutationValidator>();
        validator.Setup(v => v.PrepareAsync(It.IsAny<AttendanceMutationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttendanceMutationRequest req, CancellationToken _) =>
            {
                var reqDate = req.AttendanceDate.ToDateTime(TimeOnly.MinValue);
                var already = attendanceStore.Any(x => x.Date.Date == reqDate.Date);
                return new Result<PreparedAttendanceMutation>
                {
                    Success = true,
                    MethodResult = new PreparedAttendanceMutation
                    {
                        Employee = employee,
                        ShouldWrite = !already,
                        Attendance = new AttendanceModel
                        {
                            CompanyId = req.CompanyId,
                            UserId = req.TargetUserId,
                            Date = reqDate,
                            Status = req.StatusCode,
                            SourceType = "SYSTEM_DEFAULT_PRESENT"
                        }
                    }
                };
            });

        var attendanceRepo = new Mock<IAttendanceRepository>();
        attendanceRepo.Setup(a => a.AddAsync(It.IsAny<AttendanceModel>()))
            .ReturnsAsync((AttendanceModel m) =>
            {
                attendanceStore.RemoveAll(x => x.Date.Date == m.Date.Date);
                attendanceStore.Add(m);
                return m;
            });

        attendanceRepo.Setup(a => a.DeleteDefaultPresentOutsideEmploymentPeriodAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>()))
            .ReturnsAsync((string _, string _, DateTime joining, DateTime? lastWorking) =>
            {
                var removed = attendanceStore.RemoveAll(x =>
                    x.SourceType == "SYSTEM_DEFAULT_PRESENT" &&
                    (x.Date.Date < joining.Date || (lastWorking.HasValue && x.Date.Date > lastWorking.Value.Date)));
                return (long)removed;
            });

        var service = new AttendanceInitializationService(
            empRepo.Object,
            statusRepo.Object,
            summariesRepo.Object,
            workingCalendar.Object,
            validator.Object,
            attendanceRepo.Object);

        var throughDate = new DateTime(2026, 10, 20);

        // STEP 1: Initial joining date = 2026-10-01
        employee.DateOfJoining = "2026-10-01";
        var res1 = await service.InitializeEmployeeAttendanceAsync(
            TestCompanyId, ActorUserId, employee.UserId, month: new DateTime(2026, 10, 1), throughDate: throughDate);
        Assert.True(res1.Success);
        Assert.Equal(14, attendanceStore.Count);
        Assert.Contains(attendanceStore, x => x.Date.Day == 1);
        Assert.Contains(attendanceStore, x => x.Date.Day == 20);

        // STEP 2: Admin changes joining date forward to 2026-10-15 (joined late)
        employee.DateOfJoining = "2026-10-15";
        var res2 = await service.InitializeEmployeeAttendanceAsync(
            TestCompanyId, ActorUserId, employee.UserId, month: new DateTime(2026, 10, 1), throughDate: throughDate);
        Assert.True(res2.Success);
        // Oct 1..14 records must be removed! Only Oct 15, 16, 19, 20 (4 working days) remain.
        Assert.Equal(4, attendanceStore.Count);
        Assert.All(attendanceStore, x => Assert.True(x.Date.Day >= 15));
        Assert.DoesNotContain(attendanceStore, x => x.Date.Day < 15);

        // STEP 3: Admin corrects joining date back to 2026-10-08
        employee.DateOfJoining = "2026-10-08";
        var res3 = await service.InitializeEmployeeAttendanceAsync(
            TestCompanyId, ActorUserId, employee.UserId, month: new DateTime(2026, 10, 1), throughDate: throughDate);
        Assert.True(res3.Success);
        // Working days Oct 8..20 must now exist (Oct 8, 9, 12, 13, 14, 15, 16, 19, 20 = 9 days)
        Assert.Equal(9, attendanceStore.Count);
        Assert.All(attendanceStore, x => Assert.True(x.Date.Day >= 8));
        Assert.DoesNotContain(attendanceStore, x => x.Date.Day < 8);

        // STEP 4: Admin changes joining date to 2026-10-20
        employee.DateOfJoining = "2026-10-20";
        var res4 = await service.InitializeEmployeeAttendanceAsync(
            TestCompanyId, ActorUserId, employee.UserId, month: new DateTime(2026, 10, 1), throughDate: throughDate);
        Assert.True(res4.Success);
        // Only 1 record (Oct 20) must remain!
        Assert.Single(attendanceStore);
        Assert.Equal(20, attendanceStore[0].Date.Day);

        // STEP 5: Admin changes joining date back to 2026-10-01 (Day 1)
        employee.DateOfJoining = "2026-10-01";
        var res5 = await service.InitializeEmployeeAttendanceAsync(
            TestCompanyId, ActorUserId, employee.UserId, month: new DateTime(2026, 10, 1), throughDate: throughDate);
        Assert.True(res5.Success);
        // All 14 working days from Day 1 to Day 20 are present with zero duplicates!
        Assert.Equal(14, attendanceStore.Count);
        Assert.Equal(14, attendanceStore.Select(x => x.Date.Date).Distinct().Count());
        Assert.Contains(attendanceStore, x => x.Date.Day == 1);
        Assert.Contains(attendanceStore, x => x.Date.Day == 20);
    }
}
