using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentRequisitionCommand
{
    public string? RequisitionId { get; set; }
    [Required] public string VacancyId { get; set; } = string.Empty;
    [Required] public string PositionTitle { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string? Location { get; set; }
    [Range(1, 10000)] public int Headcount { get; set; } = 1;
    public int? ApprovedHeadcount { get; set; }
    public string? ApprovalReference { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
    [Required] public string Status { get; set; } = "Draft";
}