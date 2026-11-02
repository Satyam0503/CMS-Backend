using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

[BsonIgnoreExtraElements]
public class WorkFromHomeRequest : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string RequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime FromDate { get; set; }
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ToDate { get; set; }
    public string DurationType { get; set; } = "FullDay";
    public string ReasonCode { get; set; } = "OTHER";
    public string? ReasonText { get; set; }
    /// <summary>Optional client retry key, unique within the authenticated company and user.</summary>
    public string? IdempotencyKey { get; set; }
    /// <summary>Hash of normalized request fields; prevents replaying one key for different data.</summary>
    public string? IdempotencyPayloadHash { get; set; }
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
    public bool IsLateSubmission { get; set; }
    public List<WorkFromHomeOfficeConversion> OfficeConversions { get; set; } = [];
}

/// <summary>Auditable, date-specific conversion from approved WFH to verified office work.</summary>
public sealed class WorkFromHomeOfficeConversion
{
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime AttendanceDate { get; set; }
    public DateTime OfficeCheckInTime { get; set; }
    public DateTime OfficeCheckOutTime { get; set; }
    public string PreviousAttendanceStatus { get; set; } = string.Empty;
    public DateTime? PreviousCheckInTime { get; set; }
    public DateTime? PreviousCheckOutTime { get; set; }
    public decimal? PreviousTotalHours { get; set; }
    public string? PreviousRemarks { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string ConvertedByUserId { get; set; } = string.Empty;
    public DateTime ConvertedAtUtc { get; set; } = DateTime.UtcNow;
}

// Policy documents predate several WFH schema revisions. Ignore retired fields so a
// single legacy document cannot prevent unrelated leave-policy reads for a tenant.
[BsonIgnoreExtraElements]
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
    /// <summary>Optimistic concurrency token for policy and employee-allocation updates.</summary>
    public int Version { get; set; }
    /// <summary>Internal write token used to serialize quota claims with policy replacement.</summary>
    public long QuotaLockVersion { get; set; }
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
    public string? WfhWfoAttendanceStatusCode { get; set; } = "WFH+WFO";
    public string? WfhHdAttendanceStatusCode { get; set; } = "WFH-HD";
    public TimeSpan? SameDayCutoffTime { get; set; }
    public int SameDayGraceMinutes { get; set; }
    [MongoDB.Bson.Serialization.Attributes.BsonDefaultValue(true)]
    public bool AllowLateApplicationWithApproval { get; set; } = true;
    public List<string> AllowedDaysOfWeek { get; set; } = [];
    public List<string> BlockedDaysOfWeek { get; set; } = [];
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

/// <summary>Tenant-scoped, retryable WFH audit and notification work committed with its source transition.</summary>
public sealed class WorkFromHomeOutboxEvent : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EventId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string DedupeKey { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // AUDIT, NOTIFICATION, or EMAIL
    public string ActorUserId { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? Action { get; set; }
    public string? RemarksCode { get; set; }
    public string? RemarksText { get; set; }
    public int RequestVersion { get; set; }
    public string? RecipientUserId { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public int? NotificationType { get; set; }
    public bool SendEmail { get; set; }
    public string? RecipientEmail { get; set; }
    public string? EmailSubject { get; set; }
    public string? EmailBody { get; set; }
    public int? EmailType { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime AvailableAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public DateTime? AuditWrittenAtUtc { get; set; }
    public DateTime? NotificationPersistedAtUtc { get; set; }
    public DateTime? LivePushSentAtUtc { get; set; }
    public DateTime? EmailSentAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? LastError { get; set; }
    public int ReplayCount { get; set; }
    public DateTime? ReplayedAtUtc { get; set; }
    public string? ReplayedByUserId { get; set; }
    public List<WorkFromHomeOutboxReplayEntry> ReplayHistory { get; set; } = [];
}

public sealed class WorkFromHomeOutboxReplayEntry
{
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime ReplayedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
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
