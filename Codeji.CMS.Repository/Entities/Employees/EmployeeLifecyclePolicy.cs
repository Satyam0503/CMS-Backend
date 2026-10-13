using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

namespace Codeji.CMS.Repository.Entities.Employees;

/// <summary>
/// Company-owned defaults for lifecycle events. Leave entitlement and paid/unpaid
/// calculations remain owned by LeavePolicy and AttendanceStatusSetting.
/// </summary>
public sealed class EmployeeLifecyclePolicy : BaseClass
{
    public int Version { get; set; }
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;
    public string Name {get;set;}=string.Empty;
    
    public string EventType { get; set; } = string.Empty;
    /// <summary>Optional link to the LeavePolicy that creates this availability state.</summary>
    public string? LeavePolicyId { get; set; }
    /// <summary>UTC instant from which this company rule may be used.</summary>
    public DateTime EffectiveFromUtc { get; set; } = DateTime.MinValue;
    public bool RequiresHrApproval { get; set; }
    public bool RequiresReason { get; set; } = true;
    public bool RequiresDocument { get; set; }
    public bool RequiresExpectedReturnDate { get; set; }
    public bool CreatesEmployeeExit { get; set; }
    public string? DefaultEmploymentStatus { get; set; }
    public string? DefaultAvailabilityStatus { get; set; }
    public string? DefaultAccountStatus { get; set; }
    public string? DefaultPayrollStatus { get; set; }
    public bool AttendanceExpected { get; set; }
    public bool LeaveRequestsAllowed { get; set; } = true;
    public bool WorkFromHomeRequestsAllowed { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
