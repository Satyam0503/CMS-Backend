namespace Codeji.CMS.DTO.Employee;

public sealed class EmployeeLifecycleCaseSummary
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int PendingDocuments { get; set; }
    public int OverdueReturns { get; set; }
    public int UpcomingReturns { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = [];
}

public sealed class CreateEmployeeLifecycleCaseRequest
{
    public string LeaveRequestId { get; set; } = string.Empty;
    public string? CaseOwnerUserId { get; set; }
    public DateTime? NextReviewDate { get; set; }
}

public sealed class UpdateEmployeeLifecycleCaseRequest
{
    public string CaseId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ActualReturnDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public string? CaseOwnerUserId { get; set; }
    /// <summary>Optional per-case account override. Applied only while this lifecycle case is active.</summary>
    public string? AccountStatus { get; set; }
    /// <summary>Optional per-case payroll-processing override. It does not change the paid/unpaid leave calculation.</summary>
    public string? PayrollStatus { get; set; }
    public string? Comment { get; set; }
    public bool? DocumentsVerified { get; set; }
}

public sealed class VerifyEmployeeLifecycleCaseEvidenceRequest
{
    public string CaseId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public bool Verified { get; set; }
}
