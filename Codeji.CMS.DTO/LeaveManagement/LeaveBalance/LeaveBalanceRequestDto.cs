using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.LeaveManagement.LeaveBalance;

public class LeaveBalanceRequestDto
{
    public string? LeaveBalanceId { get; set; }
    public required string EmployeeId { get; set; }
    public required string LeavePolicyId { get; set; }
    public decimal Balance { get; set; }
}
