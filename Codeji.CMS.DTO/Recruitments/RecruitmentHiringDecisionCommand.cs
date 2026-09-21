using System.ComponentModel.DataAnnotations;
namespace Codeji.CMS.DTO.Recruitments;
public sealed class RecruitmentHiringDecisionCommand
{
    public string? DecisionId { get; set; }
    [Required] public string ApplicantId { get; set; } = string.Empty;
    [Required] public string VacancyId { get; set; } = string.Empty;
    [Required] public string Decision { get; set; } = "Hold";
    public string? Notes { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
}
