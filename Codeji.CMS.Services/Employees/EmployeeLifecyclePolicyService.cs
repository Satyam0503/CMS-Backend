using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.middlewares;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Employees;

public sealed class EmployeeLifecyclePolicyService : IEmployeeLifecyclePolicyService
{
    private readonly IMongoDbRepository<EmployeeLifecyclePolicy> _repository;
    private readonly IMongoDbRepository<LeavePolicy>? _leavePolicies;
    private readonly IMongoDbRepository<EmployeeLeaveBalance>? _leaveBalances;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmployeeLifecyclePolicyService(
        IMongoDbRepository<EmployeeLifecyclePolicy> repository,
        IHttpContextAccessor httpContextAccessor,
        IMongoDbRepository<LeavePolicy>? leavePolicies = null,
        IMongoDbRepository<EmployeeLeaveBalance>? leaveBalances = null)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
        _leavePolicies = leavePolicies;
        _leaveBalances = leaveBalances;
    }

    public async Task<IReadOnlyList<EmployeeLifecyclePolicy>> GetPoliciesAsync(string? companyId = null)
    {
        var tenantId = ResolveCompanyId(companyId);
        if (string.IsNullOrWhiteSpace(tenantId)) return Array.Empty<EmployeeLifecyclePolicy>();
        return (await _repository.GetAll(x => x.CompanyId == tenantId && x.IsActive && !x.IsDeleted))
            .OrderBy(x => x.EventType)
            .ToList();
    }

    public async Task<EmployeeLifecyclePolicy?> GetPolicyAsync(string eventType, string? companyId = null)
    {
        var tenantId = ResolveCompanyId(companyId);
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(eventType))
            return null;
        return await _repository.FirstOrDefault(x => x.CompanyId == tenantId && x.EventType == eventType && x.IsActive && !x.IsDeleted);
    }

    public async Task<string?> ValidateAvailabilityAsync(string eventType, DateTime? expectedReturnDate, string? reason, string? companyId = null)
    {
        var policy = await GetPolicyAsync(eventType, companyId);
        if (policy is null) return null;
        if (expectedReturnDate.HasValue && expectedReturnDate.Value.Date < DateTime.UtcNow.Date)
            return "LIFECYCLE_RETURN_DATE_INVALID";
        if (policy.RequiresExpectedReturnDate && !expectedReturnDate.HasValue)
            return "LIFECYCLE_RETURN_DATE_REQUIRED";
        if (policy.RequiresReason && string.IsNullOrWhiteSpace(reason))
            return "LIFECYCLE_REASON_REQUIRED";
        return null;
    }

    public async Task<(bool Success, string? Error, EmployeeLifecyclePolicy? Policy)> SavePolicyAsync(EmployeeLifecyclePolicy policy, string? companyId = null, string? actorUserId = null)
    {
        var tenantId = ResolveCompanyId(companyId);
        var allowedEvents = new[] { EmployeeLifecycleStatuses.MedicalLeave, EmployeeLifecycleStatuses.MaternityLeave, EmployeeLifecycleStatuses.PaternityLeave, EmployeeLifecycleStatuses.LongTermLeave, EmployeeLifecycleStatuses.Sabbatical };
        if (string.IsNullOrWhiteSpace(tenantId) || !allowedEvents.Contains(policy.EventType, StringComparer.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(policy.LeavePolicyId)) return (false, "LIFECYCLE_POLICY_INVALID", null);
        if (_leavePolicies is not null && await _leavePolicies.FirstOrDefault(x => x.CompanyId == tenantId && x.Id == policy.LeavePolicyId && !x.IsDeleted && x.Status) is null)
            return (false, "LIFECYCLE_LEAVE_POLICY_NOT_FOUND", null);
        var existing = await _repository.FirstOrDefault(x => x.CompanyId == tenantId && x.EventType == policy.EventType && x.IsActive && !x.IsDeleted);
        var now = DateTime.UtcNow;
        if (existing is null)
        {
            policy.CompanyId = tenantId; policy.EffectiveFromUtc = policy.EffectiveFromUtc == DateTime.MinValue ? now : policy.EffectiveFromUtc; policy.CreatedBy = actorUserId ?? CurrentContext.UserId(_httpContextAccessor); policy.CreatedDate = now; policy.UpdatedBy = policy.CreatedBy; policy.UpdatedDate = now; policy.IsActive = true; policy.IsDeleted = false;
            var added = await _repository.AddOne(policy);
            if (added.Success) await RemoveEntitlementsAsync(tenantId, policy.LeavePolicyId);
            return added.Success ? (true, null, policy) : (false, "LIFECYCLE_POLICY_SAVE_FAILED", null);
        }
        // EffectiveFromUtc is a date-only business value from the UI. Today at
        // midnight is valid; only a date before today is already effective.
        if (policy.EffectiveFromUtc != DateTime.MinValue && policy.EffectiveFromUtc.Date < now.Date) return (false, "LIFECYCLE_POLICY_EFFECTIVE_DATE_INVALID", null);
        var effectiveFromUtc = policy.EffectiveFromUtc == DateTime.MinValue
            ? (existing.EffectiveFromUtc == DateTime.MinValue ? DateTime.SpecifyKind(IndiaTime.Today, DateTimeKind.Utc) : existing.EffectiveFromUtc)
            : DateTime.SpecifyKind(policy.EffectiveFromUtc.Date, DateTimeKind.Utc);
        var currentVersion = existing.Version;
        existing.LeavePolicyId = policy.LeavePolicyId; existing.EffectiveFromUtc = effectiveFromUtc; existing.RequiresHrApproval = policy.RequiresHrApproval; existing.RequiresDocument = policy.RequiresDocument; existing.RequiresExpectedReturnDate = policy.RequiresExpectedReturnDate; existing.RequiresReason = policy.RequiresReason; existing.UpdatedBy = actorUserId ?? CurrentContext.UserId(_httpContextAccessor); existing.UpdatedDate = now; existing.Version = currentVersion + 1;
        // The client can legitimately hold an older snapshot after a refresh or
        // after another lifecycle control was saved. The policy is keyed by the
        // tenant and event/id; rejecting the save on the stale DTO made a
        // successful-looking UI revert to "Not configured" on the next load.
        var updateFilter = policy.Version > 0
            ? Builders<EmployeeLifecyclePolicy>.Filter.Where(x => x.CompanyId == tenantId && x.Id == existing.Id && x.Version == policy.Version)
            : Builders<EmployeeLifecyclePolicy>.Filter.Where(x => x.CompanyId == tenantId && x.Id == existing.Id);
        var updated = await _repository.Update(updateFilter, existing);
        if (updated.Success) await RemoveEntitlementsAsync(tenantId, policy.LeavePolicyId);
        return updated.Success ? (true, null, existing) : (false, "LIFECYCLE_POLICY_VERSION_CONFLICT", null);
    }

    private async Task RemoveEntitlementsAsync(string companyId, string leavePolicyId)
    {
        if (_leaveBalances is null) return;
        var balances = await _leaveBalances.GetAll(x => x.CompanyId == companyId && x.LeavePolicyId == leavePolicyId, withDefaultFilter: false);
        foreach (var balance in balances)
        {
            var filter = Builders<EmployeeLeaveBalance>.Filter.Where(x => x.CompanyId == companyId && x.Id == balance.Id);
            if (balance.Taken > 0)
                await _leaveBalances.UpdateMany(filter, Builders<EmployeeLeaveBalance>.Update
                    .Set(x => x.TotalAllocated, balance.Taken)
                    .Set(x => x.Remaining, 0)
                    .Inc(x => x.Version, 1)
                    .Set(x => x.UpdatedDate, DateTime.UtcNow));
            else
                await _leaveBalances.Delete(filter);
        }
    }


    public async Task<(bool Success, string? Error, EmployeeLifecyclePolicy? Policy)> CreateSpecialLeaveAsync(EmployeeLifecyclePolicy policy, string? companyId = null, string? actorUserId = null)
    {
        var tenantId = ResolveCompanyId(companyId);
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(policy.Name) || string.IsNullOrWhiteSpace(policy.EventType))
            return (false, "LIFECYCLE_SPECIAL_LEAVE_INVALID", null);
        if (!IsAllowedEvent(policy.EventType))
            return (false, "LIFECYCLE_POLICY_EVENT_TYPE_INVALID", null);
        var now = DateTime.UtcNow;
        policy.Id = string.IsNullOrWhiteSpace(policy.Id) ? Guid.NewGuid().ToString() : policy.Id;
        policy.CompanyId = tenantId; policy.IsActive = true; policy.IsDeleted = false;
        policy.CreatedBy = actorUserId ?? CurrentContext.UserId(_httpContextAccessor); policy.CreatedDate = now;
        policy.UpdatedBy = policy.CreatedBy; policy.UpdatedDate = now;
        var result = await _repository.AddOne(policy);
        return result.Success ? (true, null, policy) : (false, "LIFECYCLE_SPECIAL_LEAVE_CREATE_FAILED", null);
    }

    public async Task<(bool Success, string? Error, EmployeeLifecyclePolicy? Policy)> UpdateSpecialLeaveAsync(EmployeeLifecyclePolicy policy, string? companyId = null, string? actorUserId = null)
    {
        var tenantId = ResolveCompanyId(companyId);
        var existing = await _repository.FirstOrDefault(x => x.CompanyId == tenantId && x.Id == policy.Id && x.IsActive && !x.IsDeleted);
        if (existing is null) return (false, "LIFECYCLE_SPECIAL_LEAVE_NOT_FOUND", null);
        if (string.IsNullOrWhiteSpace(policy.Name) || string.IsNullOrWhiteSpace(policy.EventType) || !IsAllowedEvent(policy.EventType))
            return (false, "LIFECYCLE_SPECIAL_LEAVE_INVALID", null);
        existing.Name = policy.Name; existing.EventType = policy.EventType; existing.EffectiveFromUtc = policy.EffectiveFromUtc;
        existing.RequiresHrApproval = policy.RequiresHrApproval; existing.RequiresReason = policy.RequiresReason;
        existing.RequiresDocument = policy.RequiresDocument; existing.RequiresExpectedReturnDate = policy.RequiresExpectedReturnDate;
        existing.UpdatedBy = actorUserId ?? CurrentContext.UserId(_httpContextAccessor); existing.UpdatedDate = DateTime.UtcNow; existing.Version++;
        var result = await _repository.Update(Builders<EmployeeLifecyclePolicy>.Filter.Where(x => x.CompanyId == tenantId && x.Id == existing.Id), existing);
        return result.Success ? (true, null, existing) : (false, "LIFECYCLE_SPECIAL_LEAVE_UPDATE_FAILED", null);
    }

    public async Task<(bool Success, string? Error, EmployeeLifecyclePolicy? Policy)> LinkSpecialLeaveAsync(string specialLeaveId, string leavePolicyId, string? companyId = null, string? actorUserId = null)
    {
        var tenantId = ResolveCompanyId(companyId);
        var special = await _repository.FirstOrDefault(x => x.CompanyId == tenantId && x.Id == specialLeaveId && x.IsActive && !x.IsDeleted);
        if (special is null) return (false, "LIFECYCLE_SPECIAL_LEAVE_NOT_FOUND", null);
        if (_leavePolicies is null) return (false, "LIFECYCLE_LEAVE_POLICY_REPOSITORY_UNAVAILABLE", null);
        var leave = await _leavePolicies.FirstOrDefault(x => x.CompanyId == tenantId && x.Id == leavePolicyId && x.Status && !x.IsDeleted);
        if (leave is null) return (false, "LIFECYCLE_LEAVE_POLICY_NOT_FOUND", null);
        special.LeavePolicyId = leave.Id; special.UpdatedBy = actorUserId ?? CurrentContext.UserId(_httpContextAccessor); special.UpdatedDate = DateTime.UtcNow; special.Version++;
        var result = await _repository.Update(Builders<EmployeeLifecyclePolicy>.Filter.Where(x => x.CompanyId == tenantId && x.Id == special.Id), special);
        return result.Success ? (true, null, special) : (false, "LIFECYCLE_SPECIAL_LEAVE_LINK_FAILED", null);
    }

    public async Task<(bool Success, string? Error, EmployeeLifecyclePolicy? Policy)> UnlinkSpecialLeaveAsync(string specialLeaveId, string? companyId = null, string? actorUserId = null)
    {
        var tenantId = ResolveCompanyId(companyId);
        var special = await _repository.FirstOrDefault(x => x.CompanyId == tenantId && x.Id == specialLeaveId && x.IsActive && !x.IsDeleted);
        if (special is null) return (false, "LIFECYCLE_SPECIAL_LEAVE_NOT_FOUND", null);
        if (_leavePolicies is not null && !string.IsNullOrWhiteSpace(special.LeavePolicyId))
        {
            var leave = await _leavePolicies.FirstOrDefault(x => x.CompanyId == tenantId && x.Id == special.LeavePolicyId && !x.IsDeleted);
            if (leave is not null) { leave.Lifecycle = null; leave.UpdatedBy = actorUserId ?? CurrentContext.UserId(_httpContextAccessor); leave.UpdatedDate = DateTime.UtcNow; await _leavePolicies.Update(Builders<LeavePolicy>.Filter.Where(x => x.CompanyId == tenantId && x.Id == leave.Id), leave); }
        }
        special.LeavePolicyId = null; special.UpdatedBy = actorUserId ?? CurrentContext.UserId(_httpContextAccessor); special.UpdatedDate = DateTime.UtcNow; special.Version++;
        var result = await _repository.Update(Builders<EmployeeLifecyclePolicy>.Filter.Where(x => x.CompanyId == tenantId && x.Id == special.Id), special);
        return result.Success ? (true, null, special) : (false, "LIFECYCLE_SPECIAL_LEAVE_UNLINK_FAILED", null);
    }

    private static bool IsAllowedEvent(string eventType) => new[] { EmployeeLifecycleStatuses.MedicalLeave, EmployeeLifecycleStatuses.MaternityLeave, EmployeeLifecycleStatuses.PaternityLeave, EmployeeLifecycleStatuses.LongTermLeave, EmployeeLifecycleStatuses.Sabbatical, EmployeeLifecycleStatuses.AdministrativeLeave }.Contains(eventType, StringComparer.OrdinalIgnoreCase);
    public async Task<(bool Success, string? Error)> DeleteSpecialLeaveAsync(
    string specialLeaveId,
    string? companyId = null,
    string? actorUserId = null)
{
    var tenantId = ResolveCompanyId(companyId);

    var specialLeave = await _repository.FirstOrDefault(
        x => x.CompanyId == tenantId &&
             x.Id == specialLeaveId &&
             !x.IsDeleted);

    if (specialLeave is null)
    {
        return (false, "LIFECYCLE_SPECIAL_LEAVE_NOT_FOUND");
    }

    if (_leavePolicies is not null &&
        !string.IsNullOrWhiteSpace(specialLeave.LeavePolicyId))
    {
        var leavePolicy = await _leavePolicies.FirstOrDefault(
            x => x.CompanyId == tenantId &&
                 x.Id == specialLeave.LeavePolicyId &&
                 !x.IsDeleted);

        if (leavePolicy is not null)
        {
            leavePolicy.Lifecycle = null;
            leavePolicy.UpdatedBy =
                actorUserId ?? CurrentContext.UserId(_httpContextAccessor);
            leavePolicy.UpdatedDate = DateTime.UtcNow;

            await _leavePolicies.Update(
                Builders<LeavePolicy>.Filter.Where(
                    x => x.CompanyId == tenantId &&
                         x.Id == leavePolicy.Id),
                leavePolicy);
        }
    }

    specialLeave.IsActive = false;
    specialLeave.IsDeleted = true;
    specialLeave.UpdatedBy =
        actorUserId ?? CurrentContext.UserId(_httpContextAccessor);
    specialLeave.UpdatedDate = DateTime.UtcNow;
    specialLeave.Version++;

    var result = await _repository.Update(
        Builders<EmployeeLifecyclePolicy>.Filter.Where(
            x => x.CompanyId == tenantId &&
                 x.Id == specialLeave.Id),
        specialLeave);

    return result.Success
        ? (true, null)
        : (false, "LIFECYCLE_SPECIAL_LEAVE_DELETE_FAILED");
}

    private string? ResolveCompanyId(string? companyId)
        => string.IsNullOrWhiteSpace(companyId) ? CurrentContext.CompanyId(_httpContextAccessor) : companyId;
}
