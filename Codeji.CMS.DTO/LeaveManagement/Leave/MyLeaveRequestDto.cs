using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.LeaveManagement.Leave;

public class MyLeaveRequestResponse
{
    public string LeaveRequestId { get; set; }
    public bool IsHalfDay { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; }
    public string? ReviewedBy { get; set; }
    public string Comment { get; set; }
    public LeaveRequestStatus Status { get; set; }
    public string LeavePolicyName { get; set; }
    public string Code { get; set; }
}