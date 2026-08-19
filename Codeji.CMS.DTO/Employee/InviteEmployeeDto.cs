using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Employee;

public class InviteEmployeeDto
{
    public string? UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    [EmailAddress(ErrorMessage = "Invalid Email")]
    public required string Email { get; set; }
    public required string EmployeeId { get; set; }
    public string? RoleId { get; set; }
    public string? DepartmentId { get; set; }
    public string? JobRoleId { get; set; }
    public string? Gender { get; set; }
    public string? ReportingManager { get; set; }
    public string? ScheduleId { get; set; }
}
