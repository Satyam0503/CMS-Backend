using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.LeaveManagement.LeavePolicy;

public class LeavePolicyRequest
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Status { get; set; } = true;
    public bool Paid { get; set; } = true;
    [EnumDataType(typeof(LeaveAccrualPeriod), ErrorMessage = "Invalid accrual period value")]
    public LeaveAccrualPeriod AccrualPeriod { get; set; }
    [Range(1, 100)]
    public decimal AccrualAmount { get; set; }
    public decimal? MaxBalance { get; set; } = 0;
    public bool CarryOverAllowed { get; set; } = false;
    public decimal? CarryOverLimit { get; set; }
    public int MinNoticeDays { get; set; } = 0;
    public bool HalfDayAllowed { get; set; } = false;
    public bool WeekendInclusive { get; set; } = false;
    public bool HolidayInclusive { get; set; } = false;
    public string? AttendanceStatusCode { get; set; }
    public string[]? ApplicableTo { get; set; } = [];

}

