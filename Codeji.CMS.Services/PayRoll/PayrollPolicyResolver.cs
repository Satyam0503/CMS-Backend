using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public sealed record PayrollPeriodPolicyResolution(
    PayrollPolicyProfile? Profile,
    string? PayGroupId,
    bool IsConsistent,
    DateTime? FirstConflictDate);

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
        IEnumerable<PayrollPayGroup>? payGroups = null,
        string? payGroupOverride = null)
    {
        if (assignment?.PolicyProfileId is not null)
        {
            var assigned = profiles.FirstOrDefault(x => x.CompanyId == companyId && x.Id == assignment.PolicyProfileId && x.IsActive && IsApproved(x) && IsEffective(x, effectiveDate));
            if (assigned is not null) return assigned;
        }

        var effectivePayGroupId = assignment?.PayGroupId ?? payGroupOverride;
        if (effectivePayGroupId is not null && payGroups is not null)
        {
            var payGroup = payGroups.FirstOrDefault(x => x.CompanyId == companyId && x.Id == effectivePayGroupId && x.IsActive && x.EffectiveFrom.Date <= effectiveDate.Date && (x.EffectiveTo is null || x.EffectiveTo.Value.Date >= effectiveDate.Date));
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

    public static PayrollPeriodPolicyResolution ResolveConsistentPeriod(
        string companyId,
        string employeeId,
        DateTime periodStart,
        DateTime periodEnd,
        IEnumerable<EmployeePayrollAssignment> assignments,
        IEnumerable<EmployeeSalaryStructure> salaryStructures,
        IEnumerable<PayrollPolicyProfile> profiles,
        IEnumerable<PayrollPayGroup> payGroups,
        string? fallbackPayGroupId = null)
    {
        var firstDate = periodStart.Date;
        var lastDate = periodEnd.Date;
        if (lastDate < firstDate)
            throw new ArgumentOutOfRangeException(nameof(periodEnd), "Payroll period end must not precede its start.");

        PayrollPolicyProfile? firstProfile = null;
        string? firstPayGroupId = null;
        var firstDay = true;
        for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
        {
            var assignment = ResolveAssignment(companyId, employeeId, date, assignments);
            var salary = EmployeeSalaryStructureResolver.Resolve(companyId, employeeId, date, salaryStructures);
            var payGroupId = assignment?.PayGroupId ?? salary?.PayGroupId ?? fallbackPayGroupId;
            var profile = ResolvePolicy(companyId, date, assignment, profiles, payGroups, salary?.PayGroupId ?? fallbackPayGroupId);
            if (firstDay)
            {
                firstProfile = profile;
                firstPayGroupId = payGroupId;
                firstDay = false;
                continue;
            }

            if (!SamePolicy(firstProfile, profile))
                return new PayrollPeriodPolicyResolution(firstProfile, firstPayGroupId, false, date);
        }

        return new PayrollPeriodPolicyResolution(firstProfile, firstPayGroupId, true, null);
    }

    private static bool SamePolicy(PayrollPolicyProfile? left, PayrollPolicyProfile? right) =>
        left is null ? right is null : right is not null && left.Id == right.Id && left.Version == right.Version;

    private static bool IsApproved(PayrollPolicyProfile profile) => profile.ApprovalStatus == "APPROVED";

    private static bool IsEffective(PayrollPolicyProfile profile, DateTime date) =>
        profile.EffectiveFrom.Date <= date.Date && (profile.EffectiveTo is null || profile.EffectiveTo.Value.Date >= date.Date);
}
