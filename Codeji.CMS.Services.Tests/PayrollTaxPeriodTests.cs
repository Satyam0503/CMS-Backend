using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollTaxPeriodTests
{
    [Theory]
    [InlineData(2026, 3, 31, "2025-26", "INCOME_TAX_ACT_1961", "192")]
    [InlineData(2026, 4, 1, "2026-27", "INCOME_TAX_ACT_2025", "392(1)")]
    [InlineData(2027, 3, 31, "2026-27", "INCOME_TAX_ACT_2025", "392(1)")]
    [InlineData(2027, 4, 1, "2027-28", "INCOME_TAX_ACT_2025", "392(1)")]
    public void ResolveIndiaSalaryPayment_UsesPaymentDateAndTaxLawTransition(
        int year,
        int month,
        int day,
        string expectedFinancialYear,
        string expectedAct,
        string expectedSection)
    {
        var period = PayrollTaxPeriod.ResolveIndiaSalaryPayment(new DateTime(year, month, day));

        Assert.Equal(expectedFinancialYear, period.FinancialYear);
        Assert.Equal(expectedAct, period.IncomeTaxAct);
        Assert.Equal(expectedSection, period.SalaryTdsSection);
        Assert.True(period.StartDate <= new DateTime(year, month, day));
        Assert.True(period.EndDateExclusive > new DateTime(year, month, day));
    }
}
