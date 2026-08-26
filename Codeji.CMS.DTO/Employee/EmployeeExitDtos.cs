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
    public List<EmployeeExitAuditEventDto> AuditHistory { get; set; } = [];
    public List<string> PermittedActions { get; set; } = [];
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

public sealed class ReviewEmployeeExitRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Action { get; set; } = string.Empty; // Approve or Reject
    public string? Notes { get; set; }
}

public sealed class CancelEmployeeExitRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? Reason { get; set; }
}

public sealed class UpdateEmployeeExitDateRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime LastWorkingDate { get; set; }
    public string? Reason { get; set; }
    public bool OverrideNoticePolicy { get; set; }
}

public sealed class UpsertEmployeeExitPolicyRequest
{
    public string? EmployeeExitPolicyId { get; set; }
    public string? DepartmentId { get; set; }
    public string? RoleId { get; set; }
    public int MinimumNoticeDays { get; set; }
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

public sealed class CompleteEmployeeExitClearanceRequest
{
    public string EmployeeExitId { get; set; } = string.Empty;
    public string ClearanceId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? Notes { get; set; }
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
