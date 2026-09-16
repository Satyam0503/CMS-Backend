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
        var department = active
            .Where(x => !string.IsNullOrWhiteSpace(context.DepartmentId)
                && (x.DepartmentIds.Contains(context.DepartmentId!, StringComparer.Ordinal)
                    || string.Equals(x.DepartmentId, context.DepartmentId, StringComparison.Ordinal))
                && string.IsNullOrWhiteSpace(x.RoleId))
            .OrderBy(x => x.EmployeeExitPolicyId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (department is not null)
            return new() { Policy = department, SourceScope = $"Department:{context.DepartmentId}" };

        var role = active
            .Where(x => !string.IsNullOrWhiteSpace(context.RoleId)
                && string.Equals(x.RoleId, context.RoleId, StringComparison.Ordinal)
                && !x.DepartmentIds.Any() && string.IsNullOrWhiteSpace(x.DepartmentId))
            .OrderBy(x => x.EmployeeExitPolicyId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (role is not null)
            return new() { Policy = role, SourceScope = $"Role:{context.RoleId}" };

        var company = active
            .Where(x => x.DepartmentIds.Count == 0 && string.IsNullOrWhiteSpace(x.DepartmentId) && string.IsNullOrWhiteSpace(x.RoleId))
            .OrderBy(x => x.EmployeeExitPolicyId, StringComparer.Ordinal)
            .FirstOrDefault();
        return company is null ? null : new() { Policy = company, SourceScope = "Company" };
    }
}
