using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.Leave;

public class LeaveTypeResponseDto
{
    public string? LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; }
    public int MaxLeaveLength { get; set; }
    public EnumsHelper.LeaveTypes LeaveTypes { get; set; }
    // public EnumsHelper.LeaveDuration LeaveDuration { get; set; }
}
