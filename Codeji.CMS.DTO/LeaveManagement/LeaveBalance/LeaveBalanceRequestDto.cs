using System.ComponentModel.DataAnnotations;
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.LeaveManagement.LeaveBalance;

public class LeaveBalanceRequestDto
{
    public string? Id { get; set; }
    public string EmployeeId { get; set; }
    public DateTime? Year { get; set; } = DateTime.UtcNow.Date;
    public List<LeaveTypeBalance> LeaveTypeBalances { get; set; } = [];
}
public class LeaveTypeBalance
{
    public EnumsHelper.LeaveTypes LeaveType { get; set; }
    public decimal MaximumLeave { get; set; }
    public decimal? RemainingLeave { get; set; }
}

