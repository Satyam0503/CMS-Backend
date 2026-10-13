namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentOnboardingQueueItem
{
    public string CaseId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string EmployeeUserId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int RequiredPendingTasks { get; set; }
    public int CompletionPercent { get; set; }
    public string? FirstBlockingTaskCode { get; set; }
    public string? FirstBlockingTaskTitle { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

