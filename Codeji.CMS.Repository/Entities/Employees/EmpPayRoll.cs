using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public class EmpPayRoll
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public required string CompanyId { get; set; }
    public required string UserId { get; set; }
    public required string EmployeeId { get; set; }
    public required DateTime PayMonth { get; set; }
    public required DateTime PaidDate { get; set; }
    public required decimal BasicPay { get; set; }
    public decimal Bonus { get; set; }
    public required float PaidDays { get; set; }
    public Allowance Allowance { get; set; } = new Allowance();
    public Deduction Deduction { get; set; } = new Deduction();
    public DateTime CreatedAt { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedBy { get; set; }
    public List<PayrollDeductionLine> DeductionLines { get; set; } = [];
    public PayrollCalculationSnapshot? CalculationSnapshot { get; set; }
}

public class PayrollDeductionLine
{
    public required string Code { get; set; }
    public required string Description { get; set; }
    public decimal DayFraction { get; set; }
    public decimal Amount { get; set; }
    public string? SourceId { get; set; }
}

public class PayrollCalculationSnapshot
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
    public string CalculationEngineVersion { get; set; } = "2.0";
}

public class Allowance
{
    public decimal HRA { get; set; }
    public decimal LTA { get; set; }
    public decimal OtherAllowance { get; set; }
}

public class Deduction
{
    public float LossOfPayDays { get; set; }
    public decimal LossOfPay { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal HealthInsurance { get; set; }
    public decimal EPF { get; set; }
    public decimal ESIC { get; set; }
}
