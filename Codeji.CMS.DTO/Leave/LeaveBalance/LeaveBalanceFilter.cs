namespace Codeji.CMS.DTO.Leave.LeaveBalance;

public class LeaveBalanceFilter
{
    public string? EmployeeId { get; set; }
    public DateTime? Year { get; set; }
    public int PageNo { get; set; }
    public int PageSize { get; set; }
}
