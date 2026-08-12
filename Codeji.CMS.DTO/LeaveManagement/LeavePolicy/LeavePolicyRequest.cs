using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.LeaveManagement.LeavePolicy;

public class LeavePolicyRequest : IValidatableObject
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Status { get; set; } = true;
    public bool Paid { get; set; } = true;
    [EnumDataType(typeof(LeaveAccrualPeriod), ErrorMessage = "Invalid accrual period value")]
    public LeaveAccrualPeriod AccrualPeriod { get; set; }
    public decimal AccrualAmount { get; set; }
    public decimal? MaxBalance { get; set; } = 0;
    public bool CarryOverAllowed { get; set; } = false;
    public decimal? CarryOverLimit { get; set; }
    public int MinNoticeDays { get; set; } = 0;
    public bool HalfDayAllowed { get; set; } = false;
    public bool WeekendInclusive { get; set; } = false;
    public bool HolidayInclusive { get; set; } = false;
    public string? AttendanceStatusCode { get; set; }
    public string? FullDayAttendanceStatusCode { get; set; }
    public string? HalfDayAttendanceStatusCode { get; set; }
    public string[]? ApplicableTo { get; set; } = [];
    [EnumDataType(typeof(LeavePolicyType), ErrorMessage = "Invalid leave policy type value")]
    public LeavePolicyType PolicyType { get; set; } = LeavePolicyType.Leave;
    public WorkFromHomePolicySettingsRequest? WorkFromHome { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PolicyType == LeavePolicyType.Leave && (AccrualAmount < 1 || AccrualAmount > 100))
            yield return new ValidationResult("Accrual amount must be between 1 and 100 for a leave policy.", [nameof(AccrualAmount)]);
    }

}

public class WorkFromHomePolicySettingsRequest
{
    public bool ApprovalRequired { get; set; } = true;
    public string ApproverStrategy { get; set; } = "REPORTING_MANAGER";
    public bool ReasonRequired { get; set; } = true;
    public bool AttachmentRequired { get; set; }
    public int? MaxDaysPerWeek { get; set; }
    public int? MaxDaysPerMonth { get; set; }
    public bool AllowFullDay { get; set; } = true;
    public bool AllowFirstHalf { get; set; }
    public bool AllowSecondHalf { get; set; }
    public bool AllowMixedHalfDayLeave { get; set; }
    public bool AllowOnWeeklyOff { get; set; }
    public bool AllowOnHoliday { get; set; }
    public string FullDayAttendanceStatusCode { get; set; } = string.Empty;
    public string HalfDayAttendanceStatusCode { get; set; } = string.Empty;
    public string? MixedAttendanceStatusCode { get; set; }
    public bool UseEffectiveOfficeSchedule { get; set; } = true;
    public int? FullDayRequiredWorkingMinutes { get; set; }
    public int? HalfDayRequiredWorkingMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public TimeSpan? MaximumLateCheckInTime { get; set; }
}

