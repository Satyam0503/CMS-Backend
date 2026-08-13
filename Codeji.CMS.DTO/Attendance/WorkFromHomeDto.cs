using System.ComponentModel.DataAnnotations;

public class WorkFromHomeRequestDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string DurationType { get; set; } = "FullDay";
    public string ReasonCode { get; set; } = "OTHER";
    public string? ReasonText { get; set; }
}

public class WorkFromHomeDecisionDto
{
    public string RemarksCode { get; set; } = "REVIEWED";
    public string? RemarksText { get; set; }
    public int Version { get; set; }
}

public class WorkFromHomeCancelDto
{
    public int Version { get; set; }
    public string? RemarksCode { get; set; }
    public string? RemarksText { get; set; }
}

public class AttendanceRemarkOptionDto
{
    public string? RemarkOptionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public List<string> ApplicableStatusCodes { get; set; } = [];
    public bool RequiresAdditionalText { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public class WorkFromHomeTimingDto
{
    public bool IsApprovedForToday { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal? TotalHours { get; set; }
    public decimal RequiredHours { get; set; }
    public TimeSpan? OfficeStartTime { get; set; }
    public TimeSpan? OfficeEndTime { get; set; }
    public TimeSpan? CheckInAvailableFrom { get; set; }
    public TimeSpan? CheckInAvailableUntil { get; set; }
    public TimeSpan? CheckOutAvailableFrom { get; set; }
    public TimeSpan? CheckOutAvailableUntil { get; set; }
}

/// <summary>HR/Admin correction for a WFH-owned attendance row.</summary>
public class WorkFromHomeHoursCorrectionDto
{
    [Required] public DateTime CheckInTime { get; set; }
    [Required] public DateTime CheckOutTime { get; set; }
    [Required, MinLength(3)] public string Remarks { get; set; } = string.Empty;
    public int ExceptionVersion { get; set; }
}

public class EmployeeWfhContextDto
{
    public bool IsFeatureEnabled { get; set; }
    public bool IsEligible { get; set; }
    public int WeeklyLimit { get; set; } = 1;
    public decimal UsedThisWeek { get; set; }
    public decimal RemainingThisWeek { get; set; }
    public bool CanSchedule { get; set; }
    public string? DisabledReasonCode { get; set; }
    public DateTime? NextAvailableDate { get; set; }
    public bool ManagerApprovalRequired { get; set; }
    public int MinimumAdvanceNoticeHours { get; set; }
    public DateTime BusinessDate { get; set; }
    public bool IsSameDayRequestCutoffPassed { get; set; }
    public TimeSpan? EffectiveOfficeStartTime { get; set; }
    public TimeSpan? OfficeStartTime { get; set; }
    public TimeSpan? OfficeEndTime { get; set; }
    public TimeSpan? CheckInAvailableFrom { get; set; }
    public TimeSpan? CheckInAvailableUntil { get; set; }
    public TimeSpan? CheckOutAvailableFrom { get; set; }
    public TimeSpan? CheckOutAvailableUntil { get; set; }
}

public class WorkFromHomePolicyDto
{
    public bool IsEnabled { get; set; }
    public int? MaxDaysPerMonth { get; set; }
    public int MaxDaysPerWeek { get; set; } = 1;
    public int? MaxConsecutiveDays { get; set; }
    public int MinimumAdvanceNoticeHours { get; set; }
    public bool AllowBackdatedRequest { get; set; }
    public int? MaximumBackdatedDays { get; set; }
    public bool AllowHalfDay { get; set; }
    public bool AllowMixedDay { get; set; }
    public bool ManagerApprovalRequired { get; set; } = true;
    public bool AllowManagerSelfApproval { get; set; }
    public bool AllowOnWeeklyOff { get; set; }
    public bool AllowOnHoliday { get; set; }
    public bool RequireReason { get; set; } = true;
    public bool RequireAttachment { get; set; }
    public bool ApplyToAllEmployees { get; set; } = true;
    public List<string> ApplicableDepartments { get; set; } = [];
    public List<string> ApplicableEmployeeIds { get; set; } = [];
    public List<string> ExcludedEmployeeIds { get; set; } = [];
    public List<int> ApplicableEmploymentTypes { get; set; } = [];
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public TimeSpan? AllowedCheckInFrom { get; set; }
    public TimeSpan? AllowedCheckInUntil { get; set; }
    public TimeSpan? AllowedCheckOutFrom { get; set; }
    public TimeSpan? AllowedCheckOutUntil { get; set; }
    public TimeSpan? OfficeStartTime { get; set; }
    public TimeSpan? OfficeEndTime { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public decimal FullDayMinimumHours { get; set; } = 8m;
    public decimal HalfDayMinimumHours { get; set; } = 4m;
    public string FullDayAttendanceStatusCode { get; set; } = "WFH";
    public string HalfDayAttendanceStatusCode { get; set; } = "WFH-HD";
    public string MixedAttendanceStatusCode { get; set; } = "WFH+WFO";
    public string? WfhWfoAttendanceStatusCode { get; set; }
    public string? WfhHdAttendanceStatusCode { get; set; }
    public string? WfhSlAttendanceStatusCode { get; set; }
    public string? WfhClAttendanceStatusCode { get; set; }
}

public class WorkFromHomeEmployeeAllocationDto
{
    [Required] public string EmployeeId { get; set; } = string.Empty;
    [Range(0, 31)] public int WeeklyLimit { get; set; }
    // Returned for HR allocation screens. This is calculated from the same
    // policy rules used by employee WFH requests, rather than inferred from a
    // missing override record.
    public bool IsEligible { get; set; }
}

public class WorkFromHomeResponseDto : WorkFromHomeRequestDto
{
    public string RequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ApproverUserId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? ReviewRemarksText { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
