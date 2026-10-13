using System.ComponentModel.DataAnnotations;
namespace Codeji.CMS.DTO.Recruitments;
public sealed class RecruitmentOnboardingUpdateCommand
{
    [Required] public string CaseId { get; set; } = string.Empty;
    [Required] public string Status { get; set; } = string.Empty;
    public List<RecruitmentOnboardingTaskUpdate> Tasks { get; set; } = [];
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
}
public sealed class RecruitmentOnboardingTaskUpdate
{
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Status { get; set; } = string.Empty;
}
