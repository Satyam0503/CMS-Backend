namespace Codeji.CMS.DTO.LeaveManagement.LeaveBalance;

public class LeaveBalanceResponseDto
{
    public string? Id { get; set; }
    public string EmployeeId { get; set; }
    public DateTime Year { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public List<LeaveTypeBalance> LeaveTypeBalances { get; set; }
}
