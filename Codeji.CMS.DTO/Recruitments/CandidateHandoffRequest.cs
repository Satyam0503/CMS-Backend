using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Recruitments;

public sealed class CandidateHandoffRequest
{
    [Required]
    public string ApplicantId { get; set; } = string.Empty;
    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;
    [Required]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string EmployeeId { get; set; } = string.Empty;
    public string? RoleId { get; set; }
    public string? DepartmentId { get; set; }
    public string? JobRoleId { get; set; }
    public string? Gender { get; set; }
    public string? ReportingManager { get; set; }
    public string? ScheduleId { get; set; }
    public bool AutoCompletePreboarding { get; set; }
}
