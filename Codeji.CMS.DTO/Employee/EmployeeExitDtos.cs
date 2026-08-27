namespace Codeji.CMS.DTO.Employee;

/// <summary>Request to start a managed employee separation.</summary>
public sealed class InitiateEmployeeExitRequest
{
    public string EmployeeUserId { get; set; } = string.Empty;
    public string ExitType { get; set; } = string.Empty;
    public DateTime LastWorkingDate { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

public sealed class CompleteEmployeeExitRequest
{
    public string EmployeeUserId { get; set; } = string.Empty;
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
    public List<EmployeeExitClearanceDto> Clearances { get; set; } = [];
    public List<EmployeeExitHandoverTaskDto> HandoverTasks { get; set; } = [];
    public List<EmployeeExitAssetDto> Assets { get; set; } = [];
    public List<EmployeeExitAccessItemDto> AccessItems { get; set; } = [];
    public List<EmployeeExitFinanceItemDto> FinanceItems { get; set; } = [];
    public List<EmployeeExitBenefitItemDto> BenefitItems { get; set; } = [];
    public List<EmployeeExitEvidenceDetailDto> Evidence { get; set; } = [];
    public List<FormerEmployeePortalRequestDto> FormerEmployeeRequests { get; set; } = [];
    public List<EmployeeExitAuditEventDto> AuditHistory { get; set; } = [];
    public List<string> PermittedActions { get; set; } = [];
    public int PendingHandoverCount { get; set; }
    public int PendingAssetCount { get; set; }
    public int PendingAccessCount { get; set; }
    public int PendingFinanceCount { get; set; }
    public int PendingBenefitCount { get; set; }
}

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
public sealed class EmployeeExitEvidenceDetailDto { public string EvidenceId { get; set; } = string.Empty; public string Category { get; set; } = string.Empty; public string Type { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public string Visibility { get; set; } = string.Empty; public string? FileName { get; set; } public string? ContentType { get; set; } public DateTime UploadedAtUtc { get; set; } }

public sealed class ReviewEmployeeExitRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Action { get; set; } = string.Empty; // Approve, Discuss, or JointDiscussion
    public string? Notes { get; set; }
    public DateTime? ProposedLastWorkingDate { get; set; }
}

public sealed class RespondToEmployeeExitDiscussionRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
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

public sealed class DecideEmployeeExitNoticeAdjustmentRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string NoticeAdjustmentId { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool Approve { get; set; }
    public string? Reason { get; set; }
}

public sealed class UpsertEmployeeExitPolicyRequest
{
    public string? EmployeeExitPolicyId { get; set; }
    public string? DepartmentId { get; set; }
    public string? RoleId { get; set; }
    public int MinimumNoticeDays { get; set; }
    public bool RequireManagerReview { get; set; } = true;
    public bool RequireHrReview { get; set; } = true;
    public bool AllowJointDiscussion { get; set; } = true;
    public bool RequireResignationEvidence { get; set; }
    public bool RequireEmployeeAcknowledgement { get; set; }
    public bool EnableFormerEmployeePortal { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpsertEmployeeExitClearanceTemplateRequest
{
    public string? EmployeeExitClearanceTemplateId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? OwnerRole { get; set; }
    public bool IsMandatory { get; set; } = true;
    public int DueDaysBeforeLastWorkingDate { get; set; }
    public bool IsActive { get; set; } = true;
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

public sealed class CompleteEmployeeExitClearanceRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string ClearanceId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpsertEmployeeExitHandoverTaskRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string? HandoverTaskId { get; set; }
    public int Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? AssigneeUserId { get; set; }
    public DateTime? DueAtUtc { get; set; }
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

public sealed class FormerEmployeePortalRequest
{
    public string Type { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class FormerEmployeePortalInviteDto
{
    public string PortalToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class FormerEmployeePortalSessionDto
{
    public string SessionToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class FormerEmployeePortalCaseDto
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastWorkingDate { get; set; }
    public List<FormerEmployeePortalRequestDto> Requests { get; set; } = [];
    public List<EmployeeExitEvidenceDto> Documents { get; set; } = [];
}

public sealed class FormerEmployeePortalRequestDto
{
    public string RequestId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
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
