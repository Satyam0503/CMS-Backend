namespace Codeji.CMS.DTO.Employee;

/// <summary>Request to start a managed employee separation.</summary>
public sealed class InitiateEmployeeExitRequest
{
    public string EmployeeUserId { get; set; } = string.Empty;
    public string ExitType { get; set; } = string.Empty;
    public DateTime LastWorkingDate { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentUrl { get; set; }
}

public sealed class UpdateEmployeeContractRequest
{
    public string EmployeeUserId { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Action { get; set; } = "Extend"; // Extend or End
    public string? Reason { get; set; }
}

public sealed class CompleteEmployeeExitRequest
{
    /// <summary>Required for a concurrency-safe completion. EmployeeUserId is retained for older clients only.</summary>
    public string? EmployeeExitId { get; set; }
    public string EmployeeUserId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? Notes { get; set; }
}

public sealed class EmployeeExitCaseDto
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string EmployeeUserId { get; set; } = string.Empty;
    public string ExitType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastWorkingDate { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public DateTime InitiatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int Version { get; set; }
    public string PresetLevel { get; set; } = "Custom";
    public List<EmployeeExitFeatureConfigurationRequest> Features { get; set; } = [];
    public EmployeeExitPostExitContactDto? PostExitContact { get; set; }
    public List<EmployeeExitClearanceDto> Clearances { get; set; } = [];
    public List<EmployeeExitHandoverTaskDto> HandoverTasks { get; set; } = [];
    public List<EmployeeExitAssetDto> Assets { get; set; } = [];
    public List<EmployeeExitAccessItemDto> AccessItems { get; set; } = [];
    public List<EmployeeExitFinanceItemDto> FinanceItems { get; set; } = [];
    public List<EmployeeExitBenefitItemDto> BenefitItems { get; set; } = [];
    public List<EmployeeExitEvidenceDetailDto> Evidence { get; set; } = [];
    public List<FormerEmployeePortalRequestDto> FormerEmployeeRequests { get; set; } = [];
    public List<EmployeeExitResponsibilityReassignmentDto> ResponsibilityReassignments { get; set; } = [];
    public EmployeeExitInterviewDto? ExitInterview { get; set; }
    public EmployeeFinalSettlementDto? FinalSettlement { get; set; }
    public List<EmployeeExitDocumentDto> Documents { get; set; } = [];
    public EmployeeExitWithdrawalDto? Withdrawal { get; set; }
    public List<EmployeeExitNoticeAdjustmentDto> NoticeAdjustments { get; set; } = [];
    public List<EmployeeExitAuditEventDto> AuditHistory { get; set; } = [];
    public string? DiscussionRequest { get; set; }
    public string? DiscussionResponse { get; set; }
    public string? DiscussionMode { get; set; }
    public List<string> DiscussionParticipantUserIds { get; set; } = [];
    public List<EmployeeExitDiscussionMessageDto> DiscussionMessages { get; set; } = [];
    public List<string> PermittedActions { get; set; } = [];
    /// <summary>Reviewer context for the current caller only; used to present the next approval accurately.</summary>
    public bool IsAssignedManagerReviewer { get; set; }
    public bool IsHrReviewer { get; set; }
    public int PendingHandoverCount { get; set; }
    public int PendingAssetCount { get; set; }
    public int PendingAccessCount { get; set; }
    public int PendingFinanceCount { get; set; }
    public int PendingBenefitCount { get; set; }
}

public sealed class EmployeeExitDiscussionMessageDto { public string MessageId { get; set; } = string.Empty; public string AuthorUserId { get; set; } = string.Empty; public string Message { get; set; } = string.Empty; public DateTime CreatedAtUtc { get; set; } }

public sealed class EmployeeExitEvidenceDto
{
    public string EvidenceId { get; set; } = string.Empty;
    public string? StorageKey { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}

public sealed class EmployeeExitAuditEventDto
{
    public string Action { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime AtUtc { get; set; }
    public string? Notes { get; set; }
}

public sealed class EmployeeExitClearanceDto
{
    public string ClearanceId { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public DateTime? DueAtUtc { get; set; }
}

public sealed class EmployeeExitHandoverTaskDto { public string HandoverTaskId { get; set; } = string.Empty; public string Title { get; set; } = string.Empty; public string? Details { get; set; } public string? AssigneeUserId { get; set; } public DateTime? DueAtUtc { get; set; } public string Status { get; set; } = string.Empty; }
public sealed class EmployeeExitAssetDto { public string AssetExitId { get; set; } = string.Empty; public string AssetType { get; set; } = string.Empty; public string? AssetIdentifier { get; set; } public string Status { get; set; } = string.Empty; public string? TrackingReference { get; set; } public decimal RecoveryAmount { get; set; } public string? Notes { get; set; } }
public sealed class EmployeeExitAccessItemDto { public string AccessItemId { get; set; } = string.Empty; public string System { get; set; } = string.Empty; public string Mode { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public DateTime? ScheduledAtUtc { get; set; } public string? ExternalReference { get; set; } public string? Notes { get; set; } }
public sealed class EmployeeExitFinanceItemDto { public string FinanceItemId { get; set; } = string.Empty; public string Category { get; set; } = string.Empty; public string Direction { get; set; } = string.Empty; public decimal Amount { get; set; } public string Status { get; set; } = string.Empty; public string? Reference { get; set; } public string? Notes { get; set; } }
public sealed class EmployeeExitBenefitItemDto { public string BenefitItemId { get; set; } = string.Empty; public string Category { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public DateTime? EffectiveEndDate { get; set; } public string? Reference { get; set; } public string? Notes { get; set; } }
public sealed class EmployeeExitEvidenceDetailDto { public string EvidenceId { get; set; } = string.Empty; public string Category { get; set; } = string.Empty; public string Type { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public string Visibility { get; set; } = string.Empty; public string? FileName { get; set; } public string? ContentType { get; set; } public DateTime UploadedAtUtc { get; set; } public string? FormerEmployeeRequestId { get; set; } public DateTime? IssuedToFormerEmployeeAtUtc { get; set; } public DateTime? SeenByFormerEmployeeAtUtc { get; set; } }

public sealed class ReviewEmployeeExitRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Action { get; set; } = string.Empty; // Approve, Discuss, JointDiscussion, Reject, RequestDiscussion
    public string? Notes { get; set; }
    public DateTime? RecommendedLastWorkingDate { get; set; }
    public List<string> DiscussionParticipantUserIds { get; set; } = [];
}

public sealed class RespondToEmployeeExitDiscussionRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime? ProposedLastWorkingDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class RespondToEmployeeExitJointDiscussionRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpsertEmployeeExitHandoverTaskRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? HandoverTaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? AssigneeUserId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? DueAtUtc { get; set; }
}

public sealed class UpdateEmployeeExitClearanceStatusRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string ClearanceId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty; // Completed, Issue
    public string? EvidenceUrl { get; set; }
    public string? Notes { get; set; }
}

public sealed class RecordEmployeeExitInterviewRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? EmployeeFeedback { get; set; }
    public string? ManagerFeedback { get; set; }
    public string? HrNotes { get; set; }
    public bool? IsRehireEligible { get; set; }
}

public sealed class UpsertEmployeeExitReassignmentRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Responsibility { get; set; } = string.Empty;
    public string NewOwnerUserId { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? ProposedLastWorkingDate { get; set; }
}

public sealed class CancelEmployeeExitRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? Reason { get; set; }
}

public sealed class DecideEmployeeExitWithdrawalRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool Approve { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateEmployeeExitDateRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime LastWorkingDate { get; set; }
    public string? Reason { get; set; }
    public bool OverrideNoticePolicy { get; set; }
}

/// <summary>
/// Requests a notice-period adjustment for the exit case.
/// This is a separate workflow step from final exit approval: the employee may request a shorter notice period or buyout,
/// and HR must decide whether to approve it and send the employee an offer before the overall exit case can be finalized.
/// </summary>
public sealed class RequestEmployeeExitNoticeAdjustmentRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime RequestedLastWorkingDate { get; set; }
    public int WaivedDays { get; set; }
    public decimal RecoveryAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// HR approval for a notice adjustment. Approving here is not the same as final exit approval:
/// it creates the employee-facing offer, which must then be accepted or rejected before the case can move forward.
/// </summary>
public sealed class DecideEmployeeExitNoticeAdjustmentRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string NoticeAdjustmentId { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool Approve { get; set; }
    public string? Reason { get; set; }
    public string? DecisionType { get; set; }
    public int? WaivedDays { get; set; }
    public decimal? RecoveryAmount { get; set; }
}

public sealed class UpsertEmployeeExitPolicyRequest
{
    public string? EmployeeExitPolicyId { get; set; }
    /// <summary>Required when updating an existing policy.</summary>
    public int? Version { get; set; }
    /// <summary>Optional final employment outcome. Empty keeps a generic scoped policy.</summary>
    public string? ExitType { get; set; }
    public string? DepartmentId { get; set; }
    /// <summary>Departments sharing this exact policy. DepartmentId remains for legacy single-department records.</summary>
    public List<string> DepartmentIds { get; set; } = [];
    /// <summary>Explicitly replaces selected legacy single-department policies with this shared policy.</summary>
    public bool ConsolidateDepartmentPolicies { get; set; }
    public string? RoleId { get; set; }
    public int MinimumNoticeDays { get; set; }
    public string NoticePaySalaryBasis { get; set; } = "GrossMonthly";
    public int NoticePayDailyRateDivisor { get; set; } = 30;
    public bool EnableOffboarding { get; set; } = true;
    public bool AllowEmployeeSelfResignation { get; set; } = true;
    public bool AllowRejoin { get; set; } = true;
    public bool AllowExitDuringActiveLeave { get; set; }
    public bool RequireManagerReview { get; set; } = true;
    public bool RequireHrReview { get; set; } = true;
    public bool AllowJointDiscussion { get; set; } = true;
    public bool RequireResignationEvidence { get; set; }
    public bool RequireEmployeeAcknowledgement { get; set; }
    public bool EnableFormerEmployeePortal { get; set; }
    public bool EnableNoticePeriodReminders { get; set; } = true;
    public int NoticePeriodReminderEveryDays { get; set; } = 3;
    public List<string> NoticePeriodReminderOffsets { get; set; } = ["30", "15", "7", "3", "1", "0"];
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
    public List<EmployeeExitFeatureConfigurationRequest> Features { get; set; } = [];
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeExitFeatureConfigurationRequest
{
    public string Key { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool BlocksCompletion { get; set; }
    public bool Required { get; set; }
    public string? OwnerPermission { get; set; }
    public bool AllowWaiver { get; set; }
    public bool WaiverReasonRequired { get; set; }
    public int? DueDaysBeforeLastWorkingDate { get; set; }
}

public sealed class UpsertEmployeeExitClearanceTemplateRequest
{
    public string? EmployeeExitClearanceTemplateId { get; set; }
    /// <summary>Required when updating an existing template.</summary>
    public int? Version { get; set; }
    public string? TemplateKey { get; set; }
    public string Scope { get; set; } = "Global";
    public string? DepartmentId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OwnerRole { get; set; }
    /// <summary>Permission that owns the task. Empty means the reporting manager is the fallback owner.</summary>
    public string? OwnerPermission { get; set; }
    public string RequiredPolicyGate { get; set; } = "General";
    public bool IsMandatory { get; set; } = true;
    public int DueDaysBeforeLastWorkingDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
}

/// <summary>
/// Employee decision on the HR-approved notice-period offer.
/// Accepting this offer resolves the notice adjustment step and allows the case to continue according to the approved resolution.
/// Rejecting it keeps the original notice period or triggers the alternative path defined by the policy.
/// </summary>
public sealed class DecideOwnEmployeeExitNoticeOfferRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string NoticeAdjustmentId { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool Accept { get; set; }
}

/// <summary>
/// Cancel a pending notice adjustment. Employees can cancel their own Pending requests;
/// HR can cancel any pending adjustment. Cannot cancel Approved, Declined, or AwaitingEmployeeDecision statuses.
/// </summary>
public sealed class CancelEmployeeExitNoticeAdjustmentRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string NoticeAdjustmentId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? Reason { get; set; }
}

public sealed class ApproveEmployeeFinalSettlementRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
}

public sealed class UpdateEmployeeFinalSettlementPaymentRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // PaymentInitiated, Paid, PaymentFailed, Disputed
    public decimal PaidAmount { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Reason { get; set; }
}

/// <summary>Minimal employee-visible record for a prior withdrawn resignation.</summary>
public sealed class EmployeeExitHistoryDto
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastWorkingDate { get; set; }
    public DateTime InitiatedAtUtc { get; set; }
    public string? WithdrawalReason { get; set; }
}

public sealed class EmployeeExitWithdrawalDto { public string Status { get; set; } = string.Empty; public string? Reason { get; set; } }
public sealed class EmployeeExitNoticeAdjustmentDto { public string NoticeAdjustmentId { get; set; } = string.Empty; public string Type { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public DateTime RequestedLastWorkingDate { get; set; } public int RequiredNoticeDays { get; set; } public int ServedNoticeDays { get; set; } public int ShortfallDays { get; set; } public int WaivedDays { get; set; } public decimal RecoveryAmount { get; set; } public string? Reason { get; set; } }
public sealed class CalculateEmployeeFinalSettlementRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public decimal SalaryPayable { get; set; }
    public decimal Reimbursements { get; set; }
    public decimal LeaveEncashment { get; set; }
    public decimal BonusesAndIncentives { get; set; }
    public decimal Deductions { get; set; }
    public decimal NoticeRecovery { get; set; }
    public decimal OutstandingDues { get; set; }
}

public sealed class MarkEmployeeFinalSettlementPaidRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
}

public sealed class GenerateEmployeeExitDocumentRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? RequestId { get; set; }
}

