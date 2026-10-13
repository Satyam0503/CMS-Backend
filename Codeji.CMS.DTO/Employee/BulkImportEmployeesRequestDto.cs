using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Employee;

public class BulkImportEmployeesRequestDto
{
    public const int MaxBatchSize = 500;

    [Required]
    [MinLength(1, ErrorMessage = "At least one employee is required.")]
    [MaxLength(MaxBatchSize, ErrorMessage = "Maximum 500 employees are allowed per request.")]
    public required List<BulkImportEmployeeItemDto> Employees { get; set; }
}

public class BulkImportEmployeeItemDto
{
    [Required]
    public required string EmpId { get; set; }

    [Required]
    public required string FirstName { get; set; }

    [Required]
    public required string LastName { get; set; }

    [Required]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public required string Email { get; set; }

    [Required]
    public required string Role { get; set; }

    [Required]
    public required string Department { get; set; }

    [Required]
    public required string JobRole { get; set; }

    public string Gender { get; set; } = "Not Specified";
}
