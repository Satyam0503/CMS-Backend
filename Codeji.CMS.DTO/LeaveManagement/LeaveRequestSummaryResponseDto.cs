
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.LeaveManagement;

public class LeaveRequestSummaryResponseDto
{
    public int TotalRequests { get; set; }
    public Dictionary<int, int> LeaveStatusSummary { get; set; }
    public List<LeaveTypeSummary> LeaveTypeSummary { get; set; }
}

public class LeaveTypeSummary
{
    public LeaveTypes LeaveType { get; set; }
    public Dictionary<int, int> StatusValues { get; set; }
}
