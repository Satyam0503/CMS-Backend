using MongoDB.Bson.Serialization.Attributes;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Repository.Entities.Leave;

public class LeavePolicy : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string NormalizedName { get; set; } = string.Empty;
    public string NormalizedCode { get; set; } = string.Empty;
    public string? AttendanceStatusCode { get; set; }
    // AttendanceStatusCode is retained as the legacy full-day mapping.
    public string? FullDayAttendanceStatusCode { get; set; }
    public string? HalfDayAttendanceStatusCode { get; set; }
    // Existing documents deserialize to Leave, preserving all historic leave behaviour.
    public LeavePolicyType PolicyType { get; set; } = LeavePolicyType.Leave;
    public WorkFromHomePolicySettings? WorkFromHome { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Status { get; set; } = true;
    public bool Paid { get; set; } = true;

    public LeaveAccrualPeriod AccrualPeriod { get; set; }
    public decimal AccrualAmount { get; set; }
    public decimal? MaxBalance { get; set; } = 0;

    public bool CarryOverAllowed { get; set; } = false;
    public decimal? CarryOverLimit { get; set; }

    public int MinNoticeDays { get; set; } = 0;
    public bool HalfDayAllowed { get; set; } = false;
    public bool WeekendInclusive { get; set; } = false;
    public bool HolidayInclusive { get; set; } = false;
    // For WFH this records the selected employee user IDs. An empty list means
    // all active employees, matching the existing leave-policy convention.
    public List<string>? ApplicableTo { get; set; } = [];
}

/// <summary>
/// Additive WFH-specific settings for a typed LeavePolicy.  This deliberately keeps
/// normal leave accrual fields intact for historic documents while making WFH rules
/// explicit rather than inferring them from a policy name or code.
/// </summary>
public class WorkFromHomePolicySettings
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
