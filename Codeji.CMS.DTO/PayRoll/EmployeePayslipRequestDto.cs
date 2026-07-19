using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.PayRoll;

public class EmployeePayslipRequestDto
{
    [Required]
    public required string EmployeeId { get; set; }

    [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100.")]
    public int Year { get; set; }

    [Range(1, 12, ErrorMessage = "Month must be between 1 and 12.")]
    public int Month { get; set; } // 1 = Jan, 12 = Dec
}
