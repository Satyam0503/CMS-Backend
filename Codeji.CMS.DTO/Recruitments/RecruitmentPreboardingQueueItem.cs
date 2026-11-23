namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentPreboardingQueueItem
{
    public string CaseId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string? ApplicantName { get; set; }
    public string? Email { get; set; }
    public string? VacancyId { get; set; }
    public string? VacancyName { get; set; }
    public string OfferId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public int PendingTaskCount { get; set; }
    public bool ReadyForHire { get; set; }
    public string? FirstBlockerCode { get; set; }
    public List<RecruitmentPreboardingTaskQueueItem> Tasks { get; set; } = [];
}

public sealed class RecruitmentPreboardingTaskQueueItem
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? SkipReason { get; set; }
    public string? AssigneeUserId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
