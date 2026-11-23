using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentManagerReviewCommand
{
    [Required] public string DecisionId { get; set; } = string.Empty;
    [Required] public string Decision { get; set; } = string.Empty;
    public string? Notes { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
}

public sealed class RecruitmentManagerReviewQueueItem
{
    public string DecisionId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string VacancyId { get; set; } = string.Empty;
    public string VacancyTitle { get; set; } = string.Empty;
    public string? RecruiterNotes { get; set; }
    public string HiringManagerUserId { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public int Version { get; set; }
}
