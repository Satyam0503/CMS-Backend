using Codeji.CMS.Services.PayRoll;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollTaxPeriodTests
{
    [Theory]
    [InlineData(2026, 3, 31, "2025-26", "INCOME_TAX_ACT_1961", "192", "IN-SALARY-TDS-FY2025-26-FA2025-v1", "INDIA-TDS-2025.1", true)]
    [InlineData(2026, 4, 1, "2026-27", "INCOME_TAX_ACT_2025", "392(1)", "IN-SALARY-TDS-TY2026-27-FA2026-v1", "INDIA-TDS-2026.2", true)]
    [InlineData(2027, 3, 31, "2026-27", "INCOME_TAX_ACT_2025", "392(1)", "IN-SALARY-TDS-TY2026-27-FA2026-v1", "INDIA-TDS-2026.2", true)]
    [InlineData(2027, 4, 1, "2027-28", "INCOME_TAX_ACT_2025", "392(1)", "UNSUPPORTED", "UNSUPPORTED", false)]
    [InlineData(2025, 3, 31, "2024-25", "INCOME_TAX_ACT_1961", "192", "UNSUPPORTED", "UNSUPPORTED", false)]
    public void ResolveIndiaSalaryPayment_UsesPaymentDateAndTaxLawTransition(
        int year,
        int month,
        int day,
        string expectedFinancialYear,
        string expectedAct,
        string expectedSection,
        string expectedRuleSetId,
        string expectedEngineVersion,
        bool expectedSupported)
    {
        var period = PayrollTaxPeriod.ResolveIndiaSalaryPayment(new DateTime(year, month, day));

        Assert.Equal(expectedFinancialYear, period.FinancialYear);
        Assert.Equal(expectedAct, period.IncomeTaxAct);
        Assert.Equal(expectedSection, period.SalaryTdsSection);
        Assert.Equal(expectedRuleSetId, period.TaxRuleSetId);
        Assert.Equal(expectedEngineVersion, period.CalculationEngineVersion);
        Assert.Equal(expectedSupported, period.HasSupportedTaxRules);
        Assert.True(period.StartDate <= new DateTime(year, month, day));
        Assert.True(period.EndDateExclusive > new DateTime(year, month, day));
    }

    [Theory]
    [InlineData("2025-26", true)]
    [InlineData("2026-27", true)]
    [InlineData("2024-25", false)]
    [InlineData("2027-28", false)]
    [InlineData("2025-29", false)]
    [InlineData("bad", false)]
    public void TryResolveFinancialYear_RejectsMalformedAndUncataloguedYears(string financialYear, bool expectedSupported)
    {
        var period = PayrollTaxPeriod.TryResolveFinancialYear(financialYear);

        Assert.Equal(expectedSupported, period?.HasSupportedTaxRules ?? false);
    }
}
