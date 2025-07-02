using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.LeaveManagement;

public class LeaveTypeResponseDto
{
    public string? LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; }
    public bool IsHalfDay { get; set; }
    public int? MinAdvanceNoticeDate { get; set; }
    public EnumsHelper.LeaveTypes LeaveTypes { get; set; }
    // public EnumsHelper.LeaveDuration LeaveDuration { get; set; }
}
