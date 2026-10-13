using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;
using System.Text.Json.Serialization;

namespace Codeji.CMS.Repository.Entities.Employees;

/// <summary>
/// Tenant-scoped operational case for a non-routine employee availability period.
/// LeaveRequest remains the source of entitlement; this record owns lifecycle,
/// review, return-to-work, and evidence tracking.
/// </summary>
public sealed class EmployeeLifecycleCase : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string CaseId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string EmployeeUserId { get; set; } = string.Empty;
    // Safe employee summary projected for HR case review; never persisted in the case document.
    [BsonIgnore] public string EmployeeName { get; set; } = string.Empty;
    [BsonIgnore] public string EmployeeId { get; set; } = string.Empty;
    [BsonIgnore] public string EmployeeEmail { get; set; } = string.Empty;
    [BsonIgnore] public string EmployeePhoneNumber { get; set; } = string.Empty;
    [BsonIgnore] public string EmployeeDepartment { get; set; } = string.Empty;
    [BsonIgnore] public string EmployeeDepartmentName { get; set; } = string.Empty;
    [BsonIgnore] public string CaseOwnerName { get; set; } = string.Empty;
    [BsonIgnore] public string EmployeeJobRole { get; set; } = string.Empty;
    [BsonIgnore] public string EmployeeProfileUrl { get; set; } = string.Empty;
    [BsonIgnore] public string? EmployeeEmploymentStatus { get; set; }
    public string LifecycleStatus { get; set; } = string.Empty;
    public string? LeaveRequestId { get; set; }
    public string? LeavePolicyId { get; set; }
    public decimal LeaveTotalDays { get; set; }
    public string LeaveReconciliationStatus { get; set; } = "NotRequired";
    public string Status { get; set; } = "Open";
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public string? Reason { get; set; }
    public string? CaseOwnerUserId { get; set; }
    // Snapshot only the temporary dimensions this case changed. Restoration is
    // conditional, so a later HR decision is never overwritten on return.
    public string? PreviousAccountStatus { get; set; }
    public string? AppliedAccountStatus { get; set; }
    public string? PreviousPayrollStatus { get; set; }
    public string? AppliedPayrollStatus { get; set; }
    public bool DocumentsVerified { get; set; }
    public DateTime? DocumentsVerifiedAtUtc { get; set; }
    public string? DocumentsVerifiedBy { get; set; }
    [BsonIgnore]
    public bool IsReturnOverdue { get; set; }
    public List<EmployeeLifecycleCaseEvidence> Evidence { get; set; } = [];
    public List<EmployeeLifecycleCaseEvent> Events { get; set; } = [];
}

public sealed class EmployeeLifecycleCaseEvidence
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    [JsonPropertyName("evidenceId")]
    public string EvidenceId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    [JsonIgnore]
    public string StorageKey { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "PendingVerification";
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
}

public sealed class EmployeeLifecycleCaseEvent
{
    public string EventType { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
