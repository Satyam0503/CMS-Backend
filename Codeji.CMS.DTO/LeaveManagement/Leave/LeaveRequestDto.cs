using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.Leave.LeaveRequest;

public class LeaveRequestDto
{
    public string LeaveId { get; set; }
    public string EmployeeId { get; set; }
    public string LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Reason { get; set; }
    public string? ApprovedBy { get; set; }
    public EnumsHelper.LeaveRequestStatus Status { get; set; }
}
