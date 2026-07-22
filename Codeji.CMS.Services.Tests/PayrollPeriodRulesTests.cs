public class PayrollPeriodRulesTests
{
    [Theory]
    [InlineData("2025-12-01", "2026-01-15", true)]
    [InlineData("2026-01-01", "2026-01-15", false)]
    [InlineData("2027-01-01", "2026-07-15", false)]
    [InlineData("2026-12-01", "2027-01-01", true)]
    public void Closed_period_comparison_handles_year_boundaries(string requested, string now, bool expected) =>
        Assert.Equal(expected, PayrollPeriodRules.IsClosedPeriod(DateTime.Parse(requested), DateTime.Parse(now)));
}
