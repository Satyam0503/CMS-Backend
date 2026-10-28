using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.PayRoll;

public sealed class PayrollComponentCalculationOutcome
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyDictionary<string, decimal> Values { get; init; } = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    public decimal Gross { get; init; }
    public decimal Deductions { get; init; }
    public decimal Net { get; init; }
    public decimal EmployerContributions { get; init; }
    public decimal TotalCostToCompany { get; init; }

    public static PayrollComponentCalculationOutcome Invalid(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}

public static class PayrollComponentCalculationEngine
{
    private static readonly string[] SupportedKinds = ["EARNING", "DEDUCTION"];
    private static readonly string[] SupportedCalculationTypes = ["FIXED", "PERCENT_OF_GROSS", "PERCENT_OF_COMPONENT"];

    public static PayrollComponentCalculationOutcome Calculate(IEnumerable<PayrollComponentSnapshot> source)
    {
        var components = source?.ToList() ?? [];
        if (components.Count == 0)
            return PayrollComponentCalculationOutcome.Invalid("PAYROLL_COMPONENTS_EMPTY", "No payroll components were configured for this employee.");

        var byCode = new Dictionary<string, PayrollComponentSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in components)
        {
            var code = component.Code?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
                return PayrollComponentCalculationOutcome.Invalid("PAYROLL_COMPONENT_CODE_INVALID", "Every payroll component must have a code.");
            if (!byCode.TryAdd(code, component))
                return PayrollComponentCalculationOutcome.Invalid("PAYROLL_COMPONENT_DUPLICATE", $"Payroll component code '{code}' is duplicated.");
            if (!SupportedKinds.Contains(component.Kind?.Trim().ToUpperInvariant()))
                return PayrollComponentCalculationOutcome.Invalid("PAYROLL_COMPONENT_KIND_INVALID", $"Payroll component '{code}' has an unsupported kind.");
            if (!SupportedCalculationTypes.Contains(component.CalculationType?.Trim().ToUpperInvariant()))
                return PayrollComponentCalculationOutcome.Invalid("PAYROLL_COMPONENT_CALCULATION_INVALID", $"Payroll component '{code}' has an unsupported calculation type.");
            if (component.Value < 0)
                return PayrollComponentCalculationOutcome.Invalid("PAYROLL_COMPONENT_VALUE_INVALID", $"Payroll component '{code}' cannot have a negative value.");
        }

        foreach (var component in byCode.Values)
        {
            if (!string.Equals(component.CalculationType, "PERCENT_OF_COMPONENT", StringComparison.OrdinalIgnoreCase)) continue;
            var baseCode = component.BaseComponentCode?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(baseCode))
                return PayrollComponentCalculationOutcome.Invalid("PAYROLL_COMPONENT_BASE_REQUIRED", $"Payroll component '{component.Code}' requires a base component.");
            if (!byCode.ContainsKey(baseCode))
                return PayrollComponentCalculationOutcome.Invalid("PAYROLL_COMPONENT_BASE_MISSING", $"Payroll component '{component.Code}' references missing component '{baseCode}'.");
        }

        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in byCode.Values.OrderBy(x => x.SortOrder).ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
        {
            var error = Evaluate(component.Code, byCode, state, values);
            if (error is not null)
                return error;
        }

        var gross = PayrollCalculationRules.RoundMoney(byCode.Values
            .Where(x => string.Equals(x.Kind, "EARNING", StringComparison.OrdinalIgnoreCase) &&
                        x.IsRecurring && !x.IsStatutory && !x.IsEmployerContribution)
            .Sum(x => values[x.Code.Trim()]));
        var deductions = PayrollCalculationRules.RoundMoney(byCode.Values
            .Where(x => string.Equals(x.Kind, "DEDUCTION", StringComparison.OrdinalIgnoreCase) &&
                        x.IsRecurring && !x.IsStatutory && !x.IsEmployerContribution)
            .Sum(x => values[x.Code.Trim()]));
        var employerContributions = PayrollCalculationRules.RoundMoney(byCode.Values
            .Where(x => x.IsEmployerContribution && x.IsRecurring)
            .Sum(x => values[x.Code.Trim()]));
        var totalCtc = PayrollCalculationRules.RoundMoney(gross + employerContributions);

        if (gross < deductions)
            return PayrollComponentCalculationOutcome.Invalid("PAYROLL_NET_NEGATIVE", "Recurring employee deductions exceed recurring gross salary. Review the salary structure before payroll can proceed.");
        return new PayrollComponentCalculationOutcome
        {
            Success = true,
            Values = values,
            Gross = Math.Max(0m, gross),
            Deductions = Math.Max(0m, deductions),
            Net = PayrollCalculationRules.RoundMoney(gross - deductions),
            EmployerContributions = Math.Max(0m, employerContributions),
            TotalCostToCompany = Math.Max(0m, totalCtc)
        };
    }

    private static PayrollComponentCalculationOutcome? Evaluate(
        string rawCode,
        IReadOnlyDictionary<string, PayrollComponentSnapshot> components,
        IDictionary<string, int> state,
        IDictionary<string, decimal> values)
    {
        var code = rawCode.Trim().ToUpperInvariant();
        if (values.ContainsKey(code)) return null;
        if (state.TryGetValue(code, out var current))
        {
            if (current == 1)
                return PayrollComponentCalculationOutcome.Invalid("PAYROLL_COMPONENT_CYCLE", $"Payroll component formula cycle detected at '{code}'.");
            if (current == 2) return null;
        }

        state[code] = 1;
        var component = components[code];
        var type = component.CalculationType.Trim().ToUpperInvariant();
        decimal amount;
        switch (type)
        {
            case "FIXED":
                amount = component.Value;
                break;
            case "PERCENT_OF_COMPONENT":
            {
                var baseCode = component.BaseComponentCode!.Trim().ToUpperInvariant();
                var dependencyError = Evaluate(baseCode, components, state, values);
                if (dependencyError is not null) return dependencyError;
                amount = values[baseCode] * component.Value / 100m;
                break;
            }
            case "PERCENT_OF_GROSS":
            {
                // Gross percentage components use the deterministic subtotal of earnings
                // that do not themselves depend on gross. This prevents circular formulas.
                decimal grossBase = 0;
                foreach (var baseComponent in components.Values
                    .Where(x => string.Equals(x.Kind, "EARNING", StringComparison.OrdinalIgnoreCase) &&
                                x.IsRecurring &&
                                !x.IsStatutory && !x.IsEmployerContribution &&
                                !string.Equals(x.CalculationType, "PERCENT_OF_GROSS", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
                {
                    var dependencyError = Evaluate(baseComponent.Code, components, state, values);
                    if (dependencyError is not null) return dependencyError;
                    grossBase += values[baseComponent.Code.Trim()];
                }
                amount = grossBase * component.Value / 100m;
                break;
            }
            default:
                return PayrollComponentCalculationOutcome.Invalid("PAYROLL_COMPONENT_CALCULATION_INVALID", $"Payroll component '{code}' has an unsupported calculation type.");
        }

        values[code] = PayrollCalculationRules.RoundMoney(Math.Max(0m, amount));
        state[code] = 2;
        return null;
    }
}
