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
        var code = componentCode.Trim();
        var isSpecialOrOther = string.Equals(code, "SPECIAL_ALLOWANCE", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(code, "OTHER_ALLOWANCE", StringComparison.OrdinalIgnoreCase);

        var list = definitions
            .Where(definition => definition.CompanyId == companyId &&
                (string.Equals(definition.Code.Trim(), code, StringComparison.OrdinalIgnoreCase) ||
                 (isSpecialOrOther && (string.Equals(definition.Code.Trim(), "SPECIAL_ALLOWANCE", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(definition.Code.Trim(), "OTHER_ALLOWANCE", StringComparison.OrdinalIgnoreCase)))) &&
                definition.EffectiveFrom.Date <= date)
            .OrderByDescending(definition => string.Equals(definition.Code.Trim(), code, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenByDescending(definition => definition.EffectiveFrom.Date)
            .ThenByDescending(definition => definition.Version)
            .FirstOrDefault();

        return list is not null && list.IsActive &&
            (!list.EffectiveTo.HasValue || list.EffectiveTo.Value.Date >= date)
            ? list
            : null;
    }
}
