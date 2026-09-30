using MongoDB.Bson.Serialization.Attributes;
namespace Codeji.CMS.Repository.Entities.Employees;
[BsonIgnoreExtraElements]
public sealed class PayrollPaymentBatch
{
 [BsonId(IdGenerator=typeof(UniqueIdGenerator))] public string Id {get;set;}=string.Empty;
 public required string CompanyId {get;set;} public required string PayrollRunId {get;set;} public string Status {get;set;}="DRAFT";
 public string? ProviderReference {get;set;} public int EmployeeCount {get;set;} public decimal TotalNetAmount {get;set;}
 public string? CreatedBy {get;set;} public DateTime CreatedAtUtc {get;set;} public string? SubmittedBy {get;set;} public DateTime? SubmittedAtUtc {get;set;} public string? ReconciledBy {get;set;} public DateTime? ReconciledAtUtc {get;set;} public string? ReconciliationNote {get;set;}
 public List<PayrollPaymentBatchItem> Items {get;set;}=[];
}
public sealed class PayrollPaymentBatchItem { public required string EmployeeId {get;set;} public decimal NetAmount {get;set;} public string Status {get;set;}="PENDING"; public string? ProviderReference {get;set;} }