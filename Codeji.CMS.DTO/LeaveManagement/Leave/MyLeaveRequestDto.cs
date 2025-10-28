using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.LeaveManagement.Leave;

public class MyLeaveRequestResponse
{
    public string LeaveRequestId { get; set; }
    public bool IsHalfDay { get; set; }
    public EnumsHelper.LeaveTypes LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; }
    public string? ReviewedBy { get; set; }
    public string Comment { get; set; }
    public EnumsHelper.LeaveRequestStatus Status { get; set; }
}