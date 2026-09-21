using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Employees;

/// <summary>
/// Resolves the tenant's most specific active offboarding policy without making
/// authorization or tenant ownership decisions. Callers must provide policies
/// already loaded for the authenticated company.
/// </summary>
public sealed class OffboardingPolicyContext
{
    public string? DepartmentId { get; init; }
    public string? RoleId { get; init; }
    public string ExitType { get; init; } = string.Empty;
}

public sealed class ResolvedOffboardingPolicy
{
    public required EmployeeExitPolicy Policy { get; init; }
    public required string SourceScope { get; init; }
    public string? ExitType { get; init; }
    public bool Inherited => SourceScope.Equals("Company", StringComparison.OrdinalIgnoreCase);
}

public static class OffboardingPolicyResolver
{
    /// <summary>
    /// Precedence is deterministic: exact department, exact role, company.
    /// Invalid/duplicate scoped records are ignored deterministically by the
    /// lowest policy id so a query-order change cannot change behavior.
    /// </summary>
    public static ResolvedOffboardingPolicy? Resolve(
        IEnumerable<EmployeeExitPolicy> policies,
        OffboardingPolicyContext context)
    {
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentNullException.ThrowIfNull(context);

        var active = policies.Where(x => x.IsActive).ToList();
        var requestedExitType = EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(context.ExitType);

        return ResolveForExitType(active, context, requestedExitType, exactExitType: true)
            ?? ResolveForExitType(active, context, null, exactExitType: false);
    }

    private static ResolvedOffboardingPolicy? ResolveForExitType(
        IEnumerable<EmployeeExitPolicy> active,
        OffboardingPolicyContext context,
        string? exitType,
        bool exactExitType)
    {
        if (exactExitType && exitType is null) return null;
        var candidates = active.Where(policy => exactExitType
            ? string.Equals(EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(policy.ExitType), exitType, StringComparison.Ordinal)
            : string.IsNullOrWhiteSpace(policy.ExitType));

        var department = candidates
            .Where(x => !string.IsNullOrWhiteSpace(context.DepartmentId)
                && (x.DepartmentIds.Contains(context.DepartmentId!, StringComparer.Ordinal)
                    || string.Equals(x.DepartmentId, context.DepartmentId, StringComparison.Ordinal))
                && string.IsNullOrWhiteSpace(x.RoleId))
            .OrderBy(x => x.EmployeeExitPolicyId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (department is not null)
            return new() { Policy = department, SourceScope = $"Department:{context.DepartmentId}", ExitType = exitType };

        var role = candidates
            .Where(x => !string.IsNullOrWhiteSpace(context.RoleId)
                && string.Equals(x.RoleId, context.RoleId, StringComparison.Ordinal)
                && !x.DepartmentIds.Any() && string.IsNullOrWhiteSpace(x.DepartmentId))
            .OrderBy(x => x.EmployeeExitPolicyId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (role is not null)
            return new() { Policy = role, SourceScope = $"Role:{context.RoleId}", ExitType = exitType };

        var company = candidates
            .Where(x => x.DepartmentIds.Count == 0 && string.IsNullOrWhiteSpace(x.DepartmentId) && string.IsNullOrWhiteSpace(x.RoleId))
            .OrderBy(x => x.EmployeeExitPolicyId, StringComparer.Ordinal)
            .FirstOrDefault();
        return company is null ? null : new() { Policy = company, SourceScope = "Company", ExitType = exitType };
    }
}
