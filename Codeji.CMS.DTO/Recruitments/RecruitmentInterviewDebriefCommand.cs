using System.ComponentModel.DataAnnotations;
namespace Codeji.CMS.DTO.Recruitments;
public sealed class RecruitmentInterviewDebriefCommand
{
    [Required] public string InterviewId { get; set; } = string.Empty;
    [Required] public string Decision { get; set; } = string.Empty;
    public string? Notes { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
}
