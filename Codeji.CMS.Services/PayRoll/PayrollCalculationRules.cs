public static class PayrollCalculationRules
{
    public static decimal ResolveDivisor(string policy,int calendarDays,decimal eligibleWorkingDays)=>policy switch
    {
        "FIXED_30_DAYS"=>30m,
        "WORKING_DAYS"=>eligibleWorkingDays,
        _=>calendarDays
    };

    // Business decision: configured status unpaid time remains the attendance treatment;
    // an approved exceeded-limit penalty is additional and is supplied exactly once by
    // the single idempotent monthly source exception.
    public static decimal TotalLossOfPayDays(decimal attendanceUnpaidDays,decimal approvedPolicyPenaltyDays)=>
        attendanceUnpaidDays+approvedPolicyPenaltyDays;
}
