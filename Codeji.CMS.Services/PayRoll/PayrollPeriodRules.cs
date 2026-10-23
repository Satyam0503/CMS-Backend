public static class PayrollPeriodRules
{
    public static DateTime Normalize(DateTime value) => new(value.Year, value.Month, 1);
    public static bool IsClosedPeriod(DateTime requested, DateTime utcNow) => Normalize(requested) < Normalize(utcNow);
    public static bool IsFuturePeriod(DateTime requested, DateTime utcNow) => Normalize(requested) > Normalize(utcNow);
    public static bool CanProcessPayroll(DateTime requested, DateTime utcNow) => !IsFuturePeriod(requested, utcNow);
}
