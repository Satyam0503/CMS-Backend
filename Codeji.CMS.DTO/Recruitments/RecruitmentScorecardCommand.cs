using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentScorecardCommand
{
    public string? ScorecardId { get; set; }
    [Required] public string ApplicantId { get; set; } = string.Empty;
    [Required] public string VacancyId { get; set; } = string.Empty;
    public string? InterviewId { get; set; }
    [Range(1, 20)] public int RoundNumber { get; set; } = 1;
    public DateTime? FeedbackDueAtUtc { get; set; }
    public string? Recommendation { get; set; }
    [Required] public string ReviewerUserId { get; set; } = string.Empty;
    [MinLength(1)] public List<RecruitmentScorecardCriterionCommand> Criteria { get; set; } = [];
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
    [Required] public string Status { get; set; } = "Draft";
}

public sealed class RecruitmentScorecardCriterionCommand
{
    [Required] public string Name { get; set; } = string.Empty;
    [Range(1, 5)] public int Rating { get; set; }
    public string? Notes { get; set; }
}
