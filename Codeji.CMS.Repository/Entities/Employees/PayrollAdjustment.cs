using MongoDB.Bson.Serialization.Attributes;
namespace Codeji.CMS.Repository.Entities.Employees;
[BsonIgnoreExtraElements]
public sealed class PayrollAdjustment
{
 [BsonId(IdGenerator=typeof(UniqueIdGenerator))] public string Id {get;set;}=string.Empty;
 public required string CompanyId {get;set;} public required string OriginalPayrollRunId {get;set;} public string? CorrectionPayrollRunId {get;set;} public required string EmployeeId {get;set;}
 public string Type {get;set;}="CORRECTION"; public string Status {get;set;}="DRAFT"; public decimal Amount {get;set;} public required string Reason {get;set;}
 public string? ComponentCode { get; set; } public string? ComponentName { get; set; } public string? ComponentKind { get; set; }
 public bool? IsTaxable { get; set; } public int? ComponentVersion { get; set; } public DateTime? TargetPeriod { get; set; }
 public string? RejectedBy { get; set; } public DateTime? RejectedAtUtc { get; set; } public string? RejectionReason { get; set; }
 public string? CreatedBy {get;set;} public DateTime CreatedAtUtc {get;set;} public string? ApprovedBy {get;set;} public DateTime? ApprovedAtUtc {get;set;} public string? ProcessedBy {get;set;} public DateTime? ProcessedAtUtc {get;set;}
 public List<PayrollAdjustmentAuditEvent> AuditHistory {get;set;}=[];
}
public sealed class PayrollAdjustmentAuditEvent { public required string Action {get;set;} public required string ActorId {get;set;} public DateTime OccurredAtUtc {get;set;} public string? Notes {get;set;} }
