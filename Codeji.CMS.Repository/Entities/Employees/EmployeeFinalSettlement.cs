using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public sealed class EmployeeFinalSettlement : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EmployeeFinalSettlementId { get; set; } = string.Empty;
    public string EmployeeExitId { get; set; } = string.Empty;
    public string EmployeeUserId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int Version { get; set; } = 1;
    public string Direction { get; set; } = "ZeroSettlement"; // PayableToEmployee, ReceivableFromEmployee, ZeroSettlement
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? PaymentInitiatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public string? PaymentFailureReason { get; set; }
    public string? DisputeReason { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedByUserId { get; set; }
}
