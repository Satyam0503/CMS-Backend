using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

namespace Codeji.CMS.Repository.Entities.Attendance;

/// <summary>
/// A company-owned attendance shift. <see cref="BaseClass.CompanyId"/> is the
/// tenant boundary and must be populated from the authenticated server context,
/// never from a client request.
/// </summary>
public sealed class CompanyOfficeSchedule : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string ScheduleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? CheckInAllowedFrom { get; set; }
    public TimeSpan? CheckInAllowedUntil { get; set; }
    public TimeSpan? CheckOutAllowedFrom { get; set; }
    public TimeSpan? CheckOutAllowedUntil { get; set; }
    public TimeSpan? LateArrivalAfter { get; set; }
    public TimeSpan? EarlyDepartureBefore { get; set; }
    public TimeSpan? HalfDayCheckInAfter { get; set; }
    public TimeSpan? HalfDayCheckOutBefore { get; set; }
    public int RequiredWorkingMinutes { get; set; } = 480;
    public int BreakMinutes { get; set; }
    public int GraceMinutes { get; set; }
    public TimeSpan? FirstHalfStart { get; set; }
    public TimeSpan? FirstHalfEnd { get; set; }
    public TimeSpan? SecondHalfStart { get; set; }
    public TimeSpan? SecondHalfEnd { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public long Version { get; set; }
}

/// <summary>
/// Company-owned department-to-shift assignment. Both the department and shift
/// must be resolved within the inherited <see cref="BaseClass.CompanyId"/>.
/// </summary>
public sealed class DepartmentScheduleAssignment : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string AssignmentId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ScheduleId { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Company-owned employee-to-shift override. The employee and shift must belong
/// to the inherited <see cref="BaseClass.CompanyId"/>.
/// </summary>
public sealed class EmployeeScheduleAssignment : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string AssignmentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ScheduleId { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}
