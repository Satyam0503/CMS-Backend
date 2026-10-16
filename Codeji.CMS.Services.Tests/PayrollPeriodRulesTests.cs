public class PayrollPeriodRulesTests
{
    [Theory]
    [InlineData("2025-12-01", "2026-01-15", true)]
    [InlineData("2026-01-01", "2026-01-15", false)]
    [InlineData("2027-01-01", "2026-07-15", false)]
    [InlineData("2026-12-01", "2027-01-01", true)]
    public void Closed_period_comparison_handles_year_boundaries(string requested, string now, bool expected) =>
        Assert.Equal(expected, PayrollPeriodRules.IsClosedPeriod(DateTime.Parse(requested), DateTime.Parse(now)));

    [Theory]
    [InlineData("2026-02-01", "2026-01-15", true)]
    [InlineData("2026-01-01", "2026-01-15", false)]
    [InlineData("2025-12-01", "2026-01-15", false)]
    public void Future_period_comparison_identifies_future_months(string requested, string now, bool expected) =>
        Assert.Equal(expected, PayrollPeriodRules.IsFuturePeriod(DateTime.Parse(requested), DateTime.Parse(now)));

    [Theory]
    [InlineData("2025-12-01", "2026-01-15", true)]
    [InlineData("2026-01-01", "2026-01-15", true)]
    [InlineData("2026-02-01", "2026-01-15", false)]
    public void Can_process_payroll_permits_completed_and_active_months(string requested, string now, bool expected) =>
        Assert.Equal(expected, PayrollPeriodRules.CanProcessPayroll(DateTime.Parse(requested), DateTime.Parse(now)));
}
