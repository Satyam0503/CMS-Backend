using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeSalaryStructureResolverTests
{
    [Fact]
    public void Resolve_UsesApprovedEffectiveRecordWithinTenant()
    {
        var result = EmployeeSalaryStructureResolver.Resolve("company-1", "employee-1", new DateTime(2026, 10, 15),
        [
            new EmployeeSalaryStructure { Id = "old", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 1, 1), EffectiveTo = new DateTime(2026, 9, 30), ApprovalStatus = "APPROVED", Status = "ACTIVE" },
            new EmployeeSalaryStructure { Id = "new", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "APPROVED", Status = "ACTIVE" },
            new EmployeeSalaryStructure { Id = "other", CompanyId = "company-2", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "APPROVED", Status = "ACTIVE" }
        ]);

        Assert.Equal("new", result?.Id);
    }

    [Fact]
    public void Resolve_IgnoresDraftByDefault()
    {
        var result = EmployeeSalaryStructureResolver.Resolve("company-1", "employee-1", new DateTime(2026, 10, 15),
        [new EmployeeSalaryStructure { CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 10, 1), ApprovalStatus = "DRAFT", Status = "DRAFT" }]);

        Assert.Null(result);
    }

    [Fact]
    public void Overlaps_DetectsSameEmployeeOnly()
    {
        var candidate = new EmployeeSalaryStructure { CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 9, 15), EffectiveTo = new DateTime(2026, 10, 15) };
        var existing = new[]
        {
            new EmployeeSalaryStructure { Id = "same", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 10, 1) },
            new EmployeeSalaryStructure { Id = "other-tenant", CompanyId = "company-2", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 10, 1) },
            new EmployeeSalaryStructure { Id = "other-employee", CompanyId = "company-1", EmployeeId = "employee-2", EffectiveFrom = new DateTime(2026, 10, 1) }
        };

        Assert.True(EmployeeSalaryStructureResolver.Overlaps(candidate, existing));
    }
}
