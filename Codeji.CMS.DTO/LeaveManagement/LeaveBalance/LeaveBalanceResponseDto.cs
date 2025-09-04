namespace Codeji.CMS.DTO.LeaveManagement.LeaveBalance;

public class LeaveBalanceResponseDto
{
    public string Id { get; set; }
    public string EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string? ProfileUrl { get; set; }
    public Dictionary<string, string>? JobRole { get; set; }
    public DateTime Year { get; set; }
    public LeaveTypeBalance? SickLeave { get; set; }
    public LeaveTypeBalance? CasualLeave { get; set; }
    public LeaveTypeBalance? EarnedLeave { get; set; }
    public LeaveTypeBalance? MaternityLeave { get; set; }
    public LeaveTypeBalance? PaternityLeave { get; set; }
}
