using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Services.Attendance;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class OfficeScheduleSettingsServiceTests
{
    [Fact]
    public async Task EnsureDefaultScheduleAsync_WhenNoScheduleExists_CreatesAndPersistsDefaultSchedule()
    {
        const string companyId = "tenant-new";
        var persisted = new List<CompanyOfficeSchedule>();

        var schedulesRepo = new Mock<IMongoDbRepository<CompanyOfficeSchedule>>();
        schedulesRepo
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<CompanyOfficeSchedule, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<CompanyOfficeSchedule, bool>> predicate, bool _, bool _) =>
                Task.FromResult<IEnumerable<CompanyOfficeSchedule>>(persisted.Where(predicate.Compile()).ToArray()));

        schedulesRepo
            .Setup(x => x.AddOne(It.IsAny<CompanyOfficeSchedule>()))
            .Callback<CompanyOfficeSchedule>(persisted.Add)
            .ReturnsAsync(new Result { Success = true });

        var employeeAssignments = new Mock<IMongoDbRepository<EmployeeScheduleAssignment>>();
        var departmentAssignments = new Mock<IMongoDbRepository<DepartmentScheduleAssignment>>();

        var service = new OfficeScheduleSettingsService(schedulesRepo.Object, employeeAssignments.Object, departmentAssignments.Object);

        var result = await service.EnsureDefaultScheduleAsync(companyId);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal("Default shift", result.MethodResult.Name);
        Assert.True(result.MethodResult.IsDefault);
        Assert.Equal("09:00", result.MethodResult.StandardCheckIn);
        Assert.Equal("18:00", result.MethodResult.StandardCheckOut);
        Assert.Equal(15, result.MethodResult.GraceMinutes);
        Assert.Equal(60, result.MethodResult.BreakMinutes);
        Assert.Equal(480, result.MethodResult.RequiredWorkingMinutes);

        schedulesRepo.Verify(x => x.AddOne(It.Is<CompanyOfficeSchedule>(s =>
            s.CompanyId == companyId &&
            s.IsDefault &&
            s.Name == "Default shift" &&
            s.GraceMinutes == 15)), Times.Once);
        Assert.Single(persisted);
    }

    [Fact]
    public async Task EnsureDefaultScheduleAsync_WhenDefaultScheduleExists_ReturnsExistingWithoutInserting()
    {
        const string companyId = "tenant-existing";
        var existing = new CompanyOfficeSchedule
        {
            CompanyId = companyId,
            ScheduleId = "existing-shift-1",
            Name = "Existing General Shift",
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(19),
            GraceMinutes = 20,
            BreakMinutes = 60,
            RequiredWorkingMinutes = 480,
            IsDefault = true,
            IsActive = true
        };
        var persisted = new List<CompanyOfficeSchedule> { existing };

        var schedulesRepo = new Mock<IMongoDbRepository<CompanyOfficeSchedule>>();
        schedulesRepo
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<CompanyOfficeSchedule, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<CompanyOfficeSchedule, bool>> predicate, bool _, bool _) =>
                Task.FromResult<IEnumerable<CompanyOfficeSchedule>>(persisted.Where(predicate.Compile()).ToArray()));

        schedulesRepo.Setup(x => x.AddOne(It.IsAny<CompanyOfficeSchedule>())).ReturnsAsync(new Result { Success = true });

        var employeeAssignments = new Mock<IMongoDbRepository<EmployeeScheduleAssignment>>();
        var departmentAssignments = new Mock<IMongoDbRepository<DepartmentScheduleAssignment>>();

        var service = new OfficeScheduleSettingsService(schedulesRepo.Object, employeeAssignments.Object, departmentAssignments.Object);

        var result = await service.EnsureDefaultScheduleAsync(companyId);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal("existing-shift-1", result.MethodResult.ScheduleId);
        Assert.Equal("Existing General Shift", result.MethodResult.Name);
        Assert.Equal(20, result.MethodResult.GraceMinutes);

        schedulesRepo.Verify(x => x.AddOne(It.IsAny<CompanyOfficeSchedule>()), Times.Never);
        Assert.Single(persisted);
    }

    [Fact]
    public async Task GetAsync_WhenNoScheduleExists_EnsuresDefaultSchedulePersisted()
    {
        const string companyId = "tenant-get-empty";
        var persisted = new List<CompanyOfficeSchedule>();

        var schedulesRepo = new Mock<IMongoDbRepository<CompanyOfficeSchedule>>();
        schedulesRepo
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<CompanyOfficeSchedule, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<CompanyOfficeSchedule, bool>> predicate, bool _, bool _) =>
                Task.FromResult<IEnumerable<CompanyOfficeSchedule>>(persisted.Where(predicate.Compile()).ToArray()));

        schedulesRepo
            .Setup(x => x.AddOne(It.IsAny<CompanyOfficeSchedule>()))
            .Callback<CompanyOfficeSchedule>(persisted.Add)
            .ReturnsAsync(new Result { Success = true });

        var employeeAssignments = new Mock<IMongoDbRepository<EmployeeScheduleAssignment>>();
        var departmentAssignments = new Mock<IMongoDbRepository<DepartmentScheduleAssignment>>();

        var service = new OfficeScheduleSettingsService(schedulesRepo.Object, employeeAssignments.Object, departmentAssignments.Object);

        var result = await service.GetAsync(companyId);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal("Default shift", result.MethodResult.Name);
        Assert.True(result.MethodResult.IsDefault);
        schedulesRepo.Verify(x => x.AddOne(It.IsAny<CompanyOfficeSchedule>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoScheduleExists_EnsuresDefaultSchedulePersisted()
    {
        const string companyId = "tenant-getall-empty";
        var persisted = new List<CompanyOfficeSchedule>();

        var schedulesRepo = new Mock<IMongoDbRepository<CompanyOfficeSchedule>>();
        schedulesRepo
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<CompanyOfficeSchedule, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<CompanyOfficeSchedule, bool>> predicate, bool _, bool _) =>
                Task.FromResult<IEnumerable<CompanyOfficeSchedule>>(persisted.Where(predicate.Compile()).ToArray()));

        schedulesRepo
            .Setup(x => x.AddOne(It.IsAny<CompanyOfficeSchedule>()))
            .Callback<CompanyOfficeSchedule>(persisted.Add)
            .ReturnsAsync(new Result { Success = true });

        var employeeAssignments = new Mock<IMongoDbRepository<EmployeeScheduleAssignment>>();
        var departmentAssignments = new Mock<IMongoDbRepository<DepartmentScheduleAssignment>>();

        var service = new OfficeScheduleSettingsService(schedulesRepo.Object, employeeAssignments.Object, departmentAssignments.Object);

        var result = await service.GetAllAsync(companyId);

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResults);
        var item = Assert.Single(result.MethodResults);
        Assert.Equal("Default shift", item.Name);
        Assert.True(item.IsDefault);
        schedulesRepo.Verify(x => x.AddOne(It.IsAny<CompanyOfficeSchedule>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DefaultShift_CannotBeDeleted()
    {
        const string companyId = "tenant-del-def";
        var defaultSchedule = new CompanyOfficeSchedule
        {
            CompanyId = companyId,
            ScheduleId = "def-shift",
            Name = "Default shift",
            IsDefault = true,
            IsActive = true
        };

        var schedulesRepo = new Mock<IMongoDbRepository<CompanyOfficeSchedule>>();
        schedulesRepo
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<CompanyOfficeSchedule, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { defaultSchedule });

        var service = new OfficeScheduleSettingsService(
            schedulesRepo.Object,
            new Mock<IMongoDbRepository<EmployeeScheduleAssignment>>().Object,
            new Mock<IMongoDbRepository<DepartmentScheduleAssignment>>().Object);

        var result = await service.DeleteAsync(companyId, "admin-1", "def-shift");

        Assert.False(result.Success);
        Assert.Equal("OFFICE_SCHEDULE_DEFAULT_CANNOT_BE_DELETED", result.Message);
        schedulesRepo.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<CompanyOfficeSchedule>>(), It.IsAny<UpdateDefinition<CompanyOfficeSchedule>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShiftInUseByEmployee_DeactivatesAssignmentAndDeletesSuccessfully()
    {
        const string companyId = "tenant-del-emp";
        var customSchedule = new CompanyOfficeSchedule
        {
            CompanyId = companyId,
            ScheduleId = "custom-shift",
            Name = "Night Shift",
            IsDefault = false,
            IsActive = true
        };

        var schedulesRepo = new Mock<IMongoDbRepository<CompanyOfficeSchedule>>();
        schedulesRepo
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<CompanyOfficeSchedule, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { customSchedule });
        schedulesRepo
            .Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<CompanyOfficeSchedule>>(), It.IsAny<UpdateDefinition<CompanyOfficeSchedule>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Result { Success = true });

        var employeeAssignments = new Mock<IMongoDbRepository<EmployeeScheduleAssignment>>();
        employeeAssignments
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<EmployeeScheduleAssignment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { new EmployeeScheduleAssignment { CompanyId = companyId, ScheduleId = "custom-shift", IsActive = true } });
        employeeAssignments
            .Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<EmployeeScheduleAssignment>>(), It.IsAny<UpdateDefinition<EmployeeScheduleAssignment>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Result { Success = true });

        var departmentAssignments = new Mock<IMongoDbRepository<DepartmentScheduleAssignment>>();
        departmentAssignments
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<DepartmentScheduleAssignment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Array.Empty<DepartmentScheduleAssignment>());

        var reconciliation = new Mock<IShiftAttendanceReconciliationService>();
        var service = new OfficeScheduleSettingsService(schedulesRepo.Object, employeeAssignments.Object, departmentAssignments.Object, reconciliation.Object);

        var result = await service.DeleteAsync(companyId, "admin-1", "custom-shift");

        Assert.True(result.Success);
        Assert.Equal("Office schedule deleted.", result.Message);
        employeeAssignments.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<EmployeeScheduleAssignment>>(), It.IsAny<UpdateDefinition<EmployeeScheduleAssignment>>(), It.IsAny<bool>()), Times.Once);
        schedulesRepo.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<CompanyOfficeSchedule>>(), It.IsAny<UpdateDefinition<CompanyOfficeSchedule>>(), It.IsAny<bool>()), Times.Once);
        reconciliation.Verify(x => x.ReconcileTodayShiftAttendanceAsync(companyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShiftInUseByDepartment_DeactivatesAssignmentAndDeletesSuccessfully()
    {
        const string companyId = "tenant-del-dept";
        var customSchedule = new CompanyOfficeSchedule
        {
            CompanyId = companyId,
            ScheduleId = "custom-shift",
            Name = "Sales Shift",
            IsDefault = false,
            IsActive = true
        };

        var schedulesRepo = new Mock<IMongoDbRepository<CompanyOfficeSchedule>>();
        schedulesRepo
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<CompanyOfficeSchedule, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { customSchedule });
        schedulesRepo
            .Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<CompanyOfficeSchedule>>(), It.IsAny<UpdateDefinition<CompanyOfficeSchedule>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Result { Success = true });

        var employeeAssignments = new Mock<IMongoDbRepository<EmployeeScheduleAssignment>>();
        employeeAssignments
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<EmployeeScheduleAssignment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Array.Empty<EmployeeScheduleAssignment>());

        var departmentAssignments = new Mock<IMongoDbRepository<DepartmentScheduleAssignment>>();
        departmentAssignments
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<DepartmentScheduleAssignment, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { new DepartmentScheduleAssignment { CompanyId = companyId, ScheduleId = "custom-shift", IsActive = true } });
        departmentAssignments
            .Setup(x => x.UpdateMany(It.IsAny<FilterDefinition<DepartmentScheduleAssignment>>(), It.IsAny<UpdateDefinition<DepartmentScheduleAssignment>>(), It.IsAny<bool>()))
            .ReturnsAsync(new Result { Success = true });

        var reconciliation = new Mock<IShiftAttendanceReconciliationService>();
        var service = new OfficeScheduleSettingsService(schedulesRepo.Object, employeeAssignments.Object, departmentAssignments.Object, reconciliation.Object);

        var result = await service.DeleteAsync(companyId, "admin-1", "custom-shift");

        Assert.True(result.Success);
        Assert.Equal("Office schedule deleted.", result.Message);
        departmentAssignments.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<DepartmentScheduleAssignment>>(), It.IsAny<UpdateDefinition<DepartmentScheduleAssignment>>(), It.IsAny<bool>()), Times.Once);
        schedulesRepo.Verify(x => x.UpdateMany(It.IsAny<FilterDefinition<CompanyOfficeSchedule>>(), It.IsAny<UpdateDefinition<CompanyOfficeSchedule>>(), It.IsAny<bool>()), Times.Once);
        reconciliation.Verify(x => x.ReconcileTodayShiftAttendanceAsync(companyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Security_CrossTenantIsolation_CannotReadOrDeleteOtherTenantShift()
    {
        const string tenantA = "tenant-alpha";
        const string tenantB = "tenant-beta";
        var alphaShift = new CompanyOfficeSchedule
        {
            CompanyId = tenantA,
            ScheduleId = "shift-alpha-1",
            Name = "Alpha Secret Shift",
            IsDefault = false,
            IsActive = true
        };

        var allSchedules = new List<CompanyOfficeSchedule> { alphaShift };

        var schedulesRepo = new Mock<IMongoDbRepository<CompanyOfficeSchedule>>();
        schedulesRepo
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<CompanyOfficeSchedule, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<CompanyOfficeSchedule, bool>> predicate, bool _, bool _) =>
                Task.FromResult<IEnumerable<CompanyOfficeSchedule>>(allSchedules.Where(predicate.Compile()).ToArray()));

        var service = new OfficeScheduleSettingsService(
            schedulesRepo.Object,
            new Mock<IMongoDbRepository<EmployeeScheduleAssignment>>().Object,
            new Mock<IMongoDbRepository<DepartmentScheduleAssignment>>().Object);

        // Tenant B tries to delete Tenant A's shift
        var deleteResult = await service.DeleteAsync(tenantB, "attacker-user", "shift-alpha-1");
        Assert.False(deleteResult.Success);
        Assert.Equal("OFFICE_SCHEDULE_NOT_FOUND", deleteResult.Message);

        // Tenant B tries to modify/hijack Tenant A's shift via SaveAsync
        var saveRequest = new OfficeScheduleSettingsDto
        {
            ScheduleId = "shift-alpha-1",
            Name = "Hijacked Shift",
            StandardCheckIn = "09:00",
            StandardCheckOut = "18:00",
            CheckInAllowedFrom = "08:30",
            CheckInAllowedUntil = "10:00",
            CheckOutAllowedFrom = "17:30",
            CheckOutAllowedUntil = "19:00",
            GraceMinutes = 15,
            BreakMinutes = 60,
            RequiredWorkingMinutes = 480
        };

        var saveResult = await service.SaveAsync(tenantB, "attacker-user", saveRequest);
        Assert.False(saveResult.Success);
        Assert.Equal("OFFICE_SCHEDULE_NOT_FOUND", saveResult.Message);
    }

    [Fact]
    public async Task SaveAsync_DuplicateNameOrTimings_ReturnsError()
    {
        const string companyId = "tenant-dupe";
        var existing = new CompanyOfficeSchedule
        {
            CompanyId = companyId,
            ScheduleId = "shift-1",
            Name = "Morning Shift",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(18),
            CheckInAllowedFrom = TimeSpan.FromHours(8.5),
            CheckInAllowedUntil = TimeSpan.FromHours(10),
            CheckOutAllowedFrom = TimeSpan.FromHours(17.5),
            CheckOutAllowedUntil = TimeSpan.FromHours(19),
            GraceMinutes = 15,
            BreakMinutes = 60,
            RequiredWorkingMinutes = 480,
            IsDefault = true,
            IsActive = true
        };

        var schedulesRepo = new Mock<IMongoDbRepository<CompanyOfficeSchedule>>();
        schedulesRepo
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<CompanyOfficeSchedule, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new[] { existing });

        var service = new OfficeScheduleSettingsService(
            schedulesRepo.Object,
            new Mock<IMongoDbRepository<EmployeeScheduleAssignment>>().Object,
            new Mock<IMongoDbRepository<DepartmentScheduleAssignment>>().Object);

        // Attempt duplicate name
        var dupeNameRequest = new OfficeScheduleSettingsDto
        {
            Name = "Morning Shift", // Same name
            StandardCheckIn = "10:00",
            StandardCheckOut = "19:00",
            CheckInAllowedFrom = "09:30",
            CheckInAllowedUntil = "10:30",
            CheckOutAllowedFrom = "18:30",
            CheckOutAllowedUntil = "19:30",
            GraceMinutes = 15,
            BreakMinutes = 60,
            RequiredWorkingMinutes = 480
        };

        var resultName = await service.SaveAsync(companyId, "admin-1", dupeNameRequest);
        Assert.False(resultName.Success);
        Assert.Equal("OFFICE_SCHEDULE_NAME_EXISTS", resultName.Message);

        // Attempt duplicate timings
        var dupeTimingRequest = new OfficeScheduleSettingsDto
        {
            Name = "Different Shift Name",
            StandardCheckIn = "09:00", // Same start time as shift-1
            StandardCheckOut = "18:00", // Same end time as shift-1
            CheckInAllowedFrom = "08:30",
            CheckInAllowedUntil = "10:00",
            CheckOutAllowedFrom = "17:30",
            CheckOutAllowedUntil = "19:00",
            GraceMinutes = 15,
            BreakMinutes = 60,
            RequiredWorkingMinutes = 480
        };

        var resultTiming = await service.SaveAsync(companyId, "admin-1", dupeTimingRequest);
        Assert.False(resultTiming.Success);
        Assert.Equal("OFFICE_SCHEDULE_TIMING_EXISTS", resultTiming.Message);
    }
}
