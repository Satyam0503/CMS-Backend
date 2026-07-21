namespace Codeji.CMS.DTO.PayRoll;

public class AddUpdatePayRollRequestDto
{
    public string? PayRollId { get; set; }
    public required string EmployeeId { get; set; }
    public required decimal BasicPay { get; set; }
    public required float PaidDays { get; set; }
    public required DateTime PaidDate { get; set; }
    public decimal? Bonus { get; set; }
    public decimal? HRA { get; set; }
    public decimal? LTA { get; set; }
    public decimal? OtherAllowance { get; set; }
    public decimal? LossOfPayDays { get; set; }

    public decimal? LossOfPay { get; set; }
    public decimal? IncomeTax { get; set; }
    public decimal? HealthInsurance { get; set; }
    public decimal? EPF { get; set; }
    public decimal? ESIC { get; set; }
    public required DateTime PayMonth { get; set; }
    public List<PayrollDeductionLineDto> DeductionLines { get; set; } = [];
    public PayrollCalculationSnapshotDto? CalculationSnapshot { get; set; }
}

public class PayrollDeductionLineDto
{
    public required string Code { get; set; }
    public required string Description { get; set; }
    public decimal DayFraction { get; set; }
    public decimal Amount { get; set; }
    public string? SourceId { get; set; }
}

public class PayrollCalculationSnapshotDto
{
    public DateTime PayrollMonth { get; set; }
    public DateTime JoiningDateUsed { get; set; }
    public DateTime? ExitDateUsed { get; set; }
    public DateTime EligibleFrom { get; set; }
    public DateTime EligibleTo { get; set; }
    public string DivisorPolicy { get; set; } = "CALENDAR_DAYS";
    public decimal Divisor { get; set; }
    public int DivisorPolicyVersion { get; set; }
    public int LhdCount { get; set; }
    public int EdCount { get; set; }
    public int CombinedCount { get; set; }
    public int AllowedCount { get; set; }
    public int ExceededCount { get; set; }
    public string? ExceptionDecision { get; set; }
    public decimal PenaltyDayFraction { get; set; }
    public decimal PenaltyAmount { get; set; }
    public string? PolicyId { get; set; }
    public int PolicyVersion { get; set; }
    public int AttendanceSummaryVersion { get; set; }
}
