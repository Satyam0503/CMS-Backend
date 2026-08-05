using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.LeaveManagement.LeaveBalance;

public class LeaveBalanceRequestDto
{
    public string? LeaveBalanceId { get; set; }
    public required string EmployeeId { get; set; }
    public required string LeavePolicyId { get; set; }
    /// <summary>The employee-specific entitlement. Remaining is calculated as TotalAllocated - Taken.</summary>
    public decimal TotalAllocated { get; set; }
    /// <summary>Optimistic-concurrency value supplied by a client that previously read the allocation.</summary>
    public long? ExpectedVersion { get; set; }
    /// <summary>Explicit HR/Admin deallocation of this employee-policy assignment.</summary>
    public bool Remove { get; set; }
}
