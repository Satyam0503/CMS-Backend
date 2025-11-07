using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.Leave.LeaveRequest;

public class LeaveRequestDto
{
    public string? LeaveRequestId { get; set; }
    public bool IsHalfDay { get; set; }
    public EnumsHelper.LeaveTypes LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; }
    public string? ReviewedBy { get; set; }
    public EnumsHelper.LeaveRequestStatus? Status { get; set; }
    public string? UserId { get; set; }
}

public class LeaveRequestUpdateDto
{
    public string Comment { get; set; }
    public EnumsHelper.LeaveRequestStatus Status { get; set; }
}