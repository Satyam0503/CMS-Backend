
namespace Codeji.CMS.DTO.LeaveManagement.LeaveBalance;

public class EmployeeLeaveBalanceResponseDto
{

    public string LeaveBalanceId { get; set; }
    public string LeavePolicyId { get; set; }
    public decimal Balance { get; set; }
    public decimal UsedBalance { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public bool Paid { get; set; }
    public int MinNoticeDays { get; set; }
    public bool HalfDayAllowed { get; set; }
    public bool WeekendInclusive { get; set; }
    public bool HolidayInclusive { get; set; }
}