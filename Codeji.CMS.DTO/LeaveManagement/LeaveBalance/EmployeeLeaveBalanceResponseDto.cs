using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.LeaveManagement.LeaveBalance;

public class EmployeeLeaveBalanceResponseDto
{
    public EnumsHelper.LeaveTypes LeaveType { get; set; }
    public bool IsHalfDay { get; set; }
    public int MinAdvanceNoticeDate { get; set; }
    public decimal MaximumLeave { get; set; }
    public decimal RemainingLeave { get; set; }

}