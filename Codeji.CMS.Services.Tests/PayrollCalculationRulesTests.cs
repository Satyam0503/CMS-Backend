public class PayrollCalculationRulesTests
{
    [Theory]
    [InlineData("CALENDAR_DAYS",31,22,31)] [InlineData("FIXED_30_DAYS",31,22,30)] [InlineData("WORKING_DAYS",31,22,22)]
    public void Resolves_configured_divisor(string policy,int calendar,decimal working,decimal expected)=>Assert.Equal(expected,PayrollCalculationRules.ResolveDivisor(policy,calendar,working));

    [Theory]
    [InlineData(0,0.5,0.5)] [InlineData(0.5,0.5,1)] [InlineData(1.5,0,1.5)]
    public void Approved_policy_penalty_is_added_once(decimal attendanceUnpaid,decimal penalty,decimal expected)=>Assert.Equal(expected,PayrollCalculationRules.TotalLossOfPayDays(attendanceUnpaid,penalty));
}
