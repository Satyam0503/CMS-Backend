

namespace Codeji.CMS.DTO.LeaveManagement;

public class LeaveRequestSummaryResponseDto
{
    public int TotalRequests { get; set; }
    public Dictionary<int, int> LeaveStatusSummary { get; set; }
    public List<LeaveTypeSummary> LeaveTypeSummary { get; set; }
}

public class LeaveTypeSummary
{
    public string LeaveTypeName { get; set; }
    public string LeaveTypeCode { get; set; }
    public Dictionary<int, int> StatusValues { get; set; }
}
