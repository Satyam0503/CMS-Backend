namespace Codeji.CMS.DTO.Leave.LeaveBalance;

public class LeaveBalanceRequestDto
{
    public string Id { get; set; }
    public string EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string CreatedBy { get; set; }
    public DateTime Year { get; set; }
    public List<LeaveTypeBalance> LeaveTypeBalances { get; set; }
}
public class LeaveTypeBalance {
    public string LeaveTypeId { get; set; }
    public int MaximumLeave { get; set; }
    public int? RemainingLeave { get; set; }
}

