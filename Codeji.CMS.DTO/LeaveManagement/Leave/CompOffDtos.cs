using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.LeaveManagement.Leave;

public class CompOffCreditRequestDto
{
    [Required]
    public DateTime WorkedDate { get; set; }
    [Range(1, 24)]
    public decimal HoursWorked { get; set; }
    [Range(0.5, 2.0)]
    public decimal CreditDays { get; set; } = 1.0m;
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class ReviewCompOffCreditDto
{
    [Required]
    public string RequestId { get; set; } = string.Empty;
    [Required]
    public bool IsApproved { get; set; }
    public string? ReviewRemarks { get; set; }
}

public class CompOffCreditResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime WorkedDate { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal CreditDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewRemarks { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? CreatedDate { get; set; }
}
