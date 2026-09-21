using System.ComponentModel.DataAnnotations;
namespace Codeji.CMS.DTO.Recruitments;
public sealed class RecruitmentInterviewPlanCommand
{
    public string? PlanId { get; set; }
    public string? VacancyId { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    [Required, MinLength(1)] public List<RecruitmentInterviewPlanStageCommand> Stages { get; set; } = [];
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
}
public sealed class RecruitmentInterviewPlanStageCommand
{
    [Range(1, 20)] public int Order { get; set; }
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    [Range(1, 20)] public int RequiredPanelSize { get; set; } = 1;
    public bool ScorecardRequired { get; set; } = true;
    [Range(1, 720)] public int FeedbackDueHours { get; set; } = 24;
}
