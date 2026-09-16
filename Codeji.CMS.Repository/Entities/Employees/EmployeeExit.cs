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
    // Captured at initiation so scoped clearance resolution is stable if the employee later moves departments.
    public string? EmployeeDepartmentId { get; set; }
    public string ExitType { get; set; } = string.Empty;
    public string Status { get; set; } = "NoticePeriod";
    // Draft, PendingManagerApproval, PendingHrApproval, NoticePeriod,
    // PendingClearance, PendingSettlement, Completed, Rejected, Withdrawn, Cancelled.
    public int Version { get; set; }
    public string? ReportingManagerUserId { get; set; }
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ManagerReviewRemarks { get; set; }
    public DateTime? ManagerReviewedAtUtc { get; set; }
    public string? HrReviewRemarks { get; set; }
    public DateTime? HrReviewedAtUtc { get; set; }
    public DateTime? ProposedLastWorkingDate { get; set; }
    public string? DiscussionRequest { get; set; }
    public string? DiscussionResponse { get; set; }
    public string? DiscussionMode { get; set; }
    // Preserve who requested a discussion so an employee response returns to the
    // correct reviewer instead of always being routed back to the manager.
    public string? DiscussionRequestedByUserId { get; set; }
    public string? DiscussionReturnStatus { get; set; }
    public List<string> DiscussionParticipantUserIds { get; set; } = [];
    public List<EmployeeExitDiscussionMessage> DiscussionMessages { get; set; } = [];
    public string? ResignationAttachmentUrl { get; set; }
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
    public List<EmployeeExitResponsibilityReassignment> ResponsibilityReassignments { get; set; } = [];
    public EmployeeExitInterview? ExitInterview { get; set; }
    public List<EmployeeExitDocument> Documents { get; set; } = [];
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

public sealed class EmployeeExitDiscussionMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string AuthorUserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EmployeeExitPolicySnapshot
{
    public int MinimumNoticeDays { get; set; }
    public string NoticePaySalaryBasis { get; set; } = "GrossMonthly";
    public int NoticePayDailyRateDivisor { get; set; } = 30;
    public bool EnableOffboarding { get; set; } = true;
    public bool RequireManagerReview { get; set; }
    public bool RequireHrReview { get; set; }
    public bool AllowJointDiscussion { get; set; }
    public bool RequireResignationEvidence { get; set; }
    public bool RequireEmployeeAcknowledgement { get; set; }
    public bool EnableFormerEmployeePortal { get; set; }
    public bool EnableNoticePeriodReminders { get; set; } = true;
    public int NoticePeriodReminderEveryDays { get; set; } = 3;
    public List<string> NoticePeriodReminderOffsets { get; set; } = [];
    public bool NotifyNoticePeriodEmployee { get; set; } = true;
    public bool NotifyNoticePeriodReportingManager { get; set; } = true;
    public bool NotifyNoticePeriodHr { get; set; } = true;
    public bool NotifyNoticePeriodAdmins { get; set; }
    public bool NoticePeriodReminderEmail { get; set; } = true;
    public bool RequireHandover { get; set; }
    public bool RequireAssetClearance { get; set; }
    public bool RequireAccessRevocation { get; set; }
    public bool RequireFinanceClearance { get; set; }
    public bool RequireBenefitClearance { get; set; }
    public bool RequireFinalSettlement { get; set; }
    public string PresetLevel { get; set; } = "Custom";
    /// <summary>Copied when the case is created; policy edits never rewrite an active case.</summary>
    public List<EmployeeExitFeatureConfiguration> Features { get; set; } = [];
}

public sealed class EmployeeExitNoticeAdjustment
{
    public string NoticeAdjustmentId { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty; // EmployeeEarlyRelease, CompanyEarlyRelease, FullWaiver, PartialWaiver, Buyout, Recovery, NoticeExtension
    public string Status { get; set; } = "Pending"; // Pending, Approved, Declined, Cancelled, AwaitingEmployeeDecision, EmployeeDeclined
    public string RequestedByUserId { get; set; } = string.Empty;
    public DateTime CurrentLastWorkingDate { get; set; }
    public DateTime RequestedLastWorkingDate { get; set; }
    public int RequiredNoticeDays { get; set; }
    public int ServedNoticeDays { get; set; }
    public int ShortfallDays { get; set; }
    public int WaivedDays { get; set; }
    /// <summary>Remaining shortfall days settled through buyout or recovery.</summary>
    public int RecoveryDays { get; set; }
    public decimal RecoveryAmount { get; set; }
    public decimal DailyRate { get; set; }
    public string? SalaryBasis { get; set; }
    public int SalaryDivisor { get; set; }
    public string? Reason { get; set; }
    public string? DecisionByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    /// <summary>ISSUE #9: Payable offer expires after this time; auto-declined if not responded by employee.</summary>
    public DateTime? ExpiresAtUtc { get; set; }
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
    public string? CompletedByUserId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
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
    public string? FormerEmployeeRequestId { get; set; }
    public DateTime? IssuedToFormerEmployeeAtUtc { get; set; }
    public string? IssuedByUserId { get; set; }
    public DateTime? SeenByFormerEmployeeAtUtc { get; set; }
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
    public DateTime? PersonalEmailVerificationExpiresAtUtc { get; set; }
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
    // The evidence record is the private-storage authority for an issued document.
    // Keeping this association on the request makes each request independently auditable.
    public string? EvidenceId { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
}

public sealed class EmployeeExitClearance
{
    public string ClearanceId { get; set; } = Guid.NewGuid().ToString();
    public string? TemplateId { get; set; }
    public int TemplateVersion { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string TemplateScope { get; set; } = "Global";
    public string? SourceDepartmentId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RequiredPolicyGate { get; set; } = "General";
    public string OwnerUserId { get; set; } = string.Empty;
    public string? OwnerRole { get; set; }
    public string? OwnerPermission { get; set; }
    public bool IsMandatory { get; set; } = true;
    // Nullable keeps legacy clearance records immutable: only new snapshot-backed tasks are recalculated.
    public int? DaysBeforeLastWorkingDay { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? DueAtUtc { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? CompletedByUserId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? OverrideReason { get; set; }
    public DateTime? WaivedAtUtc { get; set; }
    public string? WaivedByUserId { get; set; }
}

public sealed class EmployeeExitResponsibilityReassignment
{
    public string ReassignmentId { get; set; } = Guid.NewGuid().ToString();
    public string Responsibility { get; set; } = string.Empty;
    public string NewOwnerUserId { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EmployeeExitInterview
{
    public string? EmployeeFeedback { get; set; }
    public string? ManagerFeedback { get; set; }
    public string? HrNotes { get; set; }
    public bool? IsRehireEligible { get; set; }
    public string? ConductedByUserId { get; set; }
    public DateTime? ConductedAtUtc { get; set; }
}

public sealed class EmployeeExitDocument
{
    public string DocumentId { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty; // RelievingLetter, ExperienceLetter, FinalSettlementStatement
    public string Status { get; set; } = "Pending"; // Pending, Generated, Issued
    public string? Url { get; set; }
    /// <summary>Whether this generated document is available in the former-employee portal.</summary>
    public bool IsFormerEmployeeVisible { get; set; }
    public DateTime? GeneratedAtUtc { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
}

public sealed class EmployeeExitAuditEvent
{
    public string Action { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
