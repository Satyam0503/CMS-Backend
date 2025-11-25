namespace Codeji.CMS.DTO.LeaveManagement.LeaveBalance;

public class LeaveBalanceFilter
{
    public string EmployeeName { get; set; }
    public int PageNo { get; set; }
    public int PageSize { get; set; }
    public List<string> LeavePolicies { get; set; }
}
