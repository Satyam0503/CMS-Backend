using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.LeaveManagement;

public class LeaveTypeResponseDto
{
    public string LeaveTypeId { get; set; }
    public bool IsHalfDay { get; set; }
    public int? MinAdvanceNoticeDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; }
    public EnumsHelper.LeaveTypes LeaveTypes { get; set; }
    
}
