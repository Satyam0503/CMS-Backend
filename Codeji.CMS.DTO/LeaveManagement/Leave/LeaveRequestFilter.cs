using System.ComponentModel.DataAnnotations;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.LeaveManagement.Leave;

public class LeaveRequestFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [EnumDataType(typeof(LeaveRequestStatus))]
    public LeaveRequestStatus? Status { get; set; }
    public int PageNo { get; set; }
    public int PageSize { get; set; }
}
