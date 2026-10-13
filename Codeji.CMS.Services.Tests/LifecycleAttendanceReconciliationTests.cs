using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public class LifecycleAttendanceReconciliationTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";

    [Fact]
    public async Task ReconcileCaseAttendance_WritesLifecycleAttendanceRows_ScopedToTenant()
    {
        var caseId = "case-101";
        var empUser = new EmpUser
        {
            UserId = "user-1",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            EmployeeId = "EMP-01",
            CompanyId = TenantA,
            DateOfJoining = "2026-01-01",
            Status = true
        };

        var lifecycleCase = new EmployeeLifecycleCase
        {
            CaseId = caseId,
            CompanyId = TenantA,
            EmployeeUserId = empUser.UserId,
            LifecycleStatus = EmployeeLifecycleStatuses.MedicalLeave,
            EffectiveFromUtc = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc),
            ExpectedReturnDate = new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc),
            Status = "Active",
            Version = 1
        };

        var medStatus = new AttendanceStatusSetting
        {
            CompanyId = TenantA,
            Code = "MED",
            Name = "Medical Leave",
            IsActive = true,
            PaidDayFraction = 1m
        };

        var cases = InMemoryRepository.Create(lifecycleCase);
        var lifecyclePolicies = InMemoryRepository.Create<EmployeeLifecyclePolicy>();
        var leavePolicies = InMemoryRepository.Create<LeavePolicy>();
        var employees = InMemoryRepository.Create(empUser);
        var statuses = InMemoryRepository.Create(medStatus);
        var attendanceList = new List<AttendanceModel>();
        var attendance = CreateListBackedTestRepository(attendanceList);
        var segments = InMemoryRepository.Create<AttendanceDaySegment>();
        var exceptions = InMemoryRepository.Create<AttendancePayrollException>();
        var summaries = InMemoryRepository.Create<MonthlyAttendanceSummary>();

        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        // Return 5 working days for test period
        workingCalendar.Setup(x => x.GetLeaveDatesAsync(TenantA, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DateOnly>
            {
                new(2026, 9, 11),
                new(2026, 9, 14),
                new(2026, 9, 15),
                new(2026, 9, 16),
                new(2026, 9, 17)
            });

        var initialization = new Mock<IAttendanceInitializationService>();

        var service = new LifecycleAttendanceReconciliationService(
            cases.Object, lifecyclePolicies.Object, leavePolicies.Object, employees.Object,
            statuses.Object, attendance.Object, segments.Object, exceptions.Object,
            summaries.Object, workingCalendar.Object, initialization.Object);

        // Act
        var result = await service.ReconcileCaseAttendanceAsync(TenantA, caseId, "hr-admin");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5, attendanceList.Count);
        Assert.All(attendanceList, row =>
        {
            Assert.Equal(TenantA, row.CompanyId);
            Assert.Equal(empUser.UserId, row.UserId);
            Assert.Equal(empUser.EmployeeId, row.EmployeeId);
            Assert.Equal("MED", row.Status);
            Assert.Equal("LIFECYCLE_CASE", row.SourceType);
            Assert.Equal(caseId, row.SourceId);
        });
    }

    [Fact]
    public async Task ReconcileCaseAttendance_CrossTenantAccess_ReturnsNotFound()
    {
        var lifecycleCase = new EmployeeLifecycleCase
        {
            CaseId = "case-tenant-a",
            CompanyId = TenantA,
            EmployeeUserId = "user-1",
            LifecycleStatus = EmployeeLifecycleStatuses.MedicalLeave,
            EffectiveFromUtc = new DateTime(2026, 9, 11),
            ExpectedReturnDate = new DateTime(2026, 9, 30)
        };

        var cases = InMemoryRepository.Create(lifecycleCase);
        var service = new LifecycleAttendanceReconciliationService(
            cases.Object,
            InMemoryRepository.Create<EmployeeLifecyclePolicy>().Object,
            InMemoryRepository.Create<LeavePolicy>().Object,
            InMemoryRepository.Create<EmpUser>().Object,
            InMemoryRepository.Create<AttendanceStatusSetting>().Object,
            InMemoryRepository.Create<AttendanceModel>().Object,
            InMemoryRepository.Create<AttendanceDaySegment>().Object,
            InMemoryRepository.Create<AttendancePayrollException>().Object,
            InMemoryRepository.Create<MonthlyAttendanceSummary>().Object,
            new Mock<ICompanyWorkingCalendarService>().Object,
            new Mock<IAttendanceInitializationService>().Object);

        // Attempt to reconcile Tenant A's case using Tenant B
        var result = await service.ReconcileCaseAttendanceAsync(TenantB, "case-tenant-a", "attacker");

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleReturnToWork_DeletesRecordsOnOrAfterReturnDate()
    {
        var caseId = "case-rtw";
        var empUser = new EmpUser { UserId = "user-1", FirstName = "Test", LastName = "User", Email = "test@example.com", EmployeeId = "EMP-01", CompanyId = TenantA, DateOfJoining = "2026-01-01", Status = true };
        var lifecycleCase = new EmployeeLifecycleCase
        {
            CaseId = caseId,
            CompanyId = TenantA,
            EmployeeUserId = empUser.UserId,
            LifecycleStatus = EmployeeLifecycleStatuses.MedicalLeave,
            EffectiveFromUtc = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc),
            ExpectedReturnDate = new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc),
            Status = "Completed",
            ActualReturnDate = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc)
        };

        var attendanceList = new List<AttendanceModel>
        {
            new() { AttendanceId = "1", CompanyId = TenantA, UserId = "user-1", Date = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc), SourceType = "LIFECYCLE_CASE", SourceId = caseId, Status = "MED" },
            new() { AttendanceId = "2", CompanyId = TenantA, UserId = "user-1", Date = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc), SourceType = "LIFECYCLE_CASE", SourceId = caseId, Status = "MED" },
            new() { AttendanceId = "3", CompanyId = TenantA, UserId = "user-1", Date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc), SourceType = "LIFECYCLE_CASE", SourceId = caseId, Status = "MED" },
            // Rows on or after actual return date (Sep 16)
            new() { AttendanceId = "4", CompanyId = TenantA, UserId = "user-1", Date = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc), SourceType = "LIFECYCLE_CASE", SourceId = caseId, Status = "MED" },
            new() { AttendanceId = "5", CompanyId = TenantA, UserId = "user-1", Date = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc), SourceType = "LIFECYCLE_CASE", SourceId = caseId, Status = "MED" },
        };

        var cases = InMemoryRepository.Create(lifecycleCase);
        var attendance = CreateListBackedTestRepository(attendanceList);
        attendance.Setup(x => x.DeleteAll(It.IsAny<FilterDefinition<AttendanceModel>>()))
            .Callback<FilterDefinition<AttendanceModel>>(_ =>
            {
                attendanceList.RemoveAll(x => x.Date >= new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc) && x.SourceId == caseId);
            })
            .ReturnsAsync(new Result { Success = true });
        var workingCalendar = new Mock<ICompanyWorkingCalendarService>();
        workingCalendar.Setup(x => x.GetLeaveDatesAsync(TenantA, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DateOnly> { new(2026, 9, 11), new(2026, 9, 14), new(2026, 9, 15) });

        var medStatus = new AttendanceStatusSetting { CompanyId = TenantA, Code = "MED", Name = "Medical Leave", IsActive = true, PaidDayFraction = 1m };

        var service = new LifecycleAttendanceReconciliationService(
            cases.Object,
            InMemoryRepository.Create<EmployeeLifecyclePolicy>().Object,
            InMemoryRepository.Create<LeavePolicy>().Object,
            InMemoryRepository.Create(empUser).Object,
            InMemoryRepository.Create(medStatus).Object,
            attendance.Object,
            InMemoryRepository.Create<AttendanceDaySegment>().Object,
            InMemoryRepository.Create<AttendancePayrollException>().Object,
            InMemoryRepository.Create<MonthlyAttendanceSummary>().Object,
            workingCalendar.Object,
            new Mock<IAttendanceInitializationService>().Object);

        // Act
        var result = await service.HandleReturnToWorkAsync(TenantA, caseId, new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc), "hr-admin");

        // Assert
        Assert.True(result.Success);
        // Rows on or after Sep 16 must have been removed
        Assert.DoesNotContain(attendanceList, x => x.Date >= new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc) && x.SourceId == caseId);
        // Rows before Sep 16 must be retained
        Assert.Equal(3, attendanceList.Count(x => x.SourceId == caseId));
    }

    private static Mock<IMongoDbRepository<AttendanceModel>> CreateListBackedTestRepository(List<AttendanceModel> list)
    {
        var repo = new Mock<IMongoDbRepository<AttendanceModel>>();
        repo.Setup(x => x.GetAll(It.IsAny<Expression<Func<AttendanceModel, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<AttendanceModel, bool>> predicate, bool _, bool _) => list.Where(predicate.Compile()).ToList());
        repo.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<AttendanceModel, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<AttendanceModel, bool>> predicate, bool _, bool _) => list.FirstOrDefault(predicate.Compile()));
        repo.Setup(x => x.AddOne(It.IsAny<AttendanceModel>()))
            .Callback<AttendanceModel>(item => list.Add(item))
            .ReturnsAsync(new Result { Success = true });
        repo.Setup(x => x.Update(It.IsAny<FilterDefinition<AttendanceModel>>(), It.IsAny<AttendanceModel>()))
            .Callback<FilterDefinition<AttendanceModel>, AttendanceModel>((_, updated) =>
            {
                var idx = list.FindIndex(x => x.AttendanceId == updated.AttendanceId);
                if (idx >= 0) list[idx] = updated;
            })
            .ReturnsAsync(new Result { Success = true });
        repo.Setup(x => x.Delete(It.IsAny<FilterDefinition<AttendanceModel>>()))
            .ReturnsAsync(new Result { Success = true });
        repo.Setup(x => x.DeleteAll(It.IsAny<FilterDefinition<AttendanceModel>>()))
            .ReturnsAsync(new Result { Success = true });
        return repo;
    }
}
