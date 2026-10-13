using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Employees.Interface;

public interface IEmployeeLifecyclePolicyService
{
    Task<IReadOnlyList<EmployeeLifecyclePolicy>> GetPoliciesAsync(string? companyId = null);
    Task<EmployeeLifecyclePolicy?> GetPolicyAsync(string eventType, string? companyId = null);
    Task<string?> ValidateAvailabilityAsync(string eventType, DateTime? expectedReturnDate, string? reason, string? companyId = null);
    Task<(bool Success, string? Error, EmployeeLifecyclePolicy? Policy)> SavePolicyAsync(EmployeeLifecyclePolicy policy, string? companyId = null, string? actorUserId = null);
    Task<(bool Success, string? Error, EmployeeLifecyclePolicy? Policy)> CreateSpecialLeaveAsync(EmployeeLifecyclePolicy policy, string? companyId = null, string? actorUserId = null);
    Task<(bool Success, string? Error, EmployeeLifecyclePolicy? Policy)> UpdateSpecialLeaveAsync(EmployeeLifecyclePolicy policy, string? companyId = null, string? actorUserId = null);
    Task<(bool Success, string? Error, EmployeeLifecyclePolicy? Policy)> LinkSpecialLeaveAsync(string specialLeaveId, string leavePolicyId, string? companyId = null, string? actorUserId = null);
    Task<(bool Success, string? Error, EmployeeLifecyclePolicy? Policy)> UnlinkSpecialLeaveAsync(string specialLeaveId, string? companyId = null, string? actorUserId = null);
    Task<(bool Success, string? Error)> DeleteSpecialLeaveAsync(string specialLeaveId, string? companyId = null, string? actorUserId = null);
}
