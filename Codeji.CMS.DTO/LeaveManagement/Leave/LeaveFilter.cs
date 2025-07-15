using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.LeaveManagement.Leave;

public class LeaveFilter
{
    public string? EmployeeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public EnumsHelper.LeaveTypes[]? LeaveType { get; set; }
    public EnumsHelper.LeaveRequestStatus[]? Status { get; set; }
    public int PageNo { get; set; }
    public int PageSize { get; set; }
}
