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
}
