using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace Codeji.CMS.Services.Employees;

public sealed class EmployeeLifecycleCaseService : IEmployeeLifecycleCaseService
{
    private static readonly string[] LeaveDrivenStatuses = [
        EmployeeLifecycleStatuses.MedicalLeave, EmployeeLifecycleStatuses.MaternityLeave,
        EmployeeLifecycleStatuses.PaternityLeave, EmployeeLifecycleStatuses.LongTermLeave,
        EmployeeLifecycleStatuses.Sabbatical, EmployeeLifecycleStatuses.AdministrativeLeave];
    private readonly IMongoDbRepository<EmployeeLifecycleCase> _cases;
    private readonly IMongoDbRepository<LeaveRequest> _leaveRequests;
    private readonly IMongoDbRepository<EmpUser> _employees;
    private readonly IMongoDbRepository<Department> _departments;
    private readonly IEmployeeLifecyclePolicyService _lifecyclePolicies;
    private readonly IRoleService _roles;
    private readonly IHttpContextAccessor _http;
    private readonly Codeji.CMS.Services.Attendance.ILifecycleAttendanceReconciliationService? _lifecycleAttendanceReconciliation;

    public EmployeeLifecycleCaseService(IMongoDbRepository<EmployeeLifecycleCase> cases,
        IMongoDbRepository<LeaveRequest> leaveRequests,
        IMongoDbRepository<EmpUser> employees,
        IMongoDbRepository<Department> departments,
        IEmployeeLifecyclePolicyService lifecyclePolicies, IRoleService roles,
        IHttpContextAccessor http,
        Codeji.CMS.Services.Attendance.ILifecycleAttendanceReconciliationService? lifecycleAttendanceReconciliation = null)
    { _cases = cases; _leaveRequests = leaveRequests; _employees = employees; _departments = departments; _lifecyclePolicies = lifecyclePolicies; _roles = roles; _http = http; _lifecycleAttendanceReconciliation = lifecycleAttendanceReconciliation; }

