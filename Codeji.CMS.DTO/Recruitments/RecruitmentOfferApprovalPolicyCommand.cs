using System.ComponentModel.DataAnnotations;
namespace Codeji.CMS.DTO.Recruitments;
public sealed class RecruitmentOfferApprovalPolicyCommand
{
    public string? PolicyId { get; set; }
    public string? VacancyId { get; set; }
    public string? LegalEntityId { get; set; }
    public string? DepartmentId { get; set; }
    public string? Grade { get; set; }
    [Required] public string Currency { get; set; } = "INR";
    [Range(0, double.MaxValue)] public decimal ApprovalThreshold { get; set; }
    public bool RequireApprovalReference { get; set; } = true;
    public string? ApproverRoleId { get; set; }
    public string? DelegatedApproverUserId { get; set; }
    public DateTime? DelegationExpiresAtUtc { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
}



