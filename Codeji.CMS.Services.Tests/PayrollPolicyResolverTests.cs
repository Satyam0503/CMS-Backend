using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollPolicyResolverTests
{
    [Fact]
    public void ResolveAssignment_UsesLatestApprovedEffectiveAssignmentWithinTenant()
    {
        var result = PayrollPolicyResolver.ResolveAssignment("company-1", "employee-1", new DateTime(2026, 9, 15),
        [
            new EmployeePayrollAssignment { Id = "old", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 1, 1), IsApproved = true },
            new EmployeePayrollAssignment { Id = "new", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 9, 1), IsApproved = true },
            new EmployeePayrollAssignment { Id = "other", CompanyId = "company-2", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 10, 1), IsApproved = true }
        ]);

        Assert.Equal("new", result?.Id);
    }

    [Fact]
    public void ResolveAssignment_IgnoresUnapprovedAssignments()
    {
        var result = PayrollPolicyResolver.ResolveAssignment("company-1", "employee-1", new DateTime(2026, 9, 15),
        [new EmployeePayrollAssignment { Id = "pending", CompanyId = "company-1", EmployeeId = "employee-1", EffectiveFrom = new DateTime(2026, 9, 1), IsApproved = false }]);

        Assert.Null(result);
    }

    [Fact]
    public void ResolvePolicy_PrefersApprovedAssignedProfileAndDoesNotCrossTenant()
    {
        var assignment = new EmployeePayrollAssignment { CompanyId = "company-1", EmployeeId = "employee-1", PolicyProfileId = "assigned", EffectiveFrom = new DateTime(2026, 1, 1), IsApproved = true };
        var result = PayrollPolicyResolver.ResolvePolicy("company-1", new DateTime(2026, 9, 1), assignment,
        [
            new PayrollPolicyProfile { Id = "fallback", CompanyId = "company-1", Name = "Fallback", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED" },
            new PayrollPolicyProfile { Id = "assigned", CompanyId = "company-1", Name = "Assigned", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED" },
            new PayrollPolicyProfile { Id = "other", CompanyId = "company-2", Name = "Other", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED" }
        ]);

        Assert.Equal("assigned", result?.Id);
    }

    [Fact]
    public void ResolvePolicy_IgnoresUnapprovedStatutoryProfileAndUsesApprovedFallback()
    {
        var result = PayrollPolicyResolver.ResolvePolicy("company-1", new DateTime(2026, 9, 1), null,
        [
            new PayrollPolicyProfile { Id = "pending", CompanyId = "company-1", Name = "Pending", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "SUBMITTED", EpfWageCeiling = 30000m },
            new PayrollPolicyProfile { Id = "approved", CompanyId = "company-1", Name = "Approved", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED", EpfWageCeiling = 15000m }
        ]);

        Assert.Equal("approved", result?.Id);
        Assert.Equal(15000m, result?.EpfWageCeiling);
    }
    [Fact]
    public void ResolvePolicy_UsesPayGroupBeforeCompanyFallback()
    {
        var assignment = new EmployeePayrollAssignment { CompanyId = "company-1", EmployeeId = "employee-1", PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 1, 1), IsApproved = true };
        var result = PayrollPolicyResolver.ResolvePolicy("company-1", new DateTime(2026, 9, 1), assignment,
        [
            new PayrollPolicyProfile { Id = "fallback", CompanyId = "company-1", Name = "Fallback", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED" },
            new PayrollPolicyProfile { Id = "group-policy", CompanyId = "company-1", Name = "Group", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED" }
        ],
        [new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", PolicyProfileId = "group-policy", EffectiveFrom = new DateTime(2026, 1, 1) }]);

        Assert.Equal("group-policy", result?.Id);
    }

    [Fact]
    public void ResolvePolicy_AssignmentOnlyProfileDoesNotReplaceCompanyDefault()
    {
        var profiles = new[]
        {
            new PayrollPolicyProfile { Id = "company-default", CompanyId = "company-1", Name = "Company", Scope = "COMPANY_DEFAULT", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED" },
            new PayrollPolicyProfile { Id = "group-only", CompanyId = "company-1", Name = "Group", Scope = "ASSIGNMENT_ONLY", EffectiveFrom = new DateTime(2026, 9, 1), ApprovalStatus = "APPROVED" }
        };
        var date = new DateTime(2026, 9, 15);
        var fallback = PayrollPolicyResolver.ResolvePolicy("company-1", date, null, profiles);
        var assignment = new EmployeePayrollAssignment { CompanyId = "company-1", EmployeeId = "employee-1", PolicyProfileId = "group-only", EffectiveFrom = date, IsApproved = true };
        var assigned = PayrollPolicyResolver.ResolvePolicy("company-1", date, assignment, profiles);

        Assert.Equal("company-default", fallback?.Id);
        Assert.Equal("group-only", assigned?.Id);
    }

    [Fact]
    public void ResolvePolicy_DoesNotTreatMissingApprovalStatusAsApproval()
    {
        var result = PayrollPolicyResolver.ResolvePolicy("company-1", new DateTime(2026, 9, 15), null,
        [new PayrollPolicyProfile { Id = "legacy-unreviewed", CompanyId = "company-1", Name = "Unreviewed", EffectiveFrom = new DateTime(2026, 1, 1) }]);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveConsistentPeriod_ReportsFirstDayPolicyChangesForEmployeeSalaryPayGroup()
    {
        var salaries = new[]
        {
            new EmployeeSalaryStructure { Id = "salary-1", CompanyId = "company-1", EmployeeId = "employee-1", PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 9, 1), EffectiveTo = new DateTime(2026, 9, 14), Status = "ACTIVE", ApprovalStatus = "APPROVED" },
            new EmployeeSalaryStructure { Id = "salary-2", CompanyId = "company-1", EmployeeId = "employee-1", PayGroupId = "group-2", EffectiveFrom = new DateTime(2026, 9, 15), Status = "ACTIVE", ApprovalStatus = "APPROVED" }
        };
        var groups = new[]
        {
            new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Group 1", PolicyProfileId = "policy-1", EffectiveFrom = new DateTime(2026, 1, 1) },
            new PayrollPayGroup { Id = "group-2", CompanyId = "company-1", Name = "Group 2", PolicyProfileId = "policy-2", EffectiveFrom = new DateTime(2026, 1, 1) }
        };
        var profiles = new[]
        {
            new PayrollPolicyProfile { Id = "policy-1", CompanyId = "company-1", Name = "Policy 1", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED" },
            new PayrollPolicyProfile { Id = "policy-2", CompanyId = "company-1", Name = "Policy 2", EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED" }
        };

        var result = PayrollPolicyResolver.ResolveConsistentPeriod("company-1", "employee-1",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), [], salaries, profiles, groups);

        Assert.False(result.IsConsistent);
        Assert.Equal(new DateTime(2026, 9, 15), result.FirstConflictDate);
        Assert.Equal("policy-1", result.Profile?.Id);
        Assert.Equal("group-1", result.PayGroupId);
    }

    [Fact]
    public void ResolveConsistentPeriod_AllowsGroupChangeWhenResolvedPolicyVersionIsUnchanged()
    {
        var salaries = new[]
        {
            new EmployeeSalaryStructure { Id = "salary-1", CompanyId = "company-1", EmployeeId = "employee-1", PayGroupId = "group-1", EffectiveFrom = new DateTime(2026, 9, 1), EffectiveTo = new DateTime(2026, 9, 14), Status = "ACTIVE", ApprovalStatus = "APPROVED" },
            new EmployeeSalaryStructure { Id = "salary-2", CompanyId = "company-1", EmployeeId = "employee-1", PayGroupId = "group-2", EffectiveFrom = new DateTime(2026, 9, 15), Status = "ACTIVE", ApprovalStatus = "APPROVED" }
        };
        var groups = new[]
        {
            new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Group 1", PolicyProfileId = "policy-1", EffectiveFrom = new DateTime(2026, 1, 1) },
            new PayrollPayGroup { Id = "group-2", CompanyId = "company-1", Name = "Group 2", PolicyProfileId = "policy-1", EffectiveFrom = new DateTime(2026, 1, 1) }
        };
        var profiles = new[]
        {
            new PayrollPolicyProfile { Id = "policy-1", CompanyId = "company-1", Name = "Shared policy", Version = 3, EffectiveFrom = new DateTime(2026, 1, 1), ApprovalStatus = "APPROVED" }
        };

        var result = PayrollPolicyResolver.ResolveConsistentPeriod("company-1", "employee-1",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), [], salaries, profiles, groups);

        Assert.True(result.IsConsistent);
        Assert.Null(result.FirstConflictDate);
        Assert.Equal("policy-1", result.Profile?.Id);
    }
}
