using System.ComponentModel.DataAnnotations;
namespace Codeji.CMS.DTO.Recruitments;
public sealed class RecruitmentCandidatePrivacyCommand
{
    [Required] public string ApplicantId { get; set; } = string.Empty;
    [Required] public string Action { get; set; } = string.Empty;
    [Range(30, 3650)] public int? RetentionDays { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
