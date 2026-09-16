using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;

namespace Codeji.CMS.Services.Tests;

public sealed class OffboardingPolicyResolverTests
{
    [Fact]
    public void Resolves_department_before_role_and_company()
    {
        var policies = new[]
        {
            new EmployeeExitPolicy { EmployeeExitPolicyId = "company", ScopeKey = "Company" },
            new EmployeeExitPolicy { EmployeeExitPolicyId = "role", RoleId = "r1", ScopeKey = "Role:r1" },
            new EmployeeExitPolicy { EmployeeExitPolicyId = "department", DepartmentId = "d1", ScopeKey = "Department:d1" }
        };

        var result = OffboardingPolicyResolver.Resolve(policies, new() { DepartmentId = "d1", RoleId = "r1" });

        Assert.NotNull(result);
        Assert.Equal("department", result!.Policy.EmployeeExitPolicyId);
        Assert.Equal("Department:d1", result.SourceScope);
    }

    [Fact]
    public void Falls_back_to_company_when_scope_does_not_match()
    {
        var result = OffboardingPolicyResolver.Resolve(
            new[] { new EmployeeExitPolicy { EmployeeExitPolicyId = "company" } },
            new() { DepartmentId = "other", RoleId = "other" });

        Assert.NotNull(result);
        Assert.Equal("Company", result!.SourceScope);
        Assert.True(result.Inherited);
    }

    [Fact]
    public void Ignores_inactive_policies_and_returns_null_without_default()
    {
        var result = OffboardingPolicyResolver.Resolve(
            new[] { new EmployeeExitPolicy { EmployeeExitPolicyId = "inactive", IsActive = false } },
            new());

        Assert.Null(result);
    }
}
