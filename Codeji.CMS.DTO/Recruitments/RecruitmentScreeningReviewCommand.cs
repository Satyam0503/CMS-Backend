using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentScreeningReviewCommand
{
    public string? ReviewId { get; set; }
    [Required] public string ApplicantId { get; set; } = string.Empty;
    [Required] public string VacancyId { get; set; } = string.Empty;
    public string? ReviewerUserId { get; set; }
    public string? ResumeVersion { get; set; }
    [Range(0, 100)] public int Score { get; set; }
    [Required] public string Decision { get; set; } = "Pending";
    public string? RejectionReason { get; set; }
    public string? Evidence { get; set; }
    public List<RecruitmentScreeningCriterionCommand> Criteria { get; set; } = [];
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
}

public sealed class RecruitmentScreeningCriterionCommand
{
    [Required] public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Notes { get; set; }
}