
namespace Codeji.CMS.DTO.LeaveManagement.LeaveBalance;

public class EmployeeLeaveBalanceResponseDto
{

    public string LeaveBalanceId { get; set; }
    public string LeavePolicyId { get; set; }
    public decimal TotalAllocated { get; set; }
    public decimal Taken { get; set; }
    public decimal Remaining { get; set; }
    public long Version { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public bool Paid { get; set; }
    public int MinNoticeDays { get; set; }
    public bool HalfDayAllowed { get; set; }
    public bool WeekendInclusive { get; set; }
    public bool HolidayInclusive { get; set; }
}
