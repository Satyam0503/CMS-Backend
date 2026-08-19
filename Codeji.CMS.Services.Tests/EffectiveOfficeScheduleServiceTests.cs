using System.Linq.Expressions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class EffectiveOfficeScheduleServiceTests
{
    [Fact]
    public async Task ResolveAsync_PrefersEmployeeThenDepartmentThenCompanyDefault_WithinTheAuthenticatedCompany()
    {
        const string companyId = "company-a";
        const string userId = "employee-a";
        const string departmentId = "department-a";
        var day = new DateTime(2026, 8, 19);

        var schedules = Repository(new[]
        {
            Schedule(companyId, "default", "Default", TimeSpan.FromHours(9), isDefault: true),
            Schedule(companyId, "department", "Department", TimeSpan.FromHours(10)),
            Schedule(companyId, "employee", "Employee", TimeSpan.FromHours(12)),
            // A matching ID in a different company must never be selectable.
            Schedule("company-b", "employee", "Other company", TimeSpan.FromHours(6), isDefault: true),
        });
        var employeeAssignments = Repository(new[]
        {
            new EmployeeScheduleAssignment { CompanyId = companyId, UserId = userId, ScheduleId = "employee", EffectiveFrom = day, IsActive = true },
            new EmployeeScheduleAssignment { CompanyId = "company-b", UserId = userId, ScheduleId = "employee", EffectiveFrom = day, IsActive = true },
        });
        var departmentAssignments = Repository(new[]
        {
            new DepartmentScheduleAssignment { CompanyId = companyId, Department = departmentId, ScheduleId = "department", EffectiveFrom = day, IsActive = true },
        });
        var service = new EffectiveOfficeScheduleService(schedules.Object, employeeAssignments.Object, departmentAssignments.Object, Repository(Array.Empty<EmpUser>()).Object);
        var employee = Employee(companyId, userId, departmentId);

        var resolved = await service.ResolveAsync(companyId, employee, day);

        Assert.NotNull(resolved);
        Assert.Equal("employee", resolved.ScheduleId);
        Assert.Equal(TimeSpan.FromHours(12), resolved.StartTime);
    }

    [Fact]
    public async Task ResolveAsync_UsesDepartmentAndThenDefaultWhenThereIsNoEmployeeOverride()
    {
        const string companyId = "company-a";
        var day = new DateTime(2026, 8, 19);
        var schedules = Repository(new[]
        {
            Schedule(companyId, "default", "Default", TimeSpan.FromHours(9), isDefault: true),
            Schedule(companyId, "department", "Department", TimeSpan.FromHours(10)),
        });
        var employeeAssignments = Repository(Array.Empty<EmployeeScheduleAssignment>());
        var departmentAssignments = Repository(new[]
        {
            new DepartmentScheduleAssignment { CompanyId = companyId, Department = "department-a", ScheduleId = "department", EffectiveFrom = day, IsActive = true },
        });
        var service = new EffectiveOfficeScheduleService(schedules.Object, employeeAssignments.Object, departmentAssignments.Object, Repository(Array.Empty<EmpUser>()).Object);

        var departmentSchedule = await service.ResolveAsync(companyId, Employee(companyId, "employee-a", "department-a"), day);
        var defaultSchedule = await service.ResolveAsync(companyId, Employee(companyId, "employee-b", "department-b"), day);

        Assert.Equal("department", departmentSchedule?.ScheduleId);
        Assert.Equal("default", defaultSchedule?.ScheduleId);
    }

    private static CompanyOfficeSchedule Schedule(string companyId, string id, string name, TimeSpan start, bool isDefault = false) => new()
    {
        CompanyId = companyId,
        ScheduleId = id,
        Name = name,
        StartTime = start,
        EndTime = start.Add(TimeSpan.FromHours(8)),
        EffectiveFrom = new DateTime(2026, 1, 1),
        IsActive = true,
        IsDefault = isDefault,
    };

    private static EmpUser Employee(string companyId, string userId, string department) => new()
    {
        CompanyId = companyId,
        UserId = userId,
        Department = department,
        FirstName = "Test",
        LastName = "Employee",
        Email = $"{userId}@example.test",
    };

    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>(IEnumerable<TEntity> items)
    {
        var source = items.ToArray();
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) =>
                Task.FromResult<IEnumerable<TEntity>>(source.Where(predicate.Compile()).ToArray()));
        return repository;
    }
}