    public async Task<IReadOnlyList<EmployeeLifecycleCase>> GetCasesAsync(string? employeeUserId = null, string? status = null, bool overdueOnly = false)
    {
        if (!await CanManage()) return [];
        var companyId = CurrentContext.CompanyId(_http);
        var items = (await _cases.GetAll(x => x.CompanyId == companyId && (string.IsNullOrWhiteSpace(employeeUserId) || x.EmployeeUserId == employeeUserId)))
            .OrderByDescending(x => x.EffectiveFromUtc).ToList();
        var employeeIds = items.Select(x => x.EmployeeUserId).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.Ordinal);
        var employees = await _employees.GetAll(x => x.CompanyId == companyId && employeeIds.Contains(x.UserId) && !x.IsDeleted);
        var employeeById = employees.ToDictionary(x => x.UserId, StringComparer.Ordinal);
        var departments = await _departments.GetAll(x => x.CompanyId == companyId && !x.IsDeleted);
        var departmentNames = departments.ToDictionary(x => x.DepartmentId, x => x.Titles?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Label))?.Label?.Trim() ?? x.DepartmentId, StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (!employeeById.TryGetValue(item.EmployeeUserId, out var employee)) continue;
            item.EmployeeName = string.Join(" ", new[] { employee.FirstName, employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            item.EmployeeId = employee.EmployeeId; item.EmployeeEmail = employee.Email; item.EmployeePhoneNumber = employee.PhoneNumber;
            item.EmployeeDepartment = employee.Department; item.EmployeeJobRole = employee.JobRole; item.EmployeeProfileUrl = employee.ProfileUrl;
            item.EmployeeDepartmentName = departmentNames.GetValueOrDefault(employee.Department, employee.Department);
            item.EmployeeEmploymentStatus = employee.EmploymentStatus;
        }
        foreach (var item in items.Where(x => x.Evidence.Any(e => string.IsNullOrWhiteSpace(e.EvidenceId))))
        {
            foreach (var evidence in item.Evidence.Where(e => string.IsNullOrWhiteSpace(e.EvidenceId)))
                evidence.EvidenceId = Guid.NewGuid().ToString("N");
            item.Version++;
            item.UpdatedDate = DateTime.UtcNow;
            await _cases.Update(Builders<EmployeeLifecycleCase>.Filter.Where(x => x.CompanyId == companyId && x.CaseId == item.CaseId && x.Version == item.Version - 1), item);
        }
        foreach (var item in items)
            if (employeeById.TryGetValue(item.CaseOwnerUserId ?? string.Empty, out var owner))
                item.CaseOwnerName = $"{owner.FirstName} {owner.LastName}".Trim();
        var today = DateTime.UtcNow.Date;
        foreach (var item in items)
            item.IsReturnOverdue = item.Status is not ("Completed" or "Cancelled") && item.ExpectedReturnDate.HasValue && item.ExpectedReturnDate.Value.Date < today;
        if (!string.IsNullOrWhiteSpace(status)) items = items.Where(x => string.Equals(x.Status, status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (overdueOnly) items = items.Where(x => x.IsReturnOverdue).ToList();
        return items;
    }

    public async Task<EmployeeLifecycleCaseSummary> GetSummaryAsync()
    {
        var items = await GetCasesAsync();
        var today = DateTime.UtcNow.Date;
        return new EmployeeLifecycleCaseSummary {
            Total = items.Count,
            Active = items.Count(x => x.Status is "Open" or "UnderReview" or "Active" or "Extended"),
            PendingDocuments = items.Count(x => !x.DocumentsVerified && x.Status is not ("Completed" or "Cancelled")),
            OverdueReturns = items.Count(x => x.IsReturnOverdue),
            UpcomingReturns = items.Count(x => x.ExpectedReturnDate.HasValue && x.ExpectedReturnDate.Value.Date >= today && x.ExpectedReturnDate.Value.Date <= today.AddDays(30) && x.Status is not ("Completed" or "Cancelled")),
            ByStatus = items.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase)
        };
    }

    public async Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> CreateFromApprovedLeaveAsync(CreateEmployeeLifecycleCaseRequest request)
    {
        if (!await CanManage()) return (false, "LIFECYCLE_CASE_FORBIDDEN", null);
        return await CreateFromApprovedLeaveCoreAsync(request, CurrentContext.UserId(_http));
    }

    public Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> CreateFromApprovedLeaveSystemAsync(CreateEmployeeLifecycleCaseRequest request, string actorUserId)
        => CreateFromApprovedLeaveCoreAsync(request, actorUserId);

    public async Task<(bool Success, string? Error)> CancelFromLeaveRequestSystemAsync(string leaveRequestId, string actorUserId, string reason)
    {
        var companyId = CurrentContext.CompanyId(_http);
        var item = await _cases.FirstOrDefault(x => x.CompanyId == companyId && x.LeaveRequestId == leaveRequestId && x.Status != "Completed" && x.Status != "Cancelled");
        if (item is null) return (true, null);
        var previous = item.Status; item.Status = "Cancelled"; item.UpdatedBy = actorUserId; item.UpdatedDate = DateTime.UtcNow;
        item.Events.Add(new EmployeeLifecycleCaseEvent { EventType = "CancelledByLeaveDecision", FromStatus = previous, ToStatus = "Cancelled", ActorUserId = actorUserId, Comment = reason });
        var saved = await _cases.Update(Builders<EmployeeLifecycleCase>.Filter.Where(x => x.CompanyId == companyId && x.CaseId == item.CaseId && x.Version == item.Version), item);
        if (!saved.Success) return (false, "LIFECYCLE_CASE_VERSION_CONFLICT");
        var employee = await _employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == item.EmployeeUserId && !x.IsDeleted);
        if (employee?.Lifecycle is not null)
        {
            if (!TryRestoreCaseLifecycleState(employee.Lifecycle, item, actorUserId, DateTime.UtcNow, "LEAVE_REVERSED", reason, out var changed, out var changeCount, out var error)) return (false, error);
            if (changed)
            {
                employee.UpdatedBy = actorUserId; employee.UpdatedDate = DateTime.UtcNow;
                if (!(await _employees.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted && x.Lifecycle.Version == employee.Lifecycle.Version - changeCount), employee)).Success) return (false, "LIFECYCLE_EMPLOYEE_VERSION_CONFLICT");
            }
        }
        if (_lifecycleAttendanceReconciliation is not null)
            await _lifecycleAttendanceReconciliation.ReverseCaseAttendanceAsync(companyId, item.CaseId, actorUserId);
        return (true, null);
    }

    private async Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> CreateFromApprovedLeaveCoreAsync(CreateEmployeeLifecycleCaseRequest request, string actor)
    {
        var companyId = CurrentContext.CompanyId(_http);
        var leave = await _leaveRequests.FirstOrDefault(x => x.CompanyId == companyId && x.LeaveRequestId == request.LeaveRequestId && x.Status == EnumsHelper.LeaveRequestStatus.Accepted);
        if (leave is null) return (false, "APPROVED_LEAVE_REQUEST_REQUIRED", null);
        var policy = await _lifecyclePolicies.GetPoliciesAsync(companyId);
        var lifecycle = policy.FirstOrDefault(x => x.LeavePolicyId == leave.LeavePolicyId && LeaveDrivenStatuses.Contains(x.EventType));
        if (lifecycle is null) return (false, "LIFECYCLE_POLICY_LINK_NOT_FOUND", null);
        var existing = await _cases.FirstOrDefault(x => x.CompanyId == companyId && x.LeaveRequestId == leave.LeaveRequestId && x.Status != "Cancelled");
        if (existing is not null) return (true, null, existing);
        var employee = await _employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == leave.EmployeeId && !x.IsDeleted);
        if (employee is null) return (false, "LIFECYCLE_EMPLOYEE_NOT_FOUND", null);
        var state = employee.Lifecycle ?? new EmployeeLifecycleState();
        var accountToApply = NormalizeTemporaryAccountStatus(lifecycle.DefaultAccountStatus);
        var payrollToApply = NormalizeTemporaryPayrollStatus(lifecycle.DefaultPayrollStatus);
        var item = new EmployeeLifecycleCase {
            CompanyId = companyId, CreatedBy = actor, UpdatedBy = actor, EmployeeUserId = leave.EmployeeId,
            LifecycleStatus = lifecycle.EventType, LeaveRequestId = leave.LeaveRequestId, LeavePolicyId = leave.LeavePolicyId,
            EffectiveFromUtc = leave.StartDate, ExpectedReturnDate = leave.EndDate, NextReviewDate = request.NextReviewDate,
            LeaveTotalDays = leave.TotalDays, LeaveReconciliationStatus = leave.ReconciliationStatus,
            CaseOwnerUserId = string.IsNullOrWhiteSpace(request.CaseOwnerUserId) ? actor : request.CaseOwnerUserId.Trim(), Status = "Open",
            PreviousAccountStatus = !string.IsNullOrWhiteSpace(accountToApply) && !string.Equals(state.AccountStatus, accountToApply, StringComparison.Ordinal) ? state.AccountStatus : null,
            AppliedAccountStatus = !string.IsNullOrWhiteSpace(accountToApply) && !string.Equals(state.AccountStatus, accountToApply, StringComparison.Ordinal) ? accountToApply : null,
            PreviousPayrollStatus = !string.IsNullOrWhiteSpace(payrollToApply) && !string.Equals(state.PayrollStatus, payrollToApply, StringComparison.Ordinal) ? state.PayrollStatus : null,
            AppliedPayrollStatus = !string.IsNullOrWhiteSpace(payrollToApply) && !string.Equals(state.PayrollStatus, payrollToApply, StringComparison.Ordinal) ? payrollToApply : null,
            Events = [new EmployeeLifecycleCaseEvent { EventType = "Created", ToStatus = "Open", ActorUserId = actor }]
        };
        var result = await _cases.AddOne(item);
        if (!result.Success)
        {
            var raced = await _cases.FirstOrDefault(x => x.CompanyId == companyId && x.LeaveRequestId == leave.LeaveRequestId && x.Status != "Cancelled");
            return raced is not null ? (true, null, raced) : (false, "LIFECYCLE_CASE_CREATE_FAILED", null);
        }
        var stateVersion = state.Version;
        var changed = false;
        if (!string.Equals(state.AvailabilityStatus, lifecycle.EventType, StringComparison.Ordinal))
        {
            if (!EmployeeLifecycleTransitionService.TryChangeAvailability(state, lifecycle.EventType, actor, state.Version, leave.StartDate, leave.EndDate, "APPROVED_LEAVE_REQUEST", leave.Reason, out var error))
                return await CompensateCreatedCaseAsync(item, actor, error);
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(item.AppliedAccountStatus))
        {
            if (!EmployeeLifecycleTransitionService.TryChangeAccountStatus(state, item.AppliedAccountStatus, actor, state.Version, leave.StartDate, "APPROVED_LEAVE_REQUEST", out var error))
                return await CompensateCreatedCaseAsync(item, actor, error);
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(item.AppliedPayrollStatus))
        {
            if (!EmployeeLifecycleTransitionService.TryChangePayrollStatus(state, item.AppliedPayrollStatus, actor, state.Version, leave.StartDate, "APPROVED_LEAVE_REQUEST", out var error))
                return await CompensateCreatedCaseAsync(item, actor, error);
            changed = true;
        }
        if (changed)
        {
            employee.Lifecycle = state; employee.UpdatedBy = actor; employee.UpdatedDate = DateTime.UtcNow;
            var employeeUpdate = await _employees.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == leave.EmployeeId && !x.IsDeleted && x.Lifecycle.Version == stateVersion), employee);
            if (!employeeUpdate.Success) return await CompensateCreatedCaseAsync(item, actor, "LIFECYCLE_EMPLOYEE_VERSION_CONFLICT");
        }
        if (_lifecycleAttendanceReconciliation is not null)
            await _lifecycleAttendanceReconciliation.ReconcileCaseAttendanceAsync(companyId, item.CaseId, actor);
        return (true, null, item);
    }

    public async Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> UpdateAsync(UpdateEmployeeLifecycleCaseRequest request)
    {
        if (!await CanManage()) return (false, "LIFECYCLE_CASE_FORBIDDEN", null);
        var companyId = CurrentContext.CompanyId(_http); var actor = CurrentContext.UserId(_http);
        var item = await _cases.FirstOrDefault(x => x.CompanyId == companyId && x.CaseId == request.CaseId);
        if (item is null) return (false, "LIFECYCLE_CASE_NOT_FOUND", null);
        if (item.Status == "Cancelled") return (false, "LIFECYCLE_CASE_READ_ONLY", null);
        var previous = item.Status; var next = string.IsNullOrWhiteSpace(request.Status) ? previous : request.Status.Trim();
        if (next is not ("Open" or "UnderReview" or "Active" or "Extended" or "Completed" or "Cancelled")) return (false, "LIFECYCLE_CASE_STATUS_INVALID", null);
        var linkedPolicy = (await _lifecyclePolicies.GetPoliciesAsync(companyId)).FirstOrDefault(x => x.EventType == item.LifecycleStatus && x.LeavePolicyId == item.LeavePolicyId);
        if (next == "Active" && linkedPolicy?.RequiresDocument == true && !item.DocumentsVerified && request.DocumentsVerified != true)
            return (false, "LIFECYCLE_DOCUMENT_VERIFICATION_REQUIRED", null);
        if (next == "Completed" && !(request.ActualReturnDate ?? item.ActualReturnDate).HasValue)
            return (false, "LIFECYCLE_ACTUAL_RETURN_DATE_REQUIRED", null);
        if (next is "Completed" or "Cancelled" && (!string.IsNullOrWhiteSpace(request.AccountStatus) || !string.IsNullOrWhiteSpace(request.PayrollStatus)))
            return (false, "LIFECYCLE_CASE_CONTROL_TERMINAL_STATE_FORBIDDEN", null);
        var requestedAccountStatus = NormalizeTemporaryAccountStatus(request.AccountStatus);
        var requestedPayrollStatus = NormalizeTemporaryPayrollStatus(request.PayrollStatus);
        if (!string.IsNullOrWhiteSpace(request.AccountStatus) && requestedAccountStatus is null)
            return (false, "LIFECYCLE_ACCOUNT_STATUS_INVALID", null);
        if (!string.IsNullOrWhiteSpace(request.PayrollStatus) && requestedPayrollStatus is null)
            return (false, "LIFECYCLE_PAYROLL_STATUS_INVALID", null);
        var previousOwner = item.CaseOwnerUserId;
        if (!string.IsNullOrWhiteSpace(request.CaseOwnerUserId)) item.CaseOwnerUserId = request.CaseOwnerUserId.Trim();
        item.Status = next; item.ActualReturnDate = request.ActualReturnDate ?? item.ActualReturnDate;
        item.NextReviewDate = request.NextReviewDate ?? item.NextReviewDate;
        if (next is "Extended")
        {
            if (!request.ExpectedReturnDate.HasValue || request.ExpectedReturnDate.Value.Date <= DateTime.UtcNow.Date)
                return (false, "LIFECYCLE_EXTENSION_RETURN_DATE_INVALID", null);
            if (item.ExpectedReturnDate.HasValue && request.ExpectedReturnDate.Value.Date <= item.ExpectedReturnDate.Value.Date)
                return (false, "LIFECYCLE_EXTENSION_MUST_MOVE_RETURN_DATE_FORWARD", null);
            item.ExpectedReturnDate = request.ExpectedReturnDate.Value;
        }
        if (request.DocumentsVerified.HasValue) { item.DocumentsVerified = request.DocumentsVerified.Value; if (item.DocumentsVerified) { item.DocumentsVerifiedAtUtc = DateTime.UtcNow; item.DocumentsVerifiedBy = actor; } }
        item.UpdatedBy = actor; item.UpdatedDate = DateTime.UtcNow;
        item.Events.Add(new EmployeeLifecycleCaseEvent { EventType = "Updated", FromStatus = previous, ToStatus = next, ActorUserId = actor, Comment = request.Comment });
        if (!string.Equals(previousOwner, item.CaseOwnerUserId, StringComparison.Ordinal))
            item.Events.Add(new EmployeeLifecycleCaseEvent { EventType = "OwnerReassigned", ToStatus = item.Status, ActorUserId = actor, Comment = $"Owner changed to {item.CaseOwnerUserId}" });
        var result = await _cases.Update(Builders<EmployeeLifecycleCase>.Filter.Where(x => x.CompanyId == companyId && x.CaseId == item.CaseId && x.Version == item.Version), item);
        if (!result.Success) return (false, "LIFECYCLE_CASE_VERSION_CONFLICT", null);
        if (requestedAccountStatus is not null || requestedPayrollStatus is not null)
        {
            var controlResult = await ApplyCaseControlsAsync(item, requestedAccountStatus, requestedPayrollStatus, actor, request.Comment);
            if (!controlResult.Success) return (false, controlResult.Error, item);
        }
        if (next is "Completed" or "Cancelled")
        {
            var employee = await _employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == item.EmployeeUserId && !x.IsDeleted);
            if (employee is not null && employee.Lifecycle is not null)
            {
                if (!TryRestoreCaseLifecycleState(employee.Lifecycle, item, actor, DateTime.UtcNow, next == "Completed" ? "RETURN_TO_WORK" : "CASE_CANCELLED", request.Comment, out var changed, out var changeCount, out var error))
                    return (false, error, item);
                if (changed)
                {
                    var beforeRestoreVersion = employee.Lifecycle.Version;
                    employee.UpdatedBy = actor; employee.UpdatedDate = DateTime.UtcNow;
                    var employeeUpdate = await _employees.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == item.EmployeeUserId && !x.IsDeleted && x.Lifecycle.Version == beforeRestoreVersion - changeCount), employee);
                    if (!employeeUpdate.Success) return (false, "LIFECYCLE_EMPLOYEE_VERSION_CONFLICT", item);
                }
            }
        }
        else if (next is "Extended" && request.ExpectedReturnDate.HasValue)
        {
            var employee = await _employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == item.EmployeeUserId && !x.IsDeleted);
            if (employee?.Lifecycle is not null && employee.Lifecycle.AvailabilityStatus == item.LifecycleStatus)
            {
                employee.Lifecycle.AvailabilityExpectedReturnDate = item.ExpectedReturnDate;
                employee.Lifecycle.Version++;
                employee.UpdatedBy = actor; employee.UpdatedDate = DateTime.UtcNow;
                employee.Lifecycle.History.Add(new EmployeeLifecycleStatusHistory { Dimension = nameof(EmployeeLifecycleState.AvailabilityStatus), PreviousValue = item.LifecycleStatus, NewValue = item.LifecycleStatus, ReasonCode = "CASE_EXTENDED", Reason = request.Comment, EffectiveFromUtc = DateTime.UtcNow, ExpectedReturnDate = item.ExpectedReturnDate, ChangedBy = actor });
                var employeeUpdate = await _employees.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == item.EmployeeUserId && !x.IsDeleted && x.Lifecycle.Version == employee.Lifecycle.Version - 1), employee);
                if (!employeeUpdate.Success) return (false, "LIFECYCLE_EMPLOYEE_VERSION_CONFLICT", item);
            }
        }
        if (_lifecycleAttendanceReconciliation is not null)
        {
            if (next == "Completed" && (request.ActualReturnDate ?? item.ActualReturnDate).HasValue)
            {
                await _lifecycleAttendanceReconciliation.HandleReturnToWorkAsync(companyId, item.CaseId, (request.ActualReturnDate ?? item.ActualReturnDate)!.Value, actor);
            }
            else if (next == "Cancelled")
            {
                await _lifecycleAttendanceReconciliation.ReverseCaseAttendanceAsync(companyId, item.CaseId, actor);
            }
            else if (next is "Active" or "Extended" or "Open")
            {
                await _lifecycleAttendanceReconciliation.ReconcileCaseAttendanceAsync(companyId, item.CaseId, actor);
            }
        }
        return (true, null, item);
    }

    public async Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> UploadEvidenceAsync(string caseId, IFormFile file)
    {
        if (!await CanManage()) return (false, "LIFECYCLE_CASE_FORBIDDEN", null);
        var allowedTypes = new[] { "application/pdf", "image/png", "image/jpeg" };
        if (file is null || file.Length == 0 || file.Length > 10 * 1024 * 1024 || !allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)) return (false, "LIFECYCLE_EVIDENCE_INVALID", null);
        var companyId = CurrentContext.CompanyId(_http); var actor = CurrentContext.UserId(_http);
        var item = await _cases.FirstOrDefault(x => x.CompanyId == companyId && x.CaseId == caseId);
        if (item is null) return (false, "LIFECYCLE_CASE_NOT_FOUND", null);
        if (item.Status == "Cancelled") return (false, "LIFECYCLE_CASE_READ_ONLY", null);
        var root = Path.Combine(Directory.GetCurrentDirectory(), "PrivateLifecycleEvidence", companyId, caseId);
        Directory.CreateDirectory(root);
        var storageKey = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(root, storageKey);
        try
        {
            await using (var output = File.Create(path)) await file.CopyToAsync(output);
            await using var input = File.OpenRead(path);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(input));
            var evidence = new EmployeeLifecycleCaseEvidence { EvidenceId = Guid.NewGuid().ToString("N"), FileName = Path.GetFileName(file.FileName), ContentType = file.ContentType, SizeBytes = file.Length, StorageKey = Path.Combine(companyId, caseId, storageKey), Sha256 = hash, UploadedBy = actor };
            item.Evidence.Add(evidence); item.DocumentsVerified = false; item.UpdatedBy = actor; item.UpdatedDate = DateTime.UtcNow;
            item.Events.Add(new EmployeeLifecycleCaseEvent { EventType = "EvidenceUploaded", ToStatus = item.Status, ActorUserId = actor, Comment = evidence.FileName });
            var result = await _cases.Update(Builders<EmployeeLifecycleCase>.Filter.Where(x => x.CompanyId == companyId && x.CaseId == caseId && x.Version == item.Version), item);
            if (result.Success) return (true, null, item);
            File.Delete(path);
            return (false, "LIFECYCLE_CASE_VERSION_CONFLICT", null);
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            return (false, "LIFECYCLE_EVIDENCE_SAVE_FAILED", null);
        }
    }

    public async Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> VerifyEvidenceAsync(VerifyEmployeeLifecycleCaseEvidenceRequest request)
    {
        if (!await CanManage()) return (false, "LIFECYCLE_CASE_FORBIDDEN", null);
        var companyId = CurrentContext.CompanyId(_http); var actor = CurrentContext.UserId(_http);
        var item = await _cases.FirstOrDefault(x => x.CompanyId == companyId && x.CaseId == request.CaseId);
        var evidence = item?.Evidence.FirstOrDefault(x => x.EvidenceId == request.EvidenceId);
        if (item is null || evidence is null) return (false, "LIFECYCLE_EVIDENCE_NOT_FOUND", null);
        if (item.Status == "Cancelled") return (false, "LIFECYCLE_CASE_READ_ONLY", null);
        evidence.Status = request.Verified ? "Verified" : "Rejected"; evidence.VerifiedBy = actor; evidence.VerifiedAtUtc = DateTime.UtcNow;
        item.DocumentsVerified = item.Evidence.Count > 0 && item.Evidence.All(x => x.Status == "Verified"); item.DocumentsVerifiedBy = actor; item.DocumentsVerifiedAtUtc = DateTime.UtcNow; item.UpdatedBy = actor; item.UpdatedDate = DateTime.UtcNow;
        item.Events.Add(new EmployeeLifecycleCaseEvent { EventType = request.Verified ? "EvidenceVerified" : "EvidenceRejected", ToStatus = item.Status, ActorUserId = actor, Comment = evidence.FileName });
        var result = await _cases.Update(Builders<EmployeeLifecycleCase>.Filter.Where(x => x.CompanyId == companyId && x.CaseId == item.CaseId && x.Version == item.Version), item);
        return result.Success ? (true, null, item) : (false, "LIFECYCLE_CASE_VERSION_CONFLICT", null);
    }

    public async Task<(bool Success, string? Error, EmployeeLifecycleCaseEvidence? Evidence, byte[]? Content)> GetEvidenceAsync(string caseId, string evidenceId)
    {
        if (!await CanManage()) return (false, "LIFECYCLE_CASE_FORBIDDEN", null, null);
        var companyId = CurrentContext.CompanyId(_http);
        var item = await _cases.FirstOrDefault(x => x.CompanyId == companyId && x.CaseId == caseId);
        var evidence = item?.Evidence.FirstOrDefault(x => x.EvidenceId == evidenceId);
        if (item is null || evidence is null) return (false, "LIFECYCLE_EVIDENCE_NOT_FOUND", null, null);
        var root = Path.Combine(Directory.GetCurrentDirectory(), "PrivateLifecycleEvidence", companyId, caseId);
        var path = Path.Combine(root, Path.GetFileName(evidence.StorageKey));
        if (!File.Exists(path)) return (false, "LIFECYCLE_EVIDENCE_FILE_NOT_FOUND", null, null);
        return (true, null, evidence, await File.ReadAllBytesAsync(path));
    }

    private Task<bool> CanManage() => _roles.VerifyUserAccess(AppModule.Offboarding, [Permission.CaseManage, Permission.Edit], CurrentContext.UserId(_http), CurrentContext.CompanyId(_http));

    private async Task<(bool Success, string? Error, EmployeeLifecycleCase? Case)> CompensateCreatedCaseAsync(EmployeeLifecycleCase item, string actor, string error)
    {
        item.Status = "Cancelled"; item.UpdatedBy = actor; item.UpdatedDate = DateTime.UtcNow;
        item.Events.Add(new EmployeeLifecycleCaseEvent { EventType = "CreationCompensated", FromStatus = "Open", ToStatus = "Cancelled", ActorUserId = actor, Comment = error });
        var compensated = await _cases.Update(Builders<EmployeeLifecycleCase>.Filter.Where(x => x.CompanyId == item.CompanyId && x.CaseId == item.CaseId && x.Version == item.Version), item);
        return (false, compensated.Success ? error : "LIFECYCLE_CASE_COMPENSATION_FAILED", item);
    }

    private static string? NormalizeTemporaryAccountStatus(string? value) => value?.Trim() is "Active" or "TemporarilyDisabled" ? value.Trim() : null;
    private static string? NormalizeTemporaryPayrollStatus(string? value) => value?.Trim() is "Active" or "Hold" or "Paused" ? value.Trim() : null;

    private async Task<(bool Success, string? Error)> ApplyCaseControlsAsync(EmployeeLifecycleCase item, string? accountStatus, string? payrollStatus, string actor, string? comment)
    {
        var companyId = CurrentContext.CompanyId(_http);
        var employee = await _employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == item.EmployeeUserId && !x.IsDeleted);
        if (employee?.Lifecycle is null) return (false, "LIFECYCLE_EMPLOYEE_NOT_FOUND");
        var state = employee.Lifecycle;
        var originalVersion = state.Version;
        var changed = false;

        if (accountStatus is not null && !string.Equals(state.AccountStatus, accountStatus, StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(item.AppliedAccountStatus) && !string.Equals(state.AccountStatus, item.AppliedAccountStatus, StringComparison.Ordinal))
                return (false, "LIFECYCLE_CASE_ACCOUNT_OVERRIDE_CONFLICT");
            item.PreviousAccountStatus ??= state.AccountStatus;
            if (!EmployeeLifecycleTransitionService.TryChangeAccountStatus(state, accountStatus, actor, state.Version, DateTime.UtcNow, "CASE_CONTROL_OVERRIDE", out var error)) return (false, error);
            item.AppliedAccountStatus = accountStatus;
            item.Events.Add(new EmployeeLifecycleCaseEvent { EventType = "AccountControlUpdated", ToStatus = item.Status, ActorUserId = actor, Comment = comment ?? accountStatus });
            changed = true;
        }
        if (payrollStatus is not null && !string.Equals(state.PayrollStatus, payrollStatus, StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(item.AppliedPayrollStatus) && !string.Equals(state.PayrollStatus, item.AppliedPayrollStatus, StringComparison.Ordinal))
                return (false, "LIFECYCLE_CASE_PAYROLL_OVERRIDE_CONFLICT");
            item.PreviousPayrollStatus ??= state.PayrollStatus;
            if (!EmployeeLifecycleTransitionService.TryChangePayrollStatus(state, payrollStatus, actor, state.Version, DateTime.UtcNow, "CASE_CONTROL_OVERRIDE", out var error)) return (false, error);
            item.AppliedPayrollStatus = payrollStatus;
            item.Events.Add(new EmployeeLifecycleCaseEvent { EventType = "PayrollControlUpdated", ToStatus = item.Status, ActorUserId = actor, Comment = comment ?? payrollStatus });
            changed = true;
        }
        if (!changed) return (true, null);
        employee.UpdatedBy = actor; employee.UpdatedDate = DateTime.UtcNow;
        if (!(await _employees.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted && x.Lifecycle.Version == originalVersion), employee)).Success)
            return (false, "LIFECYCLE_EMPLOYEE_VERSION_CONFLICT");
        item.UpdatedBy = actor; item.UpdatedDate = DateTime.UtcNow;
        var caseSave = await _cases.Update(Builders<EmployeeLifecycleCase>.Filter.Where(x => x.CompanyId == companyId && x.CaseId == item.CaseId && x.Version == item.Version), item);
        return caseSave.Success ? (true, null) : (false, "LIFECYCLE_CASE_CONTROL_RECONCILIATION_REQUIRED");
    }

    private static bool TryRestoreCaseLifecycleState(EmployeeLifecycleState state, EmployeeLifecycleCase item, string actor, DateTime effectiveAt, string reasonCode, string? reason, out bool changed, out int changeCount, out string error)
    {
        changed = false; changeCount = 0; error = string.Empty;
        if (state.IsFormerEmployee())
        {
            error = "FORMER_EMPLOYEE_LIFECYCLE_RESTORE_FORBIDDEN";
            return false;
        }
        if (state.AvailabilityStatus == item.LifecycleStatus)
        {
            if (!EmployeeLifecycleTransitionService.TryChangeAvailability(state, EmployeeLifecycleStatuses.Working, actor, state.Version, effectiveAt, null, reasonCode, reason, out error)) return false;
            changed = true;
            changeCount++;
        }
        if (!string.IsNullOrWhiteSpace(item.AppliedAccountStatus) && state.AccountStatus == item.AppliedAccountStatus && !string.IsNullOrWhiteSpace(item.PreviousAccountStatus))
        {
            if (!EmployeeLifecycleTransitionService.TryChangeAccountStatus(state, item.PreviousAccountStatus, actor, state.Version, effectiveAt, reasonCode, out error)) return false;
            changed = true;
            changeCount++;
        }
        if (!string.IsNullOrWhiteSpace(item.AppliedPayrollStatus) && state.PayrollStatus == item.AppliedPayrollStatus && !string.IsNullOrWhiteSpace(item.PreviousPayrollStatus))
        {
            if (!EmployeeLifecycleTransitionService.TryChangePayrollStatus(state, item.PreviousPayrollStatus, actor, state.Version, effectiveAt, reasonCode, out error)) return false;
            changed = true;
            changeCount++;
        }
        return true;
    }

    public async Task<Result> ReconcileAttendanceAsync(string caseId)
    {
        if (!await CanManage()) return new Result { Success = false, Message = "LIFECYCLE_CASE_FORBIDDEN" };
        if (_lifecycleAttendanceReconciliation is null) return new Result { Success = false, Message = "LIFECYCLE_ATTENDANCE_RECONCILIATION_UNAVAILABLE" };
        var companyId = CurrentContext.CompanyId(_http);
        var actor = CurrentContext.UserId(_http);
        return await _lifecycleAttendanceReconciliation.ReconcileCaseAttendanceAsync(companyId, caseId, actor);
    }
}
