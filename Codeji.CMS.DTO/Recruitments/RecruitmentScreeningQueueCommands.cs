namespace Codeji.CMS.DTO.Recruitments;
public sealed class RecruitmentScreeningQueueItem
{
    public string ApplicantId { get; set; } = string.Empty;
    public string VacancyId { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string ReviewId { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public int Score { get; set; }
    public string? ReviewerUserId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public double AgeHours { get; set; }
    public bool Overdue { get; set; }
}
public sealed class RecruitmentScreeningQueueReport
{
    public int TotalPending { get; set; }
    public int OverdueCount { get; set; }
    public List<RecruitmentScreeningQueueItem> Items { get; set; } = [];
}
