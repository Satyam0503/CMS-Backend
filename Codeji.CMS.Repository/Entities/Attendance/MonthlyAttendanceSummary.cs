using Codeji.CMS.Repository.Entities;
using MongoDB.Bson.Serialization.Attributes;

public class MonthlyAttendanceSummary : BaseClass
{
    [BsonId] public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string UserId { get; set; }
    public required string EmployeeId { get; set; }
    public required DateTime PayrollMonth { get; set; }
    public DateTime EligibleFrom { get; set; }
    public DateTime EligibleTo { get; set; }
    public int ExpectedWorkingDays { get; set; }
    public int EligibleWorkingDays { get; set; }
    public decimal PresentDays { get; set; }
    public decimal WfhDays { get; set; }
    public decimal PaidLeaveDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public decimal HalfDays { get; set; }
    public decimal AbsentDays { get; set; }
    public int MissingAttendanceDays { get; set; }
    public int MissingCheckoutDays { get; set; }
    public decimal PaidDays { get; set; }
    public decimal UnpaidDays { get; set; }
    public int LhdCount { get; set; }
    public int EdCount { get; set; }
    public int CombinedLhdEdCount { get; set; }
    public int OvertimeMinutes { get; set; }
    public int BlockingExceptionCount { get; set; }
    public bool IsApproved { get; set; }
    public bool IsLocked { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }
    public int Version { get; set; } = 1;
}
