using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

public class WorkFromHomeRequest : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string RequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string DurationType { get; set; } = "FullDay";
    public string ReasonCode { get; set; } = "OTHER";
    public string? ReasonText { get; set; }
    public string Status { get; set; } = "Draft";
    public string ApproverUserId { get; set; } = string.Empty;
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewRemarksCode { get; set; }
    public string? ReviewRemarksText { get; set; }
    public int Version { get; set; } = 1;
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByUserId { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByUserId { get; set; }
}

public class WorkFromHomePolicy : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string PolicyId { get; set; } = string.Empty;
    // Optional during transition: legacy company policies have no link and remain readable.
    public string? LeavePolicyId { get; set; }
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
    // Empty lists with ApplyToAllEmployees=true means company-wide. Otherwise a user must
    // match either an allowed department or an explicitly selected employee.
    public bool ApplyToAllEmployees { get; set; } = true;
    public List<string> ApplicableDepartments { get; set; } = [];
    public List<string> ApplicableEmployeeIds { get; set; } = [];
    public List<string> ExcludedEmployeeIds { get; set; } = [];
    public List<int> ApplicableEmploymentTypes { get; set; } = [];
    // Explicit HR/Admin employee WFH assignments. A zero value is a persisted
    // deallocation; a positive value grants WFH and replaces the weekly default.
    public List<WorkFromHomeEmployeeAllocation> EmployeeAllocations { get; set; } = [];
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

public class WorkFromHomeEmployeeAllocation
{
    public string EmployeeId { get; set; } = string.Empty;
    public int WeeklyLimit { get; set; }
}

public class WorkFromHomeRequestLog : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string LogId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PerformedByUserId { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? RemarksCode { get; set; }
    public string? RemarksText { get; set; }
    public int Version { get; set; }
}

public class AttendanceRemarkOption : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string RemarkOptionId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public List<string> ApplicableStatusCodes { get; set; } = [];
    public bool RequiresAdditionalText { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
