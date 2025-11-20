namespace Codeji.CMS.DTO.LeaveManagement.LeaveBalance;

public class AllEmployeeLeaveBalance
{
    public string EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string EmployeeCode { get; set; }
    public Dictionary<string, string>? Designation { get; set; }
    public string? ProfilePicture { get; set; }
    public List<LeaveBalanceDetail> LeaveBalances { get; set; }
}
public class LeaveBalanceDetail
{
    public string LeaveBalanceId { get; set; }
    public string LeavePolicyId { get; set; }
    public string LeavePolicyName { get; set; }
    public string LeavePolicyCode { get; set; }
    public decimal Remaining { get; set; }
    public decimal UsedLeave { get; set; }
}
