using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentTalentPoolCommand
{
    public string? MemberId { get; set; }
    [Required] public string ApplicantId { get; set; } = string.Empty;
    [Required] public string PoolName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string? Notes { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
    [Required] public string Status { get; set; } = "Active";
}