public static class PayrollPeriodRules
{
    public static DateTime Normalize(DateTime value) => new(value.Year, value.Month, 1);
    public static bool IsClosedPeriod(DateTime requested, DateTime utcNow) => Normalize(requested) < Normalize(utcNow);
}
