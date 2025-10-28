using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.LeaveManagement.Leave;

public class LeaveResponseDto
{
    public string LeaveRequestId { get; set; }
    public string EmployeeName { get; set; }
    public string? ProfileUrl { get; set; }
    public Dictionary<string, string>? JobRole { get; set; } = null;
    public bool IsHalfDay { get; set; }
    public EnumsHelper.LeaveTypes LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; }
    public string? ReviewedBy { get; set; }
    public EnumsHelper.LeaveRequestStatus Status { get; set; }
}