/// <summary>Safe operational summary for the offboarding queue; it intentionally omits case notes and evidence.</summary>
public sealed class EmployeeExitQueueItemDto
{
    public string EmployeeUserId { get; set; } = string.Empty;
    public string EmployeeExitId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastWorkingDate { get; set; }
    public DateTime InitiatedAtUtc { get; set; }
    public int ProgressPercent { get; set; }
}

public sealed class IssueEmployeeExitDocumentRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? Message { get; set; }
}

/// <summary>Employee-owned contact used for post-exit verification and portal access.</summary>
public sealed class EmployeeExitPostExitContactDto
{
    public string? PersonalEmail { get; set; }
    public string? Mobile { get; set; }
    public bool PersonalEmailVerified { get; set; }
    public bool PortalEnabled { get; set; }
    public DateTime? PersonalEmailVerificationExpiresAtUtc { get; set; }
}

public sealed class EmployeeExitResponsibilityReassignmentDto { public string Responsibility { get; set; } = string.Empty; public string NewOwnerUserId { get; set; } = string.Empty; public string? Notes { get; set; } }
public sealed class EmployeeExitInterviewDto { public string? EmployeeFeedback { get; set; } public string? ManagerFeedback { get; set; } public string? HrNotes { get; set; } public bool? IsRehireEligible { get; set; } public DateTime? ConductedAtUtc { get; set; } }
public sealed class EmployeeFinalSettlementDto { public string Status { get; set; } = string.Empty; public decimal SalaryPayable { get; set; } public decimal Deductions { get; set; } public decimal NoticeRecovery { get; set; } public decimal NetPayable { get; set; } public string? SourcePayrollId { get; set; } public DateTime? SourcePayrollMonth { get; set; } public string? SourcePayrollRunId { get; set; } public string? SourcePayrollRunEmployeeId { get; set; } public DateTime? CalculatedAtUtc { get; set; } public DateTime? ApprovedAtUtc { get; set; } public DateTime? PaidAtUtc { get; set; } }
public sealed class EmployeeExitDocumentDto { public string Type { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public string? Url { get; set; } }
public sealed class CompleteEmployeeExitClearanceRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string ClearanceId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? Notes { get; set; }
}

