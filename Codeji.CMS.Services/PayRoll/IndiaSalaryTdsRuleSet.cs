namespace Codeji.CMS.Services.PayRoll;

public sealed record IndiaIncomeTaxSlab(decimal LowerBound, decimal? UpperBound, decimal Rate);
public sealed record IndiaIncomeTaxSurcharge(decimal IncomeThreshold, decimal Rate);

/// <summary>
/// Application-owned, immutable Indian salary-TDS rule bundle. Company policy can select
/// regime and jurisdiction; it cannot change statutory limits or rates through freeform fields.
/// </summary>
public sealed record IndiaSalaryTdsRuleSet(
    string Id,
    string FinancialYear,
    string IncomeTaxAct,
    string SalaryTdsSection,
    string CalculationEngineVersion,
    decimal OldRegimeStandardDeduction,
    decimal NewRegimeStandardDeduction,
    decimal OldRegimeRebateThreshold,
    decimal NewRegimeRebateThreshold,
    decimal Section80CCap,
    decimal Section80Ccd1BCap,
    decimal Section80DCap,
    decimal Section24LossCap,
    decimal CessRate,
    decimal NewRegimeSurchargeMaximum,
    IReadOnlyList<IndiaIncomeTaxSlab> OldRegimeSlabs,
    IReadOnlyList<IndiaIncomeTaxSlab> NewRegimeSlabs,
    IReadOnlyList<IndiaIncomeTaxSurcharge> OldRegimeSurcharges,
    IReadOnlyList<IndiaIncomeTaxSurcharge> NewRegimeSurcharges)
{
    private static readonly IReadOnlyList<IndiaIncomeTaxSlab> OldSlabs = Array.AsReadOnly(new[]
    {
        new IndiaIncomeTaxSlab(0m, 250000m, 0m),
        new IndiaIncomeTaxSlab(250000m, 500000m, 0.05m),
        new IndiaIncomeTaxSlab(500000m, 1000000m, 0.20m),
        new IndiaIncomeTaxSlab(1000000m, null, 0.30m)
    });

    private static readonly IReadOnlyList<IndiaIncomeTaxSlab> NewSlabs = Array.AsReadOnly(new[]
    {
        new IndiaIncomeTaxSlab(0m, 400000m, 0m),
        new IndiaIncomeTaxSlab(400000m, 800000m, 0.05m),
        new IndiaIncomeTaxSlab(800000m, 1200000m, 0.10m),
        new IndiaIncomeTaxSlab(1200000m, 1600000m, 0.15m),
        new IndiaIncomeTaxSlab(1600000m, 2000000m, 0.20m),
        new IndiaIncomeTaxSlab(2000000m, 2400000m, 0.25m),
        new IndiaIncomeTaxSlab(2400000m, null, 0.30m)
    });

    private static readonly IReadOnlyList<IndiaIncomeTaxSurcharge> OldSurcharges = Array.AsReadOnly(new[]
    {
        new IndiaIncomeTaxSurcharge(5000000m, 0.10m),
        new IndiaIncomeTaxSurcharge(10000000m, 0.15m),
        new IndiaIncomeTaxSurcharge(20000000m, 0.25m),
        new IndiaIncomeTaxSurcharge(50000000m, 0.37m)
    });

    private static readonly IReadOnlyList<IndiaIncomeTaxSurcharge> NewSurcharges = Array.AsReadOnly(new[]
    {
        new IndiaIncomeTaxSurcharge(5000000m, 0.10m),
        new IndiaIncomeTaxSurcharge(10000000m, 0.15m),
        new IndiaIncomeTaxSurcharge(20000000m, 0.25m)
    });

    private static readonly IReadOnlyDictionary<string, IndiaSalaryTdsRuleSet> Catalog =
        new Dictionary<string, IndiaSalaryTdsRuleSet>(StringComparer.Ordinal)
        {
            ["2025-26"] = Create("IN-SALARY-TDS-FY2025-26-FA2025-v1", "2025-26", "INCOME_TAX_ACT_1961", "192", "INDIA-TDS-2025.1"),
            ["2026-27"] = Create("IN-SALARY-TDS-TY2026-27-FA2026-v1", "2026-27", "INCOME_TAX_ACT_2025", "392(1)", "INDIA-TDS-2026.2")
        };

    public static bool TryResolve(string financialYear, out IndiaSalaryTdsRuleSet? ruleSet) =>
        Catalog.TryGetValue(financialYear, out ruleSet);

    public static IndiaSalaryTdsRuleSet ResolveOrDefault(string? financialYear = null) =>
        !string.IsNullOrWhiteSpace(financialYear) && Catalog.TryGetValue(financialYear, out var rules)
            ? rules
            : Catalog["2025-26"];

    private static IndiaSalaryTdsRuleSet Create(
        string id, string financialYear, string act, string section, string engineVersion) =>
        new(id, financialYear, act, section, engineVersion,
            OldRegimeStandardDeduction: 50000m,
            NewRegimeStandardDeduction: 75000m,
            OldRegimeRebateThreshold: 500000m,
            NewRegimeRebateThreshold: 1200000m,
            Section80CCap: 150000m,
            Section80Ccd1BCap: 50000m,
            Section80DCap: 100000m,
            Section24LossCap: 200000m,
            CessRate: 0.04m,
            NewRegimeSurchargeMaximum: 0.25m,
            OldRegimeSlabs: OldSlabs,
            NewRegimeSlabs: NewSlabs,
            OldRegimeSurcharges: OldSurcharges,
            NewRegimeSurcharges: NewSurcharges);
}
