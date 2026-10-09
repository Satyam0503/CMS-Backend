using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

/// <summary>
/// Resolves the newest component-definition revision that has begun by a
/// payroll date. An inactive or expired newest revision deliberately does not
/// fall back to an older active revision.
/// </summary>
public static class PayrollComponentDefinitionResolver
{
    public static PayrollSalaryComponentDefinition? Resolve(
        IEnumerable<PayrollSalaryComponentDefinition> definitions,
        string companyId,
        string componentCode,
        DateTime effectiveDate)
    {
        var date = effectiveDate.Date;
        var latest = definitions
            .Where(definition => definition.CompanyId == companyId &&
                string.Equals(definition.Code.Trim(), componentCode.Trim(), StringComparison.OrdinalIgnoreCase) &&
                definition.EffectiveFrom.Date <= date)
            .OrderByDescending(definition => definition.EffectiveFrom.Date)
            .ThenByDescending(definition => definition.Version)
            .FirstOrDefault();

        return latest is not null && latest.IsActive &&
            (!latest.EffectiveTo.HasValue || latest.EffectiveTo.Value.Date >= date)
            ? latest
            : null;
    }
}
