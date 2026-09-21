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

    [Fact]
    public void Resolves_exit_specific_scope_before_generic_scope()
    {
        var policies = new[]
        {
            new EmployeeExitPolicy { EmployeeExitPolicyId = "department-generic", DepartmentId = "d1", ScopeKey = "Department:d1" },
            new EmployeeExitPolicy { EmployeeExitPolicyId = "role-retired", RoleId = "r1", ExitType = EmployeeLifecycleStatuses.Retired, ScopeKey = "Role:r1|ExitType:Retired" },
            new EmployeeExitPolicy { EmployeeExitPolicyId = "company-retired", ExitType = EmployeeLifecycleStatuses.Retired, ScopeKey = "Company|ExitType:Retired" }
        };

        var result = OffboardingPolicyResolver.Resolve(policies, new()
        {
            DepartmentId = "d1",
            RoleId = "r1",
            ExitType = EmployeeLifecycleStatuses.Retired
        });

        Assert.NotNull(result);
        Assert.Equal("role-retired", result!.Policy.EmployeeExitPolicyId);
        Assert.Equal("Role:r1", result.SourceScope);
        Assert.Equal(EmployeeLifecycleStatuses.Retired, result.ExitType);
    }

    [Fact]
    public void Falls_back_to_generic_policy_when_exit_specific_policy_does_not_exist()
    {
        var result = OffboardingPolicyResolver.Resolve(
            new[] { new EmployeeExitPolicy { EmployeeExitPolicyId = "company", ScopeKey = "Company" } },
            new() { ExitType = EmployeeLifecycleStatuses.Deceased });

        Assert.NotNull(result);
        Assert.Equal("company", result!.Policy.EmployeeExitPolicyId);
        Assert.Null(result.ExitType);
    }

    [Fact]
    public void Resolves_exit_type_specific_rejoin_policy_before_generic_policy()
    {
        var result = OffboardingPolicyResolver.Resolve(
            new[]
            {
                new EmployeeExitPolicy { EmployeeExitPolicyId = "generic", AllowRejoin = true },
                new EmployeeExitPolicy { EmployeeExitPolicyId = "termination", ExitType = EmployeeLifecycleStatuses.Terminated, AllowRejoin = false }
            },
            new() { ExitType = EmployeeLifecycleStatuses.Terminated });

        Assert.NotNull(result);
        Assert.Equal("termination", result!.Policy.EmployeeExitPolicyId);
        Assert.False(result.Policy.AllowRejoin);
        Assert.Equal(EmployeeLifecycleStatuses.Terminated, result.ExitType);
    }
}
