
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.LeaveManagement;

public class LeaveTypeRequestDto
{
    public string? LeaveTypeId { get; set; }
    public bool IsHalfDay { get; set; }
    public int MaxLeaveDays { get; set; }
    public int? MinAdvanceNoticeDate { get; set; }
    public bool IsActive { get; set; } = false;
    public EnumsHelper.LeaveTypes LeaveType { get; set; }
}
