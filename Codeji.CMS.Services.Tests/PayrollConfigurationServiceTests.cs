using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollConfigurationServiceTests
{
    [Fact]
    public async Task SaveAssignment_RejectsPayGroupFromAnotherTenant()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-2", Name = "Other", EffectiveFrom = DateTime.UtcNow, IsActive = true } });
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }

    [Fact]
    public async Task SaveAssignment_RejectsOverlappingEffectivePeriod()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = DateTime.UtcNow, IsActive = true } });
        var assignments = Repository(new[] { new EmployeePayrollAssignment { Id = "existing", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 1, 1), IsApproved = true } });
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }

    [Fact]
    public async Task SaveAssignment_RejectsPolicyProfileFromAnotherTenant()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = DateTime.UtcNow, IsActive = true } });
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var policies = Repository(new[] { new PayrollPolicyProfile { Id = "policy-1", CompanyId = "company-2", Name = "Other", EffectiveFrom = DateTime.UtcNow, IsActive = true } });
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, policies.Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PayGroupId = "group-1", PolicyProfileId = "policy-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }
    [Fact]
    public async Task SaveAssignment_RejectsEmployeeFromAnotherTenant()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = DateTime.UtcNow, IsActive = true } });
        var assignments = Repository(Array.Empty<EmployeePayrollAssignment>());
        var employees = Repository(new[] { new EmpUser { UserId = "user-1", EmployeeId = "employee-1", CompanyId = "company-2", FirstName = "Other", LastName = null, Email = "other@example.com" } });
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object, employees.Object);

        var result = await service.SaveAssignment("company-1", "actor", new EmployeePayrollAssignmentDto
        {
            EmployeeId = "employee-1", PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        assignments.Verify(x => x.AddOne(It.IsAny<EmployeePayrollAssignment>()), Times.Never);
    }
    [Fact]
    public async Task PreviewBulkAssignments_IsCompanyScopedAndFlagsOverlaps()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = DateTime.UtcNow, IsActive = true } });
        var assignments = Repository(new[] { new EmployeePayrollAssignment { Id = "existing", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 1, 1), IsApproved = true } });
        var employees = Repository(new[]
        {
            new EmpUser { EmployeeId = "employee-1", CompanyId = "company-1", FirstName = "Ready?", LastName = "No", Department = "Engineering", JobRole = "Developer", Email = "employee1@example.com" },
            new EmpUser { EmployeeId = "employee-2", CompanyId = "company-1", FirstName = "Ready", LastName = "Yes", Department = "Engineering", JobRole = "Developer", Email = "employee1@example.com" },
            new EmpUser { EmployeeId = "employee-3", CompanyId = "company-2", FirstName = "Other", LastName = "Tenant", Department = "Engineering", JobRole = "Developer", Email = "employee1@example.com" },
        });
        var service = new PayrollConfigurationService(groups.Object, assignments.Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object, employees.Object);

        var result = await service.PreviewBulkAssignments("company-1", new PayrollBulkAssignmentRequestDto
        {
            PayGroupId = "group-1", Department = "Engineering", JobRole = "Developer", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.True(result.Success);
        Assert.Equal(new[] { "employee-1", "employee-2" }, result.MethodResults.Select(x => x.EmployeeId));
        Assert.False(result.MethodResults.Single(x => x.EmployeeId == "employee-1").CanSubmit);
        Assert.True(result.MethodResults.Single(x => x.EmployeeId == "employee-2").CanSubmit);
    }

    [Fact]
    public async Task PreviewBulkAssignments_RejectsPayGroupFromAnotherTenant()
    {
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-2", CompanyId = "company-2", Name = "Other", EffectiveFrom = DateTime.UtcNow, IsActive = true } });
        var service = new PayrollConfigurationService(groups.Object, Repository(Array.Empty<EmployeePayrollAssignment>()).Object, Repository(Array.Empty<SalaryTemplate>()).Object, Repository(Array.Empty<PayrollPolicyProfile>()).Object, Repository(Array.Empty<EmpUser>()).Object);

        var result = await service.PreviewBulkAssignments("company-1", new PayrollBulkAssignmentRequestDto
        {
            PayGroupId = "group-2", EffectiveFrom = new DateTime(2026, 9, 1)
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }
    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>(IEnumerable<TEntity> items)
    {
        var source = items.ToArray();
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _) => Task.FromResult(source.Any(predicate.Compile())));
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult<IEnumerable<TEntity>>(source.Where(predicate.Compile()).ToArray()));
        repository.Setup(x => x.AddOne(It.IsAny<TEntity>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        return repository;
    }
}