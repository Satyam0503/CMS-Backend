namespace Codeji.CMS.Services.PayRoll;

/// <summary>
/// Resolves Indian salary-TDS year and governing Act from the date salary is paid.
/// Payroll month is an accrual period and does not determine the salary-TDS year.
/// </summary>
public sealed record PayrollTaxPeriod(
    string FinancialYear,
    DateTime StartDate,
    DateTime EndDateExclusive,
    string IncomeTaxAct,
    string SalaryTdsSection)
{
    /// <summary>
    /// Immutable ruleset identity for the tax year. These identifiers are application-owned;
    /// company profiles hold review metadata and policy choices, not replacement tax law.
    /// </summary>
    public IndiaSalaryTdsRuleSet? TaxRuleSet => IndiaSalaryTdsRuleSet.TryResolve(FinancialYear, out var rules) ? rules : null;
    public string TaxRuleSetId => TaxRuleSet?.Id ?? "UNSUPPORTED";
    public string CalculationEngineVersion => TaxRuleSet?.CalculationEngineVersion ?? "UNSUPPORTED";
    public bool HasSupportedTaxRules => TaxRuleSet is not null;

    public static PayrollTaxPeriod ResolveIndiaSalaryPayment(DateTime paymentDate)
    {
        var date = paymentDate.Date;
        var startYear = date.Month >= 4 ? date.Year : date.Year - 1;
        var start = new DateTime(startYear, 4, 1);
        var end = start.AddYears(1);
        var financialYear = $"{startYear}-{(startYear + 1) % 100:00}";
        var uses2025Act = start >= new DateTime(2026, 4, 1);

        return new PayrollTaxPeriod(
            financialYear,
            start,
            end,
            uses2025Act ? "INCOME_TAX_ACT_2025" : "INCOME_TAX_ACT_1961",
            uses2025Act ? "392(1)" : "192");
    }

    public static PayrollTaxPeriod? TryResolveFinancialYear(string? financialYear)
    {
        if (string.IsNullOrWhiteSpace(financialYear)) return null;
        var parts = financialYear.Trim().Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var startYear) || startYear < 1900 || startYear > 9998)
            return null;
        if (!string.Equals(parts[1], $"{(startYear + 1) % 100:00}", StringComparison.Ordinal)) return null;
        return ResolveIndiaSalaryPayment(new DateTime(startYear, 4, 1));
    }
}
