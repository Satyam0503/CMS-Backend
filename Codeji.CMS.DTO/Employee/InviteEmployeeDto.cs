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
}
