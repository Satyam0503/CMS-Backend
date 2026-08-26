using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

/// <summary>
/// Tenant-owned separation audit record. Never delete it as part of employee cleanup.
/// </summary>
public sealed class EmployeeExit : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EmployeeExitId { get; set; } = string.Empty;
    public string EmployeeUserId { get; set; } = string.Empty;
    public string ExitType { get; set; } = string.Empty;
    public string Status { get; set; } = "NoticePeriod";
    // Draft, PendingManagerApproval, PendingHrApproval, NoticePeriod,
    // PendingClearance, PendingSettlement, Completed, Rejected, Withdrawn, Cancelled.
    public int Version { get; set; }
    public string? ReportingManagerUserId { get; set; }
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public List<EmployeeExitClearance> Clearances { get; set; } = [];
    public List<EmployeeExitAuditEvent> AuditHistory { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTime InitiatedAtUtc { get; set; }
    public DateTime LastWorkingDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public sealed class EmployeeExitClearance
{
    public string ClearanceId { get; set; } = Guid.NewGuid().ToString();
    public string Category { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string? OwnerRole { get; set; }
    public bool IsMandatory { get; set; } = true;
    public string Status { get; set; } = "Pending";
    public DateTime? DueAtUtc { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? CompletedByUserId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? OverrideReason { get; set; }
}

public sealed class EmployeeExitAuditEvent
{
    public string Action { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