public sealed class ReviewEmployeeExitHandoverTaskRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string HandoverTaskId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Action { get; set; } = string.Empty; // Submit, Approve, ChangesRequested, Blocked, Waived
    public string? Notes { get; set; }
}

public sealed class UpsertEmployeeExitAssetRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string? AssetExitId { get; set; }
    public int Version { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string? AssetIdentifier { get; set; }
    public string Status { get; set; } = "Pending";
    public string? TrackingReference { get; set; }
    public decimal RecoveryAmount { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpsertEmployeeExitAccessItemRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string? AccessItemId { get; set; }
    public int Version { get; set; }
    public string System { get; set; } = string.Empty;
    public string Mode { get; set; } = "AtLwd";
    public string Status { get; set; } = "Pending";
    public DateTime? ScheduledAtUtc { get; set; }
    public string? ExternalReference { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpsertEmployeeExitFinanceItemRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string? FinanceItemId { get; set; }
    public int Version { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Direction { get; set; } = "Informational";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpsertEmployeeExitBenefitItemRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string? BenefitItemId { get; set; }
    public int Version { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime? EffectiveEndDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class AddEmployeeExitEvidenceRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Visibility { get; set; } = "EmployeeManagerHR";
    public string? StorageKey { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string? Sha256 { get; set; }
    public string? FormerEmployeeRequestId { get; set; }
    public string? ExternalReference { get; set; }
    public string? EmailMessageId { get; set; }
    public string? EmailSubject { get; set; }
    public DateTime? EmailSentAtUtc { get; set; }
}

public sealed class VerifyEmployeeExitEvidenceRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool Verified { get; set; }
}

public sealed class SupersedeEmployeeExitEvidenceRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public int Version { get; set; }
}

public sealed class UpdateEmployeeExitPostExitContactRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string PersonalEmail { get; set; } = string.Empty;
    public string? Mobile { get; set; }
}

public sealed class AcknowledgeEmployeeExitRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? Notes { get; set; }
}

public sealed class CreateEmployeeExitFormerEmployeeRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class ResolveEmployeeExitFormerEmployeeRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty; // InProgress, Issued, Closed
    public string? Notes { get; set; }
}

public sealed class CreateFormerEmployeePortalInviteRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public int ValidForHours { get; set; } = 72;
}

public sealed class RevokeFormerEmployeePortalAccessRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class FormerEmployeePortalTokenRequest
{
    public string Token { get; set; } = string.Empty;
}

public sealed class FormerEmployeePortalOtpRequest { public string PersonalEmail { get; set; } = string.Empty; }
public sealed class FormerEmployeePortalOtpVerifyRequest { public string PersonalEmail { get; set; } = string.Empty; public string Otp { get; set; } = string.Empty; }
public sealed class SetFormerEmployeePortalAccessRequest { public string EmployeeExitId { get; set; } = string.Empty; public int Version { get; set; } public bool Enabled { get; set; } public string? Reason { get; set; } }
public sealed class SetCompanyFormerEmployeePortalRequest { public bool Enabled { get; set; } public string? Reason { get; set; } }

public sealed class FormerEmployeePortalRequest
{
    public string Type { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class FormerEmployeePortalInviteDto
{
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class FormerEmployeePortalSessionDto
{
    public string SessionToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string? Theme { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyLogo { get; set; }
}

public sealed class FormerEmployeePortalCaseDto
{
    public string? CompanyName { get; set; }
    public string? CompanyLogo { get; set; }
    public string EmployeeExitId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastWorkingDate { get; set; }
    public FormerEmployeePortalProfileDto? Profile { get; set; }
    public List<FormerEmployeePortalRequestDto> Requests { get; set; } = [];
    public List<EmployeeExitEvidenceDto> Documents { get; set; } = [];
}

/// <summary>Non-sensitive employment summary exposed only through an authenticated former-employee portal session.</summary>
public sealed class FormerEmployeePortalProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string? JobRole { get; set; }
    public string? Department { get; set; }
    public DateTime? DateOfJoining { get; set; }
    public DateTime LastWorkingDate { get; set; }
}

public sealed class FormerEmployeePortalRequestDto
{
    public string RequestId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public string? EvidenceId { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
}

public sealed class RejoinEmployeeRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class ExceptionalCompleteEmployeeExitRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Reason { get; set; } = string.Empty;
}
