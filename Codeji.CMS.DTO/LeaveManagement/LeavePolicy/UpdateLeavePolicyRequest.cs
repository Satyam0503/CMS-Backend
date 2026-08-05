using System.ComponentModel.DataAnnotations;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.LeaveManagement.LeavePolicy;

public class UpdateLeavePolicyRequest
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string Description { get; set; }
    public required bool Status { get; set; }
    public required bool Paid { get; set; }
    [EnumDataType(typeof(LeaveAccrualPeriod), ErrorMessage = "Invalid accrual period value")]
    public LeaveAccrualPeriod AccrualPeriod { get; set; }
    public decimal? AccrualAmount { get; set; }
    public required decimal MaxBalance { get; set; }
    public required bool CarryOverAllowed { get; set; }
    public decimal? CarryOverLimit { get; set; }
    public required int MinNoticeDays { get; set; }
    public required bool HalfDayAllowed { get; set; }
    public required bool WeekendInclusive { get; set; }
    public required bool HolidayInclusive { get; set; }
    public string? AttendanceStatusCode { get; set; }
    public string[]? ApplicableTo { get; set; } = [];
    [EnumDataType(typeof(LeavePolicyType), ErrorMessage = "Invalid leave policy type value")]
    public LeavePolicyType PolicyType { get; set; } = LeavePolicyType.Leave;
    public WorkFromHomePolicySettingsRequest? WorkFromHome { get; set; }
}
