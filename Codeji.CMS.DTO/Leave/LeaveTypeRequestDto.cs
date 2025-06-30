
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.Leave;

public class LeaveTypeRequestDto
{
    public string LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; }
    public int MaxLeaveLength { get; set; }
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; }
    public EnumsHelper.LeaveTypes LeaveTypes { get; set; }
}
