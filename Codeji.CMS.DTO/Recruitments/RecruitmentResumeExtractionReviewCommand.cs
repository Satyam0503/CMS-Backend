using System.ComponentModel.DataAnnotations;
namespace Codeji.CMS.DTO.Recruitments;
public sealed class RecruitmentResumeExtractionReviewCommand
{
    public string? ReviewId { get; set; }
    [Required] public string ApplicantId { get; set; } = string.Empty;
    [Required] public string VacancyId { get; set; } = string.Empty;
    public string? ResumeVersion { get; set; }
    [Required] public Dictionary<string, string> ExtractedFields { get; set; } = [];
    [Required] public string Decision { get; set; } = "Pending";
    public string? Notes { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
}
