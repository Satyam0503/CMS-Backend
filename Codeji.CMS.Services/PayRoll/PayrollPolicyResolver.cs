using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public static class PayrollPolicyResolver
{
    public static EmployeePayrollAssignment? ResolveAssignment(
        string companyId,
        string employeeId,
        DateTime effectiveDate,
        IEnumerable<EmployeePayrollAssignment> assignments)
    {
        return assignments
            .Where(x => x.CompanyId == companyId && x.EmployeeId == employeeId && x.IsApproved)
            .Where(x => x.EffectiveFrom.Date <= effectiveDate.Date && (x.EffectiveTo is null || x.EffectiveTo.Value.Date >= effectiveDate.Date))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.ApprovedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(x => x.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    public static PayrollPolicyProfile? ResolvePolicy(
        string companyId,
        DateTime effectiveDate,
        EmployeePayrollAssignment? assignment,
        IEnumerable<PayrollPolicyProfile> profiles,
        IEnumerable<PayrollPayGroup>? payGroups = null)
    {
        if (assignment?.PolicyProfileId is not null)
        {
            var assigned = profiles.FirstOrDefault(x => x.CompanyId == companyId && x.Id == assignment.PolicyProfileId && x.IsActive && IsApproved(x) && IsEffective(x, effectiveDate));
            if (assigned is not null) return assigned;
        }

        if (assignment?.PayGroupId is not null && payGroups is not null)
        {
            var payGroup = payGroups.FirstOrDefault(x => x.CompanyId == companyId && x.Id == assignment.PayGroupId && x.IsActive && x.EffectiveFrom.Date <= effectiveDate.Date && (x.EffectiveTo is null || x.EffectiveTo.Value.Date >= effectiveDate.Date));
            if (payGroup?.PolicyProfileId is not null)
            {
                var payGroupPolicy = profiles.FirstOrDefault(x => x.CompanyId == companyId && x.Id == payGroup.PolicyProfileId && x.IsActive && IsApproved(x) && IsEffective(x, effectiveDate));
                if (payGroupPolicy is not null) return payGroupPolicy;
            }
        }

        return profiles
            .Where(x => x.CompanyId == companyId && x.IsActive && x.Scope != "ASSIGNMENT_ONLY" && IsApproved(x) && IsEffective(x, effectiveDate))
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.ApprovedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(x => x.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static bool IsApproved(PayrollPolicyProfile profile) => profile.ApprovalStatus == "APPROVED";

    private static bool IsEffective(PayrollPolicyProfile profile, DateTime date) =>
        profile.EffectiveFrom.Date <= date.Date && (profile.EffectiveTo is null || profile.EffectiveTo.Value.Date >= date.Date);
}
