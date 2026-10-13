using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Leave;

public class EmployeeLeaveBalance : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public required string UserId { get; set; }
    public required string LeavePolicyId { get; set; }
    /// <summary>Total entitlement granted in the current accrual cycle.</summary>
    public decimal TotalAllocated { get; set; }
    /// <summary>Leave approved and consumed in the current accrual cycle.</summary>
    public decimal Taken { get; set; }
    /// <summary>Leave still available to request or approve.</summary>
    public decimal Remaining { get; set; }
    /// <summary>True when HR/Admin has set a person-specific entitlement; scheduled accrual must not overwrite it.</summary>
    public bool IsManualAllocation { get; set; }
    /// <summary>Technical marker that prevents the recurring accrual job from crediting twice.</summary>
    public DateTime? LastAccrual { get; set; }
    public long Version { get; set; }
}
