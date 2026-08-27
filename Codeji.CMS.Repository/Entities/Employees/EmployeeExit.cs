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
    public List<EmployeeExitNoticeAdjustment> NoticeAdjustments { get; set; } = [];
    public List<EmployeeExitHandoverTask> HandoverTasks { get; set; } = [];
    public List<EmployeeExitAsset> Assets { get; set; } = [];
    public List<EmployeeExitAccessItem> AccessItems { get; set; } = [];
    public List<EmployeeExitFinanceItem> FinanceItems { get; set; } = [];
    public List<EmployeeExitBenefitItem> BenefitItems { get; set; } = [];
    public List<EmployeeExitEvidence> Evidence { get; set; } = [];
    public List<EmployeeExitReminder> Reminders { get; set; } = [];
    public EmployeeExitPostExitAccess? PostExitAccess { get; set; }
    public List<EmployeeExitFormerEmployeeRequest> FormerEmployeeRequests { get; set; } = [];
    public List<EmployeeExitAuditEvent> AuditHistory { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTime InitiatedAtUtc { get; set; }
    public DateTime LastWorkingDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? PolicyVersionId { get; set; }
    public int? PolicyVersion { get; set; }
    public EmployeeExitPolicySnapshot? PolicySnapshot { get; set; }
    public EmployeeExitWithdrawal? Withdrawal { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public sealed class EmployeeExitPolicySnapshot
{
    public int MinimumNoticeDays { get; set; }
    public bool RequireManagerReview { get; set; }
    public bool RequireHrReview { get; set; }
    public bool AllowJointDiscussion { get; set; }
    public bool RequireResignationEvidence { get; set; }
    public bool RequireEmployeeAcknowledgement { get; set; }
    public bool EnableFormerEmployeePortal { get; set; }
}

public sealed class EmployeeExitNoticeAdjustment
{
    public string NoticeAdjustmentId { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty; // EmployeeEarlyRelease, CompanyEarlyRelease, FullWaiver, PartialWaiver, Buyout, Recovery, NoticeExtension
    public string Status { get; set; } = "Pending"; // Pending, Approved, Declined, Cancelled
    public string RequestedByUserId { get; set; } = string.Empty;
    public DateTime CurrentLastWorkingDate { get; set; }
    public DateTime RequestedLastWorkingDate { get; set; }
    public int RequiredNoticeDays { get; set; }
    public int ServedNoticeDays { get; set; }
    public int ShortfallDays { get; set; }
    public int WaivedDays { get; set; }
    public decimal RecoveryAmount { get; set; }
    public string? Reason { get; set; }
    public string? DecisionByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
}

public sealed class EmployeeExitWithdrawal
{
    public string Status { get; set; } = "Pending"; // Pending, Approved, Declined
    public string RequestedByUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public string? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public string? DecisionNotes { get; set; }
}

public sealed class EmployeeExitHandoverTask
{
    public string HandoverTaskId { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? AssigneeUserId { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Submitted, ChangesRequested, Approved, Blocked, Waived
    public string? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ReviewNotes { get; set; }
}

public sealed class EmployeeExitAsset
{
    public string AssetExitId { get; set; } = Guid.NewGuid().ToString();
    public string AssetType { get; set; } = string.Empty;
    public string? AssetIdentifier { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, ReturnRequested, PickupScheduled, Shipped, InTransit, Returned, Verified, Damaged, Lost, Waived, RecoveredFinancially
    public string? TrackingReference { get; set; }
    public decimal RecoveryAmount { get; set; }
    public string? Notes { get; set; }
    public string? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
}

public sealed class EmployeeExitAccessItem
{
    public string AccessItemId { get; set; } = Guid.NewGuid().ToString();
    public string System { get; set; } = string.Empty;
    public string Mode { get; set; } = "AtLwd"; // Immediate, Scheduled, AtLwd, Manual, Phased
    public string Status { get; set; } = "Pending"; // Pending, Scheduled, Revoked, Failed, Waived
    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ExternalReference { get; set; }
    public string? Notes { get; set; }
}

public sealed class EmployeeExitFinanceItem
{
    public string FinanceItemId { get; set; } = Guid.NewGuid().ToString();
    public string Category { get; set; } = string.Empty;
    public string Direction { get; set; } = "Informational"; // PayableToEmployee, ReceivableFromEmployee, Informational
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class EmployeeExitBenefitItem
{
    public string BenefitItemId { get; set; } = Guid.NewGuid().ToString();
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime? EffectiveEndDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class EmployeeExitEvidence
{
    public string EvidenceId { get; set; } = Guid.NewGuid().ToString();
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // UploadedFile, Email, SystemRecord, ExternalReference, GeneratedDocument, SignedAcknowledgement
    public string Status { get; set; } = "Submitted"; // Submitted, UnderReview, Verified, NeedsCorrection, Superseded, Archived
    public string Visibility { get; set; } = "EmployeeManagerHR";
    public int Version { get; set; } = 1;
    public string? StorageKey { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string? Sha256 { get; set; }
    public string? ExternalReference { get; set; }
    public string? EmailMessageId { get; set; }
    public string? EmailSubject { get; set; }
    public DateTime? EmailSentAtUtc { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public string? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
}

public sealed class EmployeeExitReminder
{
    public string ReminderId { get; set; } = Guid.NewGuid().ToString();
    public string Trigger { get; set; } = string.Empty;
    public DateTime DueAtUtc { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Sent, Cancelled, Failed
    public string? RecipientUserId { get; set; }
    public string? LastError { get; set; }
    public DateTime? SentAtUtc { get; set; }
}

public sealed class EmployeeExitPostExitAccess
{
    public string? PersonalEmail { get; set; }
    public string? PersonalEmailVerificationTokenHash { get; set; }
    public bool PersonalEmailVerified { get; set; }
    public DateTime? PersonalEmailVerifiedAtUtc { get; set; }
    public string? Mobile { get; set; }
    public bool Enabled { get; set; }
    public string AcknowledgementStatus { get; set; } = "Pending"; // Pending, Viewed, Acknowledged, Disputed, Refused, ClosedWithoutAcknowledgement
    public DateTime? AcknowledgedAtUtc { get; set; }
    public string? AcknowledgementNotes { get; set; }
}

public sealed class EmployeeExitFormerEmployeeRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty; // RelievingLetter, ExperienceLetter, FnFStatement, TaxDocument, Correction, Other
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Issued, Closed
    public string Details { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolutionNotes { get; set; }
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
