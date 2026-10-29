using System.Linq.Expressions;
using System.Net;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave.LeaveRequest;
using Codeji.CMS.DTO.LeaveManagement;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using Codeji.CMS.DTO.LeaveManagement.LeavePolicy;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using LinqKit;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Security.Cryptography;

namespace Codeji.CMS.Services.LeaveManagement;

public class LeaveManagementService : ILeaveManagementService
{
    private readonly IMongoDbRepository<Roles> _roleRepository;
    private readonly IMongoDbRepository<EmpUser> _employeeRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMongoDbRepository<LeaveRequest> _leave;
    private readonly INotificationService _notificationService;
    private readonly IMongoDbRepository<Notifications> _notificationsRepo;
    private readonly IMongoDbRepository<UserNotifications> _userNotificationsRepo;
    private readonly IMongoDbRepository<JobTitles> _jobTitleRepo;
    private readonly IMapper _mapper;
    private readonly IEmployeeService _employeeService;
    private readonly IMiddlewareService _middlewareService;
    private readonly IMongoDbRepository<LeavePolicy> _leavePolicyRepo;
    private readonly IMongoDbRepository<EmployeeLeaveBalance> _employeeLeaveBalanceRepo;
    private readonly ICompanyWorkingCalendarService _workingCalendar;
    private readonly IMongoDbRepository<AttendanceStatusSetting> _attendanceStatusRepo;
    private readonly IMongoDbRepository<WorkFromHomePolicy> _workFromHomePolicies;
    private readonly IMongoDbRepository<WorkFromHomeRequest> _workFromHomeRequests;
    private readonly ILeaveAttendanceReconciliationService _leaveAttendanceReconciliation;
    private readonly IMongoClient _mongoClient;
    private readonly IEmployeeLifecycleCaseService? _lifecycleCases;
    private readonly IMongoDbRepository<EmployeeLifecyclePolicy>? _lifecyclePolicyRepository;
    private readonly IEmployeeLifecyclePolicyService? _lifecyclePolicyService;
    private readonly IRoleService? _roleService;
    public LeaveManagementService(
    IMongoDbRepository<EmpUser> employeeRepository,
    IMongoDbRepository<LeaveRequest> leave,
    IHttpContextAccessor httpContextAccessor, IMapper mapper,
    INotificationService notificationService,
    IMongoDbRepository<Notifications> notificationsRepo,
    IMongoDbRepository<UserNotifications> userNotificationsRepo,
    IMongoDbRepository<Roles> roleRepository,
    IMongoDbRepository<JobTitles> jobTitleRepo,
    IEmployeeService employeeService,
    IMiddlewareService middlewareService,
    IMongoDbRepository<LeavePolicy> leavePolicyRepo,
    IMongoDbRepository<EmployeeLeaveBalance> employeeLeaveBalanceRepo,
    ICompanyWorkingCalendarService workingCalendar,
    IMongoDbRepository<AttendanceStatusSetting> attendanceStatusRepo,
    ILeaveAttendanceReconciliationService leaveAttendanceReconciliation,
    IMongoDbRepository<WorkFromHomePolicy> workFromHomePolicies,
    IMongoDbRepository<WorkFromHomeRequest> workFromHomeRequests,
    IMongoClient mongoClient,
    IEmployeeLifecycleCaseService? lifecycleCases = null,
    IMongoDbRepository<EmployeeLifecyclePolicy>? lifecyclePolicyRepository = null,
    IEmployeeLifecyclePolicyService? lifecyclePolicyService = null,
    IRoleService? roleService = null
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _employeeRepository = employeeRepository;
        _leave = leave;
        _mapper = mapper;
        _notificationService = notificationService;
        _notificationsRepo = notificationsRepo;
        _userNotificationsRepo = userNotificationsRepo;
        _roleRepository = roleRepository;
        _employeeService = employeeService;
        _jobTitleRepo = jobTitleRepo;
        _middlewareService = middlewareService;
        _leavePolicyRepo = leavePolicyRepo;
        _employeeLeaveBalanceRepo = employeeLeaveBalanceRepo;
        _workingCalendar = workingCalendar;
        _attendanceStatusRepo = attendanceStatusRepo;
        _leaveAttendanceReconciliation = leaveAttendanceReconciliation;
        _workFromHomePolicies = workFromHomePolicies;
        _workFromHomeRequests = workFromHomeRequests;
        _mongoClient = mongoClient;
        _lifecycleCases = lifecycleCases;
        _lifecyclePolicyRepository = lifecyclePolicyRepository;
        _lifecyclePolicyService = lifecyclePolicyService;
        _roleService = roleService;
    }

    // updated services
    public async Task<Result> CreateNewLeavePolicy(LeavePolicyRequest leavePolicyDto, string company_id)
    {
        Result result = new();
        NormalizeUnpaidLeavePolicy(leavePolicyDto);
        NormalizeLifecycleLeavePolicy(leavePolicyDto);
        NormalizeLeaveAttendanceMappings(leavePolicyDto);
        if (!await ValidatePolicyShape(company_id, leavePolicyDto.PolicyType, leavePolicyDto.FullDayAttendanceStatusCode, leavePolicyDto.HalfDayAttendanceStatusCode, leavePolicyDto.HalfDayAllowed, leavePolicyDto.WorkFromHome))
            return new Result { Success = false, Message = "LEAVE_POLICY_CONFIGURATION_INVALID: The policy type and attendance configuration are not valid for this company." };
        if (leavePolicyDto.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome && _roleService is not null && !await _roleService.IsModuleAccessibleForCompany(AppModule.WorkFromHome, company_id))
            return new Result { Success = false, Message = "WFH_MODULE_DISABLED: Work from home module is not enabled for this company." };
        if (leavePolicyDto.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome &&
            await _workFromHomePolicies.Exist(x => x.CompanyId == company_id && !string.IsNullOrEmpty(x.LeavePolicyId)))
            return new Result { Success = false, Message = "WFH_POLICY_ALREADY_CONFIGURED: This company already has a typed WFH policy. Update that policy instead." };
        // check if policy already exist;
        leavePolicyDto.Name = leavePolicyDto.Name.Trim();
        leavePolicyDto.Code = leavePolicyDto.Code.Trim().ToUpperInvariant();
        // WFH is attendance work, not unpaid leave. Keep the policy flag aligned
        // with the WFH attendance status so payroll and the policy UI agree.
        if (leavePolicyDto.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
        {
            leavePolicyDto.Paid = true;
            leavePolicyDto.MinNoticeDays = 0;
        }
        // A leave policy may be created before its attendance status has been configured.
        // The mapping is optional and is checked again when a leave request is approved,
        // which prevents an invalid mapping from creating attendance while allowing HR to
        // set up the policy independently of Attendance Settings.
        leavePolicyDto.AttendanceStatusCode = string.IsNullOrWhiteSpace(leavePolicyDto.AttendanceStatusCode)
            ? null
            : leavePolicyDto.AttendanceStatusCode.Trim().ToUpperInvariant();
        var exist = await _leavePolicyRepo.Exist(lp => lp.CompanyId == company_id &&
            (lp.Name.Equals(leavePolicyDto.Name, StringComparison.CurrentCultureIgnoreCase) || lp.Code == leavePolicyDto.Code));
        if (exist)
        {
            result.StatusCode = CustomStatusCode.LeavePolicyAlreadyExist;
            return result;
        }
        LeavePolicy leavePolicy = _mapper.Map<LeavePolicy>(leavePolicyDto);
        leavePolicy.Id = Guid.NewGuid().ToString();
        leavePolicy.CompanyId = company_id;
        leavePolicy.NormalizedName = leavePolicyDto.Name.ToUpperInvariant();
        leavePolicy.NormalizedCode = leavePolicyDto.Code;
        leavePolicy.AttendanceStatusCode = leavePolicyDto.AttendanceStatusCode;
        leavePolicy.FullDayAttendanceStatusCode = leavePolicyDto.FullDayAttendanceStatusCode;
        leavePolicy.HalfDayAttendanceStatusCode = leavePolicyDto.HalfDayAttendanceStatusCode;

        // insert new leave policy
        result = await _leavePolicyRepo.AddOne(leavePolicy);
        if (result.Success && leavePolicyDto.Lifecycle?.Enabled == true)
        {
            var lifecycleResult = await SynchronizeLifecyclePolicy(company_id, leavePolicy, leavePolicyDto.Lifecycle);
            if (!lifecycleResult.Success)
                return new Result { Success = false, Message = lifecycleResult.Error };
        }
        if (result.Success && leavePolicy.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
        {
            var sync = await SyncWorkFromHomePolicy(company_id, leavePolicy, leavePolicyDto.WorkFromHome!);
            if (!sync.Success) return sync;
        }

        // WFH is operational and does not own a leave balance. Normal leave policies
        // must synchronise employee eligibility immediately, including "All Employees".
        if (leavePolicy.PolicyType == EnumsHelper.LeavePolicyType.Leave && result.Success)
            return await SynchronizeLeavePolicyEligibility(company_id, leavePolicy, leavePolicyDto.ApplicableTo);
        return result;
    }

    public async Task<Result<UpdateLeavePolicyRequest>> UpdateLeavePolicy(UpdateLeavePolicyRequest model)
    {
        Result<UpdateLeavePolicyRequest> result = new() { Success = false };
        // check if leave policy exits or not;
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        Expression<Func<LeavePolicy, bool>> expression = lp => lp.CompanyId == companyId && lp.Id == model.Id;
        var leavePolicy = await _leavePolicyRepo.FirstOrDefault(expression);
        if (leavePolicy == null) return result;

        if (leavePolicy.PolicyType != model.PolicyType)
        {
            var hasBalances = await _employeeLeaveBalanceRepo.Exist(x => x.CompanyId == companyId && x.LeavePolicyId == leavePolicy.Id);
            var hasRequests = await _leave.Exist(x => x.CompanyId == companyId && x.LeavePolicyId == leavePolicy.Id);
            if (hasBalances || hasRequests)
                return new Result<UpdateLeavePolicyRequest> { Success = false, Message = "LEAVE_POLICY_TYPE_IMMUTABLE: Create a new policy instead of changing the type of a policy that already has balances or requests." };
        }

        model.Name = model.Name.Trim();
        model.Code = model.Code.Trim().ToUpperInvariant();
        if (model.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome && _roleService is not null && !await _roleService.IsModuleAccessibleForCompany(AppModule.WorkFromHome, companyId))
            return new Result<UpdateLeavePolicyRequest> { Success = false, Message = "WFH_MODULE_DISABLED: Work from home module is not enabled for this company." };
        if (model.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
        {
            model.Paid = true;
            model.MinNoticeDays = 0;
        }
        NormalizeUnpaidLeavePolicy(model);
        NormalizeLifecycleLeavePolicy(model);
        NormalizeLeaveAttendanceMappings(model);
        if (!await ValidatePolicyShape(companyId, model.PolicyType, model.FullDayAttendanceStatusCode, model.HalfDayAttendanceStatusCode, model.HalfDayAllowed, model.WorkFromHome))
        {
            result.Message = "LEAVE_POLICY_CONFIGURATION_INVALID: The policy type and attendance configuration are not valid for this company.";
            return result;
        }
        bool duplicateLeavePolicy = await _leavePolicyRepo.Exist(lp => lp.CompanyId == companyId && (lp.Name.Equals(model.Name, StringComparison.CurrentCultureIgnoreCase) || lp.Code == model.Code) && lp.Id != model.Id);
        if (duplicateLeavePolicy)
        {
            result.StatusCode = CustomStatusCode.DuplicationLeavePolicy;
            return result;
        }
        if (leavePolicy.PolicyType == EnumsHelper.LeavePolicyType.Leave)
        {
            var policyBalances = await _employeeLeaveBalanceRepo.GetAll(
                balance => balance.CompanyId == companyId && balance.LeavePolicyId == leavePolicy.Id,
                withDefaultFilter: false);
            var duplicateEmployeeCount = policyBalances.GroupBy(balance => balance.UserId).Count(group => group.Count() > 1);
            if (duplicateEmployeeCount > 0)
            {
                return new Result<UpdateLeavePolicyRequest>
                {
                    Success = false,
                    Message = $"LEAVE_BALANCE_DUPLICATE_DATA: {duplicateEmployeeCount} employee leave allocation relationship(s) require data repair before this policy can be updated."
                };
            }
        }
        LeavePolicy updatedPolicyModel = _mapper.Map<LeavePolicy>(model);
        updatedPolicyModel.CompanyId = companyId;
        updatedPolicyModel.NormalizedName = model.Name.ToUpperInvariant();
        updatedPolicyModel.NormalizedCode = model.Code;
        updatedPolicyModel.AttendanceStatusCode = model.AttendanceStatusCode?.Trim().ToUpperInvariant();
        updatedPolicyModel.FullDayAttendanceStatusCode = model.FullDayAttendanceStatusCode?.Trim().ToUpperInvariant();
        updatedPolicyModel.HalfDayAttendanceStatusCode = model.HalfDayAttendanceStatusCode?.Trim().ToUpperInvariant();
        updatedPolicyModel.CreatedBy = leavePolicy.CreatedBy;
        updatedPolicyModel.CreatedDate = leavePolicy.CreatedDate;
        updatedPolicyModel.ApplicableTo = model.ApplicableTo?.ToList();
        var updateResult = await _leavePolicyRepo.Update(expression, updatedPolicyModel);
        result.Success = updateResult.Success;
        if (result.Success)
        {
            if (model.Lifecycle?.Enabled == true)
            {
                var lifecycleResult = await SynchronizeLifecyclePolicy(companyId, updatedPolicyModel, model.Lifecycle);
                if (!lifecycleResult.Success)
                    return new Result<UpdateLeavePolicyRequest> { Success = false, Message = lifecycleResult.Error };
            }
            else
            {
                var unlinkResult = await RetireLifecycleLinks(companyId, updatedPolicyModel.Id);
                if (!unlinkResult.Success)
                    return new Result<UpdateLeavePolicyRequest> { Success = false, Message = unlinkResult.Error };
            }
            if (updatedPolicyModel.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
            {
                if (model.WorkFromHome is null)
                    return new Result<UpdateLeavePolicyRequest> { Success = false, Message = "WFH_POLICY_CONFIGURATION_INVALID: Work From Home settings are required." };
                var sync = await SyncWorkFromHomePolicy(companyId, updatedPolicyModel, model.WorkFromHome);
                if (!sync.Success) return new Result<UpdateLeavePolicyRequest> { Success = false, Message = sync.Message };
            }
            else if (updatedPolicyModel.PolicyType == EnumsHelper.LeavePolicyType.Leave)
            {
                var sync = await SynchronizeLeavePolicyEligibility(companyId, updatedPolicyModel, model.ApplicableTo);
                if (!sync.Success) return new Result<UpdateLeavePolicyRequest> { Success = false, Message = sync.Message };
            }
            else if (IsUnpaidLeavePolicy(updatedPolicyModel) && updatedPolicyModel.HalfDayAllowed)
            {
                // Repair only source-owned half-day UL rows when an older UL
                // policy is saved. This moves legacy FH/SH absent segments to
                // HD without touching manual attendance or other leave types.
                var approvedHalfDayRequests = await _leave.GetAll(request =>
                    request.CompanyId == companyId &&
                    request.LeavePolicyId == updatedPolicyModel.Id &&
                    request.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
                    request.IsHalfDay);
                var actorUserId = CurrentContext.UserId(_httpContextAccessor);
                foreach (var request in approvedHalfDayRequests)
                    await _leaveAttendanceReconciliation.ReconcileAcceptedLeaveAsync(companyId, request.LeaveRequestId, actorUserId);
            }
            result.MethodResult = model;
        }
        return result;
    }


    private async Task<bool> HasActiveEmployeeAbsenceAsync(string companyId, string employeeId, DateTime startDate, DateTime endDate, string? excludeLeaveRequestId = null)
    {
        var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employeeId && !x.IsDeleted);
        if (employee is null || !EmploymentExitRules.IsCurrentEmployee(employee)) return false;

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var availability = EmploymentPeriod.ResolveAvailabilityOn(employee, date);
            if (availability is EmployeeLifecycleStatuses.MedicalLeave
                or EmployeeLifecycleStatuses.AnnualLeave
                or EmployeeLifecycleStatuses.MaternityLeave
                or EmployeeLifecycleStatuses.PaternityLeave
                or EmployeeLifecycleStatuses.SickLeave)
            {
                return true;
            }
        }

        var activeLeave = await _leave.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeId == employeeId && x.LeaveRequestId != excludeLeaveRequestId &&
            (x.Status == EnumsHelper.LeaveRequestStatus.Accepted || x.Status == EnumsHelper.LeaveRequestStatus.Pending) &&
            x.StartDate <= endDate && x.EndDate >= startDate
        );
        return activeLeave is not null;
    }



public async Task<Result> DeleteLeavePolicy(string policyId, string companyId)
    {
        var policy = await _leavePolicyRepo.FirstOrDefault(x =>
            x.CompanyId == companyId && x.Id == policyId && !x.IsDeleted);
        if (policy is null)
            return new Result { Success = false, Message = "LEAVE_POLICY_NOT_FOUND" };

        policy.Status = false;
        policy.IsDeleted = true;
        policy.UpdatedDate = DateTime.UtcNow;
        var updated = await _leavePolicyRepo.Update(
            Builders<LeavePolicy>.Filter.Where(x => x.Id == policyId && x.CompanyId == companyId), policy);
        if (!updated.Success)
            return new Result { Success = false, Message = "LEAVE_POLICY_DELETE_FAILED" };

        // Deleting a leave policy also unlinks its lifecycle compatibility records.
        // Keep this tenant-scoped so a policy from another company can never be
        // detached accidentally.
        if (_lifecyclePolicyRepository is not null)
        {
            var lifecycleLinks = await _lifecyclePolicyRepository.GetAll(x =>
                x.CompanyId == companyId && x.LeavePolicyId == policyId &&
                x.IsActive && !x.IsDeleted);

            foreach (var link in lifecycleLinks)
            {
                link.IsActive = false;
                link.IsDeleted = true;
                link.UpdatedBy = CurrentContext.UserId(_httpContextAccessor);
                link.UpdatedDate = DateTime.UtcNow;

                var unlinked = await _lifecyclePolicyRepository.Update(
                    Builders<EmployeeLifecyclePolicy>.Filter.Where(x =>
                        x.CompanyId == companyId &&
                        x.Id == link.Id &&
                        x.LeavePolicyId == policyId), link);
                if (!unlinked.Success)
                    return new Result { Success = false, Message = "LIFECYCLE_POLICY_UNLINK_FAILED" };
            }
        }

        return new Result { Success = true, Message = "LEAVE_POLICY_DELETED_SUCCESSFULLY" };
    }
    private async Task<Result> SynchronizeLeavePolicyEligibility(string companyId, LeavePolicy policy, string[]? applicableTo)
    {
        // null explicitly means no employees; an empty list means every active employee.
            var activeUserIds = (await _employeeRepository.GetAll(emp => emp.Status && emp.CompanyId == companyId && (emp.Lifecycle == null || emp.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || emp.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || emp.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended)))
            .Select(emp => emp.UserId)
            .Distinct()
            .ToHashSet();
        // No-accrual policies are approval-driven and never own an entitlement.
        var eligibleUserIds = policy.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.None || policy.AccrualAmount <= 0
            ? new HashSet<string>()
            : applicableTo == null
            ? new HashSet<string>()
            : applicableTo.Length == 0
                ? activeUserIds
                : applicableTo.Where(activeUserIds.Contains).ToHashSet();
        var existing = (await _employeeLeaveBalanceRepo.GetAll(lb => lb.CompanyId == companyId && lb.LeavePolicyId == policy.Id, withDefaultFilter: false)).ToList();
        var duplicateEmployeeCount = existing.GroupBy(balance => balance.UserId).Count(group => group.Count() > 1);
        if (duplicateEmployeeCount > 0)
        {
            return new Result
            {
                Success = false,
                Message = $"LEAVE_BALANCE_DUPLICATE_DATA: {duplicateEmployeeCount} employee leave allocation relationship(s) require data repair before this policy can be updated."
            };
        }
        var existingUserIds = existing.Select(lb => lb.UserId).ToHashSet();
        var now = DateTime.UtcNow;

        var additions = eligibleUserIds.Except(existingUserIds).Select(userId => new EmployeeLeaveBalance
        {
            CompanyId = companyId,
            UserId = userId,
            LeavePolicyId = policy.Id,
            TotalAllocated = policy.AccrualAmount,
            Taken = 0,
            Remaining = policy.AccrualAmount,
            IsManualAllocation = false,
            LastAccrual = now
        }).ToList();
        if (additions.Count > 0)
        {
            var addResult = await _employeeLeaveBalanceRepo.AddMany(additions);
            if (!addResult.Success) return addResult;
        }

        // Deallocation must not erase an approved-leave audit trail. Existing consumption is
        // retained, while remaining entitlement is removed. Untouched balances can be deleted.
        foreach (var balance in existing.Where(lb => !eligibleUserIds.Contains(lb.UserId)))
        {
            var filter = Builders<EmployeeLeaveBalance>.Filter.Where(lb => lb.CompanyId == companyId && lb.Id == balance.Id);
            if (balance.Taken > 0)
            {
                var taken = balance.Taken;
                var updateResult = await _employeeLeaveBalanceRepo.UpdateMany(filter, Builders<EmployeeLeaveBalance>.Update
                    .Set(lb => lb.TotalAllocated, taken).Set(lb => lb.Taken, taken).Set(lb => lb.Remaining, 0).Inc(lb => lb.Version, 1));
                if (!updateResult.Success) return updateResult;
            }
            else
            {
                var deleteResult = await _employeeLeaveBalanceRepo.Delete(filter);
                if (!deleteResult.Success) return deleteResult;
            }
        }

        return new Result { Success = true };
    }

    public async Task<Result<UpdateLeavePolicyRequest>> GetAllLeavePolicies(string companyId, bool? status)
    {
        Expression<Func<LeavePolicy, bool>> expression = status.HasValue ? lp => lp.CompanyId == companyId && lp.Status == status.Value : lp => lp.CompanyId == companyId;
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(expression);
        var list = _mapper.Map<List<UpdateLeavePolicyRequest>>(leavePolicies);
        if (_lifecyclePolicyRepository is not null)
        {
            var lifecycleLinks = (await _lifecyclePolicyRepository.GetAll(x =>
                x.CompanyId == companyId && x.IsActive && !x.IsDeleted && !string.IsNullOrWhiteSpace(x.LeavePolicyId)))
                .ToDictionary(x => x.LeavePolicyId!, StringComparer.OrdinalIgnoreCase);
            foreach (var policy in list)
            {
                if (!lifecycleLinks.TryGetValue(policy.Id, out var link)) continue;
                policy.Lifecycle = new LifecycleLeavePolicySettingsRequest
                {
                    Enabled = true,
                    EventType = link.EventType,
                    RequiresHrApproval = link.RequiresHrApproval,
                    RequiresReason = link.RequiresReason,
                    RequiresDocument = link.RequiresDocument,
                    RequiresExpectedReturnDate = link.RequiresExpectedReturnDate,
                    DefaultAccountStatus = link.DefaultAccountStatus,
                    DefaultPayrollStatus = link.DefaultPayrollStatus,
                    EffectiveFromUtc = link.EffectiveFromUtc == DateTime.MinValue ? null : link.EffectiveFromUtc
                };
                policy.AccrualPeriod = EnumsHelper.LeaveAccrualPeriod.None;
                policy.AccrualAmount = 0;
                policy.MaxBalance = 0;
                policy.CarryOverAllowed = false;
                policy.CarryOverLimit = null;
            }
        }
        foreach (var policy in list.Where(x => x.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome))
        {
            policy.Paid = true;
            var operationalPolicy = await _workFromHomePolicies.FirstOrDefault(x =>
                x.CompanyId == companyId && x.LeavePolicyId == policy.Id);
            if (operationalPolicy is null) continue;

            policy.HalfDayAllowed = operationalPolicy.AllowHalfDay;
            policy.FullDayAttendanceStatusCode = operationalPolicy.FullDayAttendanceStatusCode;
            policy.HalfDayAttendanceStatusCode = operationalPolicy.HalfDayAttendanceStatusCode;
            policy.WorkFromHome = new WorkFromHomePolicySettingsRequest
            {
                ApprovalRequired = operationalPolicy.ManagerApprovalRequired,
                ReasonRequired = operationalPolicy.RequireReason,
                AttachmentRequired = operationalPolicy.RequireAttachment,
                MaxDaysPerWeek = operationalPolicy.MaxDaysPerWeek,
                MaxDaysPerMonth = operationalPolicy.MaxDaysPerMonth,
                AllowFullDay = true,
                AllowFirstHalf = operationalPolicy.AllowHalfDay,
                AllowSecondHalf = operationalPolicy.AllowHalfDay,
                AllowMixedHalfDayLeave = operationalPolicy.AllowMixedDay,
                AllowOnWeeklyOff = operationalPolicy.AllowOnWeeklyOff,
                AllowOnHoliday = operationalPolicy.AllowOnHoliday,
                FullDayAttendanceStatusCode = operationalPolicy.FullDayAttendanceStatusCode,
                HalfDayAttendanceStatusCode = operationalPolicy.HalfDayAttendanceStatusCode,
                MixedAttendanceStatusCode = operationalPolicy.MixedAttendanceStatusCode,
                WfhWfoAttendanceStatusCode = operationalPolicy.WfhWfoAttendanceStatusCode,
                WfhHdAttendanceStatusCode = operationalPolicy.WfhHdAttendanceStatusCode,
                FullDayRequiredWorkingMinutes = (int)(operationalPolicy.FullDayMinimumHours * 60),
                HalfDayRequiredWorkingMinutes = (int)(operationalPolicy.HalfDayMinimumHours * 60),
                BreakMinutes = 0,
                SameDayCutoffTime = operationalPolicy.SameDayCutoffTime,
                SameDayGraceMinutes = operationalPolicy.SameDayGraceMinutes,
                AllowLateApplicationWithApproval = operationalPolicy.AllowLateApplicationWithApproval,
                AllowedDaysOfWeek = operationalPolicy.AllowedDaysOfWeek ?? [],
                BlockedDaysOfWeek = operationalPolicy.BlockedDaysOfWeek ?? [],
            };
        }
        foreach (var policy in list.Where(IsUnpaidLeavePolicy))
        {
            // Older UL policies predate the typed policy field. Return them as UL so
            // employee self-service can use the same no-credit workflow.
            policy.PolicyType = EnumsHelper.LeavePolicyType.UnpaidLeave;
            NormalizeUnpaidLeavePolicy(policy);
        }
        if (_roleService is not null && !await _roleService.IsModuleAccessibleForCompany(AppModule.WorkFromHome, companyId))
        {
            list = list.Where(x => x.PolicyType != EnumsHelper.LeavePolicyType.WorkFromHome).ToList();
        }
        return new Result<UpdateLeavePolicyRequest>()
        {
            Success = true,
            MethodResults = list
        };
    }

    public async Task<Result<UpdateLeavePolicyRequest>> GetMySelfServicePolicies()
    {
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var userId = CurrentContext.UserId(_httpContextAccessor);
        var policies = await GetAllLeavePolicies(companyId, true);

        if (!policies.Success || policies.MethodResults is null)
            return policies;

        // An empty ApplicableTo list is the established all-employees rule.
        // Do not expose a policy to an employee who is not entitled to request it.
        policies.MethodResults = policies.MethodResults
            .Where(policy => policy.ApplicableTo is null || policy.ApplicableTo.Length == 0 ||
                             policy.ApplicableTo.Contains(userId, StringComparer.Ordinal))
            .ToList();
        policies.TotalRecords = policies.MethodResults.Count();
        return policies;
    }

    public Task<Result> CreateLeaveRequest(LeaveRequestDto leaveRequest) => CreateLeaveRequestCore(leaveRequest, false);

    public async Task<Result> CreateLeaveRequestWithEvidence(LeaveRequestDto leaveRequest, IFormFile evidence)
    {
        if (!IsValidLeaveEvidence(evidence))
            return new Result { Success = false, Message = "LEAVE_EVIDENCE_INVALID: Upload a PDF, JPG, or PNG file up to 10 MB." };

        // Allocate the request ID before inserting it. A just-created request
        // must never be rediscovered by dates because date-only values are
        // normalized to UTC before persistence.
        var requestId = Guid.NewGuid().ToString();
        var created = await CreateLeaveRequestCore(leaveRequest, true, requestId);
        if (!created.Success) return created;

        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var request = await _leave.FirstOrDefault(x => x.CompanyId == companyId && x.LeaveRequestId == requestId && x.Status == EnumsHelper.LeaveRequestStatus.Pending);
        if (request is null)
            return new Result { Success = false, Message = "LEAVE_EVIDENCE_REQUEST_LOOKUP_FAILED: The leave request was created but could not be linked to the uploaded evidence." };

        var saved = await SaveLeaveEvidenceAsync(request, evidence);
        if (saved.Success) return saved;

        // Do not leave a request that cannot meet a mandatory-evidence rule.
        await _leave.Delete(Builders<LeaveRequest>.Filter.Where(x => x.CompanyId == companyId && x.LeaveRequestId == request.LeaveRequestId && x.Status == EnumsHelper.LeaveRequestStatus.Pending));
        return saved;
    }

    private async Task<Result> CreateLeaveRequestCore(LeaveRequestDto leaveRequest, bool evidenceIncluded, string? requestedLeaveRequestId = null)
    {
        Result result = new();
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var leavePolicy = await _leavePolicyRepo.FirstOrDefault(lp => lp.CompanyId == companyId && lp.Id == leaveRequest.LeavePolicyId && lp.Status);
        if (leavePolicy is null) return result;
        if (leavePolicy.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
            return new Result { Success = false, Message = "WFH_POLICY_REQUIRES_WFH_REQUEST: Submit Work From Home through the WFH request workflow so its attendance and clocking rules are enforced." };
        if (!TryNormalizeLeaveRequestDates(leaveRequest.StartDate, leaveRequest.EndDate, leaveRequest.IsHalfDay, leavePolicy.HalfDayAllowed,
            out var requestStartDate, out var requestEndDate, out var validationError))
        {
            return new Result { Success = false, Message = validationError };
        }
        var requestStartDateTime = UtcMidnight(requestStartDate);
        var requestEndDateTime = UtcMidnight(requestEndDate);
        var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == leaveRequest.UserId && x.Status && !x.IsDeleted && (x.Lifecycle == null || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended));
        if (employee == null) return new Result { Success = false, Message = "Employee does not belong to the authenticated company." };
        if (employee.Lifecycle is not null && EmploymentPeriod.ResolveAvailabilityOn(employee, requestStartDate.ToDateTime(TimeOnly.MinValue)) != EmployeeLifecycleStatuses.Working)
            return new Result { Success = false, Message = "LEAVE_LIFECYCLE_NOT_WORKING: Leave cannot be requested while the employee is on another leave, sabbatical, suspension, or absence state." };
        var lastWorkingDate = EmploymentPeriod.GetLastWorkingDate(employee);
        if (lastWorkingDate.HasValue && requestEndDate > DateOnly.FromDateTime(lastWorkingDate.Value))
            return new Result { Success = false, Message = "LEAVE_AFTER_EXIT" };
        if (!IsPolicyApplicableToEmployee(leavePolicy, employee.UserId))
            return new Result { Success = false, Message = "This leave policy is not assigned to the selected employee." };

        // check if employee has balance for requested leave type
        var isUnpaidLeave = IsUnpaidLeavePolicy(leavePolicy);
        var isLifecycleLinked = await IsLifecycleLinkedPolicyAsync(companyId, leavePolicy.Id);
        if (isLifecycleLinked && await RequiresLifecycleEvidenceAsync(companyId, leavePolicy.Id) && !evidenceIncluded)
            return new Result { Success = false, Message = "LEAVE_EVIDENCE_REQUIRED: Supporting evidence is required for this special leave request." };
        var employeeLeaveBalance = isUnpaidLeave || isLifecycleLinked ? null : await _employeeLeaveBalanceRepo.FirstOrDefault(elb => elb.CompanyId == companyId && elb.UserId == leaveRequest.UserId && elb.LeavePolicyId == leaveRequest.LeavePolicyId);
        if (!isUnpaidLeave && !isLifecycleLinked && employeeLeaveBalance is null)
        {
            result.StatusCode = CustomStatusCode.LeaveBalanceNotExist;
            return result;
        }

        var activeRequests = await _leave.GetAll(lr => lr.CompanyId == companyId && lr.EmployeeId == leaveRequest.UserId &&
            (lr.Status == EnumsHelper.LeaveRequestStatus.Pending || lr.Status == EnumsHelper.LeaveRequestStatus.Accepted));
        if (activeRequests.Any(lr => requestStartDateTime <= lr.EndDate && requestEndDateTime >= lr.StartDate))
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.LeaveDateOverlaps;
            result.Message = "The leave request overlaps an existing pending or accepted request.";
            return result;
        }

        var db = _workFromHomeRequests.GetCollection()?.Database;
        if (db is not null)
        {
            var attendanceCol = db.GetCollection<AttendanceModel>("Attendance");
            var hasHybrid = (await attendanceCol.Find(x =>
                x.CompanyId == companyId && x.UserId == leaveRequest.UserId &&
                x.Date >= requestStartDateTime.Date && x.Date < requestEndDateTime.Date.AddDays(1) &&
                (x.Status == "WFH+WFO" || x.RemarkCode == "WFH_WFO_COMBINATION")).Limit(1).ToListAsync()).Any();
            if (hasHybrid)
            {
                return new Result
                {
                    Success = false,
                    Message = "LEAVE_HYBRID_CONFLICT: The selected date is already fully scheduled for hybrid work (WFH + In-Office). A single day cannot have both WFH+WFO and leave simultaneously."
                };
            }
        }

        if (await HasActiveWorkFromHomeOverlap(companyId, leaveRequest.UserId, requestStartDateTime, requestEndDateTime, leaveRequest.IsHalfDay, leaveRequest.HalfDayPeriod))
            return new Result { Success = false, Message = "LEAVE_WFH_OVERLAP: Leave overlaps Work From Home for the same half or a full day." };

        // check if employee already has approved leave on request date
        var nowDate = DateTime.UtcNow.Date;
        var upcomingApprovedLeaves = await _leave.GetAll(lr => lr.CompanyId == companyId && lr.EmployeeId == leaveRequest.UserId && lr.Status == EnumsHelper.LeaveRequestStatus.Accepted && lr.EndDate >= nowDate);
        bool isOverlapping = upcomingApprovedLeaves.Any(lr => requestStartDateTime <= lr.EndDate && requestEndDateTime >= lr.StartDate);
        if (isOverlapping)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.LeaveDateOverlaps;
            result.Message = "The leave request overlaps an existing pending or accepted request.";
            return result;
        }

        // validate min advance notice day
        var totalAdvanceNoticeDays = (requestStartDateTime - DateTime.UtcNow.Date).TotalDays + 1;
        if (leavePolicy.MinNoticeDays > 0 && leavePolicy.MinNoticeDays > totalAdvanceNoticeDays)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.MinAdvanceLeaveNoticeDays;
            return result;
        }

        var leaveDates = await _workingCalendar.GetLeaveDatesAsync(companyId,
            requestStartDate, requestEndDate,
            leavePolicy.WeekendInclusive, leavePolicy.HolidayInclusive);
        decimal totalRequestedDays = leaveDates.Count;
        if (leaveRequest.IsHalfDay)
        {
            if (leaveDates.Count != 1)
                return new Result { Success = false, Message = "Half-day leave must fall on an included business day according to the selected policy." };
            totalRequestedDays = 0.5m;
        }

        if (totalRequestedDays <= 0)
            return new Result { Success = false, Message = "Leave duration must be greater than zero." };
        var pendingLeaves = await _leave.GetAll(lr =>
            lr.CompanyId == companyId && lr.EmployeeId == leaveRequest.UserId &&
            lr.LeavePolicyId == leaveRequest.LeavePolicyId &&
            lr.Status == EnumsHelper.LeaveRequestStatus.Pending &&
            lr.LeaveRequestId != requestedLeaveRequestId);
        var pendingDays = pendingLeaves.Sum(lr => lr.TotalDays);
        var availableBalance = (employeeLeaveBalance?.Remaining ?? 0m) - pendingDays;
        if (!isUnpaidLeave && !isLifecycleLinked && (!IsValidBalance(employeeLeaveBalance!) || totalRequestedDays > availableBalance))
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.InsufficientLeaveBalance;
            result.Message = $"Insufficient leave balance. You have {employeeLeaveBalance?.Remaining ?? 0:0.#} remaining, with {pendingDays:0.#} day(s) pending approval.";
            return result;
        }
        if (await HasActiveEmployeeAbsenceAsync(companyId, leaveRequest.UserId, requestStartDateTime, requestEndDateTime))
        {
            return new Result
            {
                Success = false,
                Message = "This Employee have Already active leave"
            };
        }

        var request = new LeaveRequest
        {
            LeaveRequestId = requestedLeaveRequestId ?? string.Empty,
            EmployeeId = leaveRequest.UserId,
            LeavePolicyId = leaveRequest.LeavePolicyId,
            StartDate = requestStartDateTime,
            EndDate = requestEndDateTime,
            TotalDays = totalRequestedDays,
            IsHalfDay = leavePolicy.HalfDayAllowed && leaveRequest.IsHalfDay,
            HalfDayPeriod = leaveRequest.IsHalfDay ? NormalizeHalfDayPeriod(leaveRequest.HalfDayPeriod) : null,
            Reason = leaveRequest.Reason,
            ReviewedBy = "",
            Status = EnumsHelper.LeaveRequestStatus.Pending
        };

        // WFH and leave claim the same per-employee lock inside the write
        // transaction. Recheck after acquiring it so concurrent claims cannot
        // both pass the earlier overlap query.
        try
        {
            using var session = await _mongoClient.StartSessionAsync();
            if (session is null)
            {
                throw new NotSupportedException("MongoDB session not available");
            }
            session.StartTransaction();
            await AcquireEmployeeWfhClaimLock(session, companyId, leaveRequest.UserId);
            if (await HasActiveWorkFromHomeOverlap(companyId, leaveRequest.UserId, requestStartDateTime, requestEndDateTime, leaveRequest.IsHalfDay, leaveRequest.HalfDayPeriod))
            {
                await session.AbortTransactionAsync();
                return new Result { Success = false, Message = "LEAVE_WFH_OVERLAP: Leave overlaps Work From Home for the same half or a full day." };
            }

            request.CompanyId = companyId;
            request.CreatedBy = CurrentContext.UserId(_httpContextAccessor);
            request.CreatedDate = DateTime.UtcNow;
            await _leave.GetCollection().InsertOneAsync(session, request);
            await session.CommitTransactionAsync();
            result.Success = true;
            result.StatusCode = StatusCodes.Status200OK;
            result.Message = "OK";
        }
        catch (Exception ex) when (IsTransactionUnsupported(ex))
        {
            await AcquireEmployeeWfhClaimLock(null, companyId, leaveRequest.UserId);
            if (await HasActiveWorkFromHomeOverlap(companyId, leaveRequest.UserId, requestStartDateTime, requestEndDateTime, leaveRequest.IsHalfDay, leaveRequest.HalfDayPeriod))
            {
                return new Result { Success = false, Message = "LEAVE_WFH_OVERLAP: Leave overlaps Work From Home for the same half or a full day." };
            }

            request.CompanyId = companyId;
            request.CreatedBy = CurrentContext.UserId(_httpContextAccessor);
            request.CreatedDate = DateTime.UtcNow;
            await _leave.GetCollection().InsertOneAsync(request);
            result.Success = true;
            result.StatusCode = StatusCodes.Status200OK;
            result.Message = "OK";
        }
        catch (MongoException)
        {
            return new Result { Success = false, Message = "Leave request could not be created. Please retry." };
        }
        if (result.Success)
        {
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            // An HR/Admin-created request is an administrative entry, not an employee
            // submission. Do not email the same review group about a request it just made.
            await LeaveNotification(request, request.Status, companyId, actorUserId,
                sendEmail: actorUserId == request.EmployeeId);
        }
        return result;
    }

    public async Task<Result> UpdateLeaveRequest(UpdateLeaveRequestDto leaveRequestDto)
    {
        Result result = new();
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var leavePolicyEntity = await _leavePolicyRepo.FirstOrDefault(lp => lp.CompanyId == companyId && lp.Id == leaveRequestDto.LeavePolicyId && lp.Status);
        if (leavePolicyEntity is null) return result;
        if (leavePolicyEntity.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome)
            return new Result { Success = false, Message = "WFH_POLICY_REQUIRES_WFH_REQUEST: WFH requests must use the WFH workflow." };
        if (!IsPolicyApplicableToEmployee(leavePolicyEntity, leaveRequestDto.UserId))
            return new Result { Success = false, Message = "This leave policy is not assigned to the selected employee." };
        if (!TryNormalizeLeaveRequestDates(leaveRequestDto.StartDate, leaveRequestDto.EndDate, leaveRequestDto.IsHalfDay, leavePolicyEntity.HalfDayAllowed,
            out var requestStartDate, out var requestEndDate, out var validationError))
        {
            return new Result { Success = false, Message = validationError };
        }
        var requestStartDateTime = UtcMidnight(requestStartDate);
        var requestEndDateTime = UtcMidnight(requestEndDate);

        var existingLeaveRequest = await _leave.FirstOrDefault(lr => lr.CompanyId == companyId && lr.LeaveRequestId == leaveRequestDto.LeaveRequestId && leaveRequestDto.UserId == lr.EmployeeId);
        if (existingLeaveRequest is null) return result;
        if (existingLeaveRequest.Status != EnumsHelper.LeaveRequestStatus.Pending) return result;
        if (IsLeaveRequestStarted(existingLeaveRequest, DateTime.UtcNow))
            return new Result { Success = false, Message = "This leave request has started and cannot be updated." };

        var isUnpaidLeave = IsUnpaidLeavePolicy(leavePolicyEntity);
        var isLifecycleLinked = await IsLifecycleLinkedPolicyAsync(companyId, leavePolicyEntity.Id);
        var employeeLeaveBalance = isUnpaidLeave || isLifecycleLinked ? null : await _employeeLeaveBalanceRepo.FirstOrDefault(elb => elb.CompanyId == companyId && elb.UserId == leaveRequestDto.UserId && elb.LeavePolicyId == leaveRequestDto.LeavePolicyId);
        if (!isUnpaidLeave && !isLifecycleLinked && employeeLeaveBalance is null)
        {
            result.StatusCode = CustomStatusCode.LeaveBalanceNotExist;
            return result;
        }

        var activeRequests = await _leave.GetAll(lr => lr.CompanyId == companyId && lr.EmployeeId == leaveRequestDto.UserId &&
            lr.LeaveRequestId != leaveRequestDto.LeaveRequestId &&
            (lr.Status == EnumsHelper.LeaveRequestStatus.Pending || lr.Status == EnumsHelper.LeaveRequestStatus.Accepted));
        bool isOverlapping = activeRequests.Any(lr => requestStartDateTime <= lr.EndDate && requestEndDateTime >= lr.StartDate);
        if (isOverlapping)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.LeaveDateOverlaps;
            result.Message = "The leave request overlaps an existing pending or accepted request.";
            return result;
        }

        if (await HasActiveWorkFromHomeOverlap(companyId, leaveRequestDto.UserId, requestStartDateTime, requestEndDateTime, leaveRequestDto.IsHalfDay, leaveRequestDto.HalfDayPeriod))
            return new Result { Success = false, Message = "LEAVE_WFH_OVERLAP: Leave overlaps Work From Home for the same half or a full day." };

        var totalAdvanceNoticeDays = (requestStartDateTime - DateTime.UtcNow.Date).TotalDays + 1;
        if (leavePolicyEntity.MinNoticeDays > 0 && leavePolicyEntity.MinNoticeDays > totalAdvanceNoticeDays)
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.MinAdvanceLeaveNoticeDays;
            return result;
        }

        var leaveDates = await _workingCalendar.GetLeaveDatesAsync(companyId,
            requestStartDate, requestEndDate,
            leavePolicyEntity.WeekendInclusive, leavePolicyEntity.HolidayInclusive);
        decimal totalRequestedDays = leaveDates.Count;
        if (leaveRequestDto.IsHalfDay)
        {
            if (leaveDates.Count != 1)
                return new Result { Success = false, Message = "Half-day leave must fall on an included business day according to the selected policy." };
            totalRequestedDays = 0.5m;
        }

        if (totalRequestedDays <= 0)
            return new Result { Success = false, Message = "Leave duration must be greater than zero." };
        if (!isUnpaidLeave && !isLifecycleLinked && (!IsValidBalance(employeeLeaveBalance!) || totalRequestedDays > employeeLeaveBalance!.Remaining))
        {
            result.Success = false;
            result.StatusCode = CustomStatusCode.InsufficientLeaveBalance;
            return result;
        }
        if (await HasActiveEmployeeAbsenceAsync(companyId, leaveRequestDto.UserId, requestStartDateTime, requestEndDateTime, leaveRequestDto.LeaveRequestId))
        {
            return new Result
            {
                Success = false,
                Message = "This Employee have Already active leave"
            };
        }

        var now = DateTime.UtcNow;
        try
        {
            using var session = await _mongoClient.StartSessionAsync();
            session.StartTransaction();
            await AcquireEmployeeWfhClaimLock(session, companyId, leaveRequestDto.UserId);
            if (await HasActiveWorkFromHomeOverlap(companyId, leaveRequestDto.UserId, requestStartDateTime, requestEndDateTime, leaveRequestDto.IsHalfDay, leaveRequestDto.HalfDayPeriod))
            {
                await session.AbortTransactionAsync();
                return new Result { Success = false, Message = "LEAVE_WFH_OVERLAP: Leave overlaps Work From Home for the same half or a full day." };
            }
            var filter = Builders<LeaveRequest>.Filter.Where(lr => lr.CompanyId == companyId &&
                lr.LeaveRequestId == leaveRequestDto.LeaveRequestId && lr.EmployeeId == leaveRequestDto.UserId &&
                lr.Status == EnumsHelper.LeaveRequestStatus.Pending && lr.Version == existingLeaveRequest.Version);
            var update = Builders<LeaveRequest>.Update
                .Set(lr => lr.LeavePolicyId, leaveRequestDto.LeavePolicyId)
                .Set(lr => lr.StartDate, requestStartDateTime)
                .Set(lr => lr.EndDate, requestEndDateTime)
                .Set(lr => lr.TotalDays, totalRequestedDays)
                .Set(lr => lr.IsHalfDay, leavePolicyEntity.HalfDayAllowed && leaveRequestDto.IsHalfDay)
                .Set(lr => lr.HalfDayPeriod, leaveRequestDto.IsHalfDay ? NormalizeHalfDayPeriod(leaveRequestDto.HalfDayPeriod) : null)
                .Set(lr => lr.Reason, leaveRequestDto.Reason)
                .Set(lr => lr.UpdatedDate, now)
                .Set(lr => lr.UpdatedBy, CurrentContext.UserId(_httpContextAccessor))
                .Inc(lr => lr.Version, 1);
            var changed = await _leave.GetCollection().UpdateOneAsync(session, filter, update);
            if (changed.ModifiedCount != 1)
            {
                await session.AbortTransactionAsync();
                return new Result { Success = false, Message = "LEAVE_REQUEST_VERSION_CONFLICT: The leave request changed. Refresh and try again." };
            }
            await session.CommitTransactionAsync();
            return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "OK" };
        }
        catch (Exception ex) when (IsTransactionUnsupported(ex))
        {
            await AcquireEmployeeWfhClaimLock(null, companyId, leaveRequestDto.UserId);
            if (await HasActiveWorkFromHomeOverlap(companyId, leaveRequestDto.UserId, requestStartDateTime, requestEndDateTime, leaveRequestDto.IsHalfDay, leaveRequestDto.HalfDayPeriod))
            {
                return new Result { Success = false, Message = "LEAVE_WFH_OVERLAP: Leave overlaps Work From Home for the same half or a full day." };
            }
            var standaloneFilter = Builders<LeaveRequest>.Filter.Where(lr => lr.CompanyId == companyId &&
                lr.LeaveRequestId == leaveRequestDto.LeaveRequestId && lr.EmployeeId == leaveRequestDto.UserId &&
                lr.Status == EnumsHelper.LeaveRequestStatus.Pending && lr.Version == existingLeaveRequest.Version);
            var standaloneUpdate = Builders<LeaveRequest>.Update
                .Set(lr => lr.LeavePolicyId, leaveRequestDto.LeavePolicyId)
                .Set(lr => lr.StartDate, requestStartDateTime)
                .Set(lr => lr.EndDate, requestEndDateTime)
                .Set(lr => lr.TotalDays, totalRequestedDays)
                .Set(lr => lr.IsHalfDay, leavePolicyEntity.HalfDayAllowed && leaveRequestDto.IsHalfDay)
                .Set(lr => lr.HalfDayPeriod, leaveRequestDto.IsHalfDay ? NormalizeHalfDayPeriod(leaveRequestDto.HalfDayPeriod) : null)
                .Set(lr => lr.Reason, leaveRequestDto.Reason)
                .Set(lr => lr.UpdatedDate, now)
                .Set(lr => lr.UpdatedBy, CurrentContext.UserId(_httpContextAccessor))
                .Inc(lr => lr.Version, 1);
            var standaloneChanged = await _leave.GetCollection().UpdateOneAsync(standaloneFilter, standaloneUpdate);
            if (standaloneChanged.ModifiedCount != 1)
            {
                return new Result { Success = false, Message = "LEAVE_REQUEST_VERSION_CONFLICT: The leave request changed. Refresh and try again." };
            }
            return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "OK" };
        }
        catch (MongoException)
        {
            return new Result { Success = false, Message = "Leave request could not be updated. Please retry." };
        }
    }

    private static string NormalizeHalfDayPeriod(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (normalized is "FIRST_HALF" or "SECOND_HALF") return normalized;
        // Existing half-day leave records predate segment ownership.  Preserve a
        // stable backwards-compatible interpretation instead of guessing later.
        return "FIRST_HALF";
    }

    internal static bool TryNormalizeLeaveRequestDates(DateTime start, DateTime end, bool isHalfDay, bool halfDayAllowed,
        out DateOnly requestStartDate, out DateOnly requestEndDate, out string? validationError)
    {
        requestStartDate = DateOnly.FromDateTime(start.Date);
        requestEndDate = DateOnly.FromDateTime(end.Date);
        validationError = null;

        if (isHalfDay)
        {
            if (!halfDayAllowed)
            {
                validationError = "Half-day leave is not allowed for the selected policy.";
                return false;
            }
            if (requestStartDate != requestEndDate)
            {
                validationError = "Half-day leave requires a single date.";
                return false;
            }
        }
        else if (requestEndDate < requestStartDate)
        {
            validationError = "End date cannot be before start date.";
            return false;
        }

        return true;
    }

    internal static DateTime UtcMidnight(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    internal static bool IsLeaveRequestStarted(LeaveRequest request, DateTime currentDateTime) =>
        request.StartDate.Date <= currentDateTime.Date;

    public async Task<Result<LeaveResponseDto>> GetLeaveRequest(LeaveRequestFilter? leaveFilter)
    {
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        int count;
        IEnumerable<LeaveRequest> leaveRequestList = [];
        if (leaveFilter == null)
        {
            leaveRequestList = await _leave.GetAll(lr => lr.CompanyId == companyId);
            count = await _leave.Count(lr => lr.CompanyId == companyId);
        }
        else
        {
            Expression<Func<LeaveRequest, bool>> whereCondition = lr => lr.CompanyId == companyId && (leaveFilter.Status == null || (lr.Status == leaveFilter.Status))
            && ((leaveFilter.StartDate == null || (lr.StartDate >= leaveFilter.StartDate)) && (leaveFilter.EndDate == null || (lr.StartDate <= leaveFilter.EndDate)));

            count = await _leave.Count(whereCondition);
            leaveRequestList = (await _leave.GetAggregateDataAsync<LeaveRequest>(whereCondition, pageNo: leaveFilter.PageNo, pageSize: leaveFilter.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();
        }
        var users = await _employeeRepository.GetAll(u => u.CompanyId == companyId);
        var jobTitles = await _jobTitleRepo.GetAll(j => j.CompanyId == companyId);
        var leavePolicies = await _leavePolicyRepo.GetAll(lp => lp.CompanyId == companyId);

        List<LeaveResponseDto> FinalLeaveRequestList = (from leave in leaveRequestList
                                                        join user in users on leave.EmployeeId equals user.UserId
                                                        join reviewerGroup in users on leave.ReviewedBy equals reviewerGroup.UserId into approvedUsers
                                                        from approvedUser in approvedUsers.DefaultIfEmpty()
                                                        join job in jobTitles on user.JobRole equals job.JobTitleId into jobRolesTitles
                                                        from jobTitle in jobRolesTitles.DefaultIfEmpty()
                                                        join policy in leavePolicies on leave.LeavePolicyId equals policy.Id
                                                        select new LeaveResponseDto
                                                        {
                                                            LeaveRequestId = leave.LeaveRequestId,
                                                            EmployeeName = user.FirstName + " " + user.LastName,
                                                            EmployeeId = user.EmployeeId,
                                                            ProfileUrl = Common.GetEmployeeImageUrl(user.ProfileUrl),
                                                            JobRole = jobTitle?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label),
                                                            IsHalfDay = leave.IsHalfDay,
                                                            StartDate = leave.StartDate,
                                                            EndDate = leave.EndDate,
                                                            TotalDays = leave.TotalDays,
                                                            Reason = leave.Reason,
                                                            ReviewedBy = string.IsNullOrEmpty(leave.ReviewedBy) ? "-" : approvedUser != null ?
                                                                     approvedUser.FirstName + " " + approvedUser.LastName : "-",
                                                            Status = leave.Status,
                                                            LeavePolicyName = policy.Name,
                                                            Code = policy.Code,
                                                            Comment = leave.Comment,
                                                        }
                                                        ).ToList();

        return new Result<LeaveResponseDto>
        {
            Success = true,
            TotalRecords = count,
            MethodResults = FinalLeaveRequestList
        };
    }

    public async Task<Result<MyLeaveRequestResponse>> GetMyLeaveRequests(EmpLeaveRequestFilter filter)
    {
        Result<MyLeaveRequestResponse> result = new();
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        Expression<Func<LeaveRequest, bool>> whereCondition = lr =>
                lr.CompanyId == companyId && lr.EmployeeId == filter.EmployeeId
            && (filter.Status == null || (lr.Status == filter.Status))
            && ((filter.StartDate == null || (lr.StartDate >= filter.StartDate)) && (filter.EndDate == null || (lr.StartDate <= filter.EndDate)));

        int count = await _leave.Count(whereCondition);
        List<LeaveRequest> myLeaveRequestList = (await _leave.GetAggregateDataAsync<LeaveRequest>(whereCondition, pageNo: filter.PageNo, pageSize: filter.PageSize, isAscending: false, orderedKey: "CreatedDate")).ToList();

        // get reviewer details
        var userIds = myLeaveRequestList.Select(lr => lr.ReviewedBy).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var users = await _employeeRepository.GetAll(u => u.CompanyId == companyId && userIds.Contains(u.UserId));

        // get leave policy details.
        List<string> leavePolicyIds = myLeaveRequestList.Select(lr => lr.LeavePolicyId).ToList();
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(lp => lp.CompanyId == companyId && leavePolicyIds.Contains(lp.Id));

        var userDict = users.ToDictionary(u => u.UserId, u => u);
        var responseList = myLeaveRequestList.Select(lr =>
        {
            var reviewer = !string.IsNullOrEmpty(lr.ReviewedBy) && userDict.ContainsKey(lr.ReviewedBy)
                ? userDict[lr.ReviewedBy]
                : null;
            var existingLeavePolicy = leavePolicies.FirstOrDefault(lp => lp.Id == lr.LeavePolicyId);
            return new MyLeaveRequestResponse
            {
                LeaveRequestId = lr.LeaveRequestId,
                IsHalfDay = lr.IsHalfDay,
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                TotalDays = lr.TotalDays,
                Reason = lr.Reason,
                Status = lr.Status,
                ReviewedBy = reviewer != null ? $"{reviewer.FirstName} {reviewer.LastName}" : "-",
                Comment = lr.Comment ?? "",
                LeavePolicyName = existingLeavePolicy?.Name,
                LeavePolicyId = existingLeavePolicy?.Id,
                Code = existingLeavePolicy?.Code,
            };
        }).ToList();

        result.MethodResults = responseList;
        result.TotalRecords = count;
        return result;
    }

    public async Task<Result> DeleteLeaveRequest(string leaveRequestId)
    {
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var userId = CurrentContext.UserId(_httpContextAccessor);
        Expression<Func<LeaveRequest, bool>> whereCondition = lr => lr.CompanyId == companyId && lr.EmployeeId == userId && lr.LeaveRequestId == leaveRequestId;
        LeaveRequest? existingLeaveRequest = await _leave.FirstOrDefault(whereCondition);
        if (existingLeaveRequest is null)
            return new Result { Success = false, Message = "Leave request was not found in the authenticated company." };
        // Withdrawal is idempotent. A duplicate browser click or retry after the
        // first successful request must not turn a completed withdrawal into an error.
        if (existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.WithDrawn)
            return new Result { Success = true, Message = "Leave request is already withdrawn." };
        if (existingLeaveRequest.Status is not (EnumsHelper.LeaveRequestStatus.Pending or EnumsHelper.LeaveRequestStatus.Accepted))
            return new Result { Success = false, Message = "This leave request cannot be withdrawn." };
        if (IsPastDatedLeaveRequest(existingLeaveRequest, DateTime.UtcNow))
            return new Result { Success = false, Message = "This leave request has already ended and cannot be withdrawn." };

        return await UpdateLeaveRequestStatus(leaveRequestId, new LeaveRequestUpdateDto
        {
            Status = EnumsHelper.LeaveRequestStatus.WithDrawn,
            Comment = existingLeaveRequest.Comment ?? string.Empty
        });
    }

    public async Task<Result> UpdateLeaveRequestStatus(string leaveRequestId, LeaveRequestUpdateDto model)
    {
        Result result = new();
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        Expression<Func<LeaveRequest, bool>> expression = lr => lr.CompanyId == companyId && lr.LeaveRequestId == leaveRequestId;
        var existingLeaveRequest = await _leave.FirstOrDefault(expression);

        if (existingLeaveRequest == null)
            return new Result { Success = false, Message = "Leave request was not found in the authenticated company." };
        var reviewedBy = CurrentContext.UserId(_httpContextAccessor);
        bool isSelfWithdrawal = model.Status == EnumsHelper.LeaveRequestStatus.WithDrawn && existingLeaveRequest.EmployeeId == reviewedBy;
        if (existingLeaveRequest.EmployeeId == reviewedBy && !isSelfWithdrawal)
            return new Result { Success = false, Message = "LEAVE_SELF_APPROVAL_NOT_ALLOWED: A user cannot approve their own leave request." };
        // This action is exposed only through the protected Leave Management
        // review endpoint. HR/Admin may therefore cancel an approved leave to
        // correct a source-owned attendance exception; self-service callers
        // can still withdraw only their own request.
        if (model.Status == EnumsHelper.LeaveRequestStatus.WithDrawn && !isSelfWithdrawal &&
            _httpContextAccessor.HttpContext?.Request.Path.StartsWithSegments("/api/LeaveManagement/LeaveRequest") != true &&
            _httpContextAccessor.HttpContext?.Request.Path.StartsWithSegments("/api/wfh/attendance-exceptions") != true)
            return new Result { Success = false, Message = "Only the employee who submitted this leave request can withdraw it." };
        if (model.ExpectedVersion.HasValue && model.ExpectedVersion.Value != existingLeaveRequest.Version)
            return new Result { Success = false, Message = "LEAVE_REQUEST_VERSION_CONFLICT: The leave request was changed by another reviewer. Refresh and try again." };
        if (model.Status == EnumsHelper.LeaveRequestStatus.Pending || model.Status == existingLeaveRequest.Status)
            return new Result { Success = false, Message = "The selected leave status transition is not allowed." };

        var decisionCommentError = ValidateLeaveDecisionComment(model);
        if (decisionCommentError != null)
            return new Result { Success = false, Message = decisionCommentError };

        var policyForDecision = await _leavePolicyRepo.FirstOrDefault(lp => lp.CompanyId == companyId && lp.Id == existingLeaveRequest.LeavePolicyId && lp.Status);
        if (policyForDecision == null)
            return new Result { Success = false, Message = "The leave policy for this request is not active." };
        var isUnpaidLeave = IsUnpaidLeavePolicy(policyForDecision);
        var isLifecycleLinked = await IsLifecycleLinkedPolicyAsync(companyId, policyForDecision.Id);
        if (isLifecycleLinked && model.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
            await RequiresLifecycleEvidenceAsync(companyId, policyForDecision.Id) &&
            existingLeaveRequest.Evidence.Count == 0)
        {
            return new Result
            {
                Success = false,
                Message = "LEAVE_EVIDENCE_REQUIRED: Evidence is required before this special leave can be approved."
            };
        }
        bool debitBalance = !isUnpaidLeave && !isLifecycleLinked && model.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
            existingLeaveRequest.Status is EnumsHelper.LeaveRequestStatus.Pending or EnumsHelper.LeaveRequestStatus.Rejected;
        bool creditBalance = !isUnpaidLeave && !isLifecycleLinked && existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
            model.Status is EnumsHelper.LeaveRequestStatus.Rejected or EnumsHelper.LeaveRequestStatus.WithDrawn;
        bool needsReconciliation = model.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
            existingLeaveRequest.Status is EnumsHelper.LeaveRequestStatus.Pending or EnumsHelper.LeaveRequestStatus.Rejected;
        bool needsReversal = existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
            model.Status is EnumsHelper.LeaveRequestStatus.Rejected or EnumsHelper.LeaveRequestStatus.WithDrawn;
        // Lifecycle special leave is approval-driven and has no employee balance
        // to debit. Its approval is still a durable leave status change; the
        // lifecycle case is created immediately after that commit.
        bool statusOnly = (isLifecycleLinked && (
                (model.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
                 existingLeaveRequest.Status is EnumsHelper.LeaveRequestStatus.Pending or EnumsHelper.LeaveRequestStatus.Rejected) ||
                (existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
                 model.Status is EnumsHelper.LeaveRequestStatus.Rejected or EnumsHelper.LeaveRequestStatus.WithDrawn))) ||
            (existingLeaveRequest.Status == EnumsHelper.LeaveRequestStatus.Pending &&
             model.Status is EnumsHelper.LeaveRequestStatus.Rejected or EnumsHelper.LeaveRequestStatus.WithDrawn) ||
            isUnpaidLeave && (
                model.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
                existingLeaveRequest.Status is EnumsHelper.LeaveRequestStatus.Pending or EnumsHelper.LeaveRequestStatus.Rejected ||
                needsReversal);
        if (!debitBalance && !creditBalance && !statusOnly)
            return new Result { Success = false, Message = "The selected leave status transition is not allowed." };

        EmployeeLeaveBalance? leaveBalance = null;
        if (debitBalance || creditBalance)
        {
            leaveBalance = await _employeeLeaveBalanceRepo.FirstOrDefault(lb =>
                lb.CompanyId == companyId && lb.UserId == existingLeaveRequest.EmployeeId && lb.LeavePolicyId == existingLeaveRequest.LeavePolicyId);
            if (leaveBalance == null)
                return new Result { Success = false, Message = "No leave balance was found for this employee and leave policy." };
        }

        // Validate before changing the request. Otherwise approval could be committed while
        // leave-to-attendance reconciliation later fails because the policy is incomplete.
        if (needsReconciliation)
        {
            if (IsPastDatedLeaveRequest(existingLeaveRequest, DateTime.UtcNow))
            {
                var daysSinceEnd = (DateTime.UtcNow.Date - existingLeaveRequest.EndDate.Date).TotalDays;
                bool isWithinGracePeriod = daysSinceEnd <= 7;
                bool isHrAdmin = _roleService != null && await _roleService.VerifyUserAccess(AppModule.LeaveManagement,
                    [Codeji.CMS.Utility.Constraints.Permission.Approve, Codeji.CMS.Utility.Constraints.Permission.ApproveTeam, Codeji.CMS.Utility.Constraints.Permission.Override, Codeji.CMS.Utility.Constraints.Permission.Edit], reviewedBy, companyId);
                if (!isWithinGracePeriod && !isHrAdmin)
                    return new Result { Success = false, Message = "Leave requests older than the 7-day approval grace period require HR Admin authorization." };
            }

            var mappedStatus = existingLeaveRequest.IsHalfDay && IsUnpaidLeavePolicy(policyForDecision)
                ? "HD"
                : existingLeaveRequest.IsHalfDay
                    ? policyForDecision.HalfDayAttendanceStatusCode ?? policyForDecision.FullDayAttendanceStatusCode ?? policyForDecision.AttendanceStatusCode
                    : policyForDecision.FullDayAttendanceStatusCode ?? policyForDecision.AttendanceStatusCode;
            if (string.IsNullOrWhiteSpace(mappedStatus) || !await ValidateAttendanceStatusMapping(companyId, mappedStatus))
                return new Result { Success = false, Message = existingLeaveRequest.IsHalfDay ? "Select an active leave attendance status for half-day leave before approval." : "Select an active leave attendance status for full-day leave before approval." };
        var db = _workFromHomeRequests.GetCollection()?.Database;
        if (db is not null)
        {
            var attendanceCol = db.GetCollection<AttendanceModel>("Attendance");
            var hasHybrid = (await attendanceCol.Find(x =>
                x.CompanyId == companyId && x.UserId == existingLeaveRequest.EmployeeId &&
                x.Date >= existingLeaveRequest.StartDate.Date && x.Date < existingLeaveRequest.EndDate.Date.AddDays(1) &&
                (x.Status == "WFH+WFO" || x.RemarkCode == "WFH_WFO_COMBINATION")).Limit(1).ToListAsync()).Any();
            if (hasHybrid)
            {
                return new Result
                {
                    Success = false,
                    Message = "LEAVE_HYBRID_CONFLICT: The selected date is already fully scheduled for hybrid work (WFH + In-Office). A single day cannot have both WFH+WFO and leave simultaneously."
                };
            }
        }

            if (await HasActiveWorkFromHomeOverlap(companyId, existingLeaveRequest.EmployeeId, existingLeaveRequest.StartDate, existingLeaveRequest.EndDate, existingLeaveRequest.IsHalfDay, existingLeaveRequest.HalfDayPeriod))
                return new Result { Success = false, Message = "LEAVE_WFH_OVERLAP: Leave cannot be approved because it overlaps Work From Home for the same half or a full day." };
        }

        var now = DateTime.UtcNow;
        var leaveFilter = Builders<LeaveRequest>.Filter.Where(lr =>
            lr.CompanyId == companyId && lr.LeaveRequestId == leaveRequestId &&
            lr.Status == existingLeaveRequest.Status && lr.Version == existingLeaveRequest.Version);
        var leaveUpdate = Builders<LeaveRequest>.Update
            .Set(lr => lr.ReviewedBy, reviewedBy)
            .Set(lr => lr.Comment, model.Comment)
            .Set(lr => lr.Status, model.Status)
            .Set(lr => lr.ReviewedAt, now)
            .Set(lr => lr.UpdatedDate, now)
            .Inc(lr => lr.Version, 1);

        if (needsReconciliation)
            leaveUpdate = leaveUpdate
                .Set(lr => lr.ReconciliationStatus, "Pending")
                .Set(lr => lr.ReconciliationErrorCode, null)
                .Set(lr => lr.ReconciliationErrorMessage, null)
                .Set(lr => lr.NextReconciliationAttemptAtUtc, now);
        else if (needsReversal)
            leaveUpdate = leaveUpdate
                .Set(lr => lr.ReconciliationStatus, "ReversalPending")
                .Set(lr => lr.ReconciliationErrorCode, null)
                .Set(lr => lr.ReconciliationErrorMessage, null)
                .Set(lr => lr.NextReconciliationAttemptAtUtc, now);

        try
        {
            using var session = await _mongoClient.StartSessionAsync();
            session.StartTransaction();

            if (needsReconciliation)
            {
                await AcquireEmployeeWfhClaimLock(session, companyId, existingLeaveRequest.EmployeeId);
                if (await HasActiveWorkFromHomeOverlap(companyId, existingLeaveRequest.EmployeeId, existingLeaveRequest.StartDate, existingLeaveRequest.EndDate, existingLeaveRequest.IsHalfDay, existingLeaveRequest.HalfDayPeriod))
                {
                    await session.AbortTransactionAsync();
                    return new Result { Success = false, Message = "LEAVE_WFH_OVERLAP: Leave cannot be approved because it overlaps Work From Home for the same half or a full day." };
                }
            }

            if (debitBalance || creditBalance)
            {
                var balanceFilter = Builders<EmployeeLeaveBalance>.Filter.Where(lb =>
                    lb.CompanyId == companyId && lb.Id == leaveBalance!.Id &&
                    (!debitBalance || lb.Remaining >= existingLeaveRequest.TotalDays) &&
                    (!creditBalance || lb.Taken >= existingLeaveRequest.TotalDays));
                decimal remainingChange = debitBalance ? -existingLeaveRequest.TotalDays : existingLeaveRequest.TotalDays;
                decimal takenChange = -remainingChange;
                var balanceUpdate = Builders<EmployeeLeaveBalance>.Update
                    .Inc(lb => lb.Remaining, remainingChange)
                    .Inc(lb => lb.Taken, takenChange)
                    .Inc(lb => lb.Version, 1)
                    .Set(lb => lb.UpdatedDate, now);
                var balanceResult = await _employeeLeaveBalanceRepo.GetCollection()
                    .UpdateOneAsync(session, balanceFilter, balanceUpdate);
                if (balanceResult.ModifiedCount != 1)
                {
                    await session.AbortTransactionAsync();
                    result.StatusCode = CustomStatusCode.InsufficientLeaveBalance;
                    return result;
                }
            }

            var leaveResult = await _leave.GetCollection().UpdateOneAsync(session, leaveFilter, leaveUpdate);
            if (leaveResult.ModifiedCount != 1)
            {
                await session.AbortTransactionAsync();
                result.Message = "The leave request was changed by another reviewer. Refresh and try again.";
                return result;
            }

            await session.CommitTransactionAsync();
            existingLeaveRequest.Status = model.Status;
            existingLeaveRequest.ReviewedBy = reviewedBy;
            existingLeaveRequest.Comment = model.Comment;
            existingLeaveRequest.ReviewedAt = now;
            existingLeaveRequest.Version++;
            existingLeaveRequest.ReconciliationStatus = needsReconciliation ? "Pending" : needsReversal ? "ReversalPending" : existingLeaveRequest.ReconciliationStatus;
            result.Success = true;
        }
        catch (Exception ex) when (IsTransactionUnsupported(ex))
        {
            if (needsReconciliation)
            {
                // Standalone MongoDB has no transaction support. Preserve the
                // leave/WFH conflict check, then use the existing optimistic
                // leave update and balance-compensation path so HR can complete
                // ordinary approvals on the current development deployment.
                await AcquireEmployeeWfhClaimLock(null, companyId, existingLeaveRequest.EmployeeId);
                if (await HasActiveWorkFromHomeOverlap(companyId, existingLeaveRequest.EmployeeId,
                        existingLeaveRequest.StartDate, existingLeaveRequest.EndDate,
                        existingLeaveRequest.IsHalfDay, existingLeaveRequest.HalfDayPeriod))
                {
                    return new Result
                    {
                        Success = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        ErrorCode = "LEAVE_WFH_OVERLAP",
                        Message = "Leave cannot be approved because it overlaps Work From Home for the same half or a full day."
                    };
                }
            }
            // Local standalone MongoDB does not support transactions. This fallback keeps
            // compare-and-set protection and compensates the balance when the leave write
            // loses its optimistic-concurrency race.
            result = await ApplyLeaveStatusWithoutTransactionAsync(
                companyId, existingLeaveRequest, leaveBalance, leaveFilter, leaveUpdate,
                debitBalance, creditBalance, now);
            if (!result.Success) return result;
            existingLeaveRequest.Status = model.Status;
            existingLeaveRequest.ReviewedBy = reviewedBy;
            existingLeaveRequest.Comment = model.Comment;
            existingLeaveRequest.ReviewedAt = now;
            existingLeaveRequest.Version++;
        }
        catch (MongoException)
        {
            return new Result { Success = false, Message = "Leave update could not be completed. Please retry or contact an administrator." };
        }

        if (result.Success && model.Status == EnumsHelper.LeaveRequestStatus.Accepted)
        {
            var reconciliation = await _leaveAttendanceReconciliation.ReconcileAcceptedLeaveAsync(companyId, leaveRequestId, reviewedBy);
            // The decision and the reconciliation have separate durable states.
            // A retryable attendance failure must never turn a committed approval
            // into a false rejection or silently look like a completed sync.
            if (!reconciliation.Success)
            {
                await MarkReconciliationRetryAsync(companyId, leaveRequestId, reconciliation.Message);
                result = new Result { Success = true, Message = "Leave approved. Attendance synchronization is pending retry." };
            }
            if (_lifecycleCases is not null)
            {
                var lifecycleCase = await _lifecycleCases.CreateFromApprovedLeaveSystemAsync(new Codeji.CMS.DTO.Employee.CreateEmployeeLifecycleCaseRequest { LeaveRequestId = leaveRequestId }, reviewedBy);
                if (!lifecycleCase.Success && lifecycleCase.Error is not "LIFECYCLE_POLICY_LINK_NOT_FOUND")
                    result = new Result { Success = false, Message = $"Leave approved but lifecycle case creation failed: {lifecycleCase.Error}" };
            }
        }
        else if (result.Success && model.Status is EnumsHelper.LeaveRequestStatus.Rejected or EnumsHelper.LeaveRequestStatus.WithDrawn)
        {
            if (_lifecycleCases is not null)
                await _lifecycleCases.CancelFromLeaveRequestSystemAsync(leaveRequestId, reviewedBy, model.Comment ?? "Leave decision reversed");
            result = await _leaveAttendanceReconciliation.ReverseLeaveAttendanceAsync(companyId, leaveRequestId, existingLeaveRequest.Version, reviewedBy);
        }
        if (result.Success)
        {
            await LeaveNotification(existingLeaveRequest, model.Status, companyId, reviewedBy);
        }
        return result;
    }

    /// <summary>
    /// Executes linked WFH cancellation and leave attendance regeneration while
    /// preserving an already accepted leave request and its consumed balance.
    /// The callback must apply all source-owned WFH, attendance, exception, and
    /// outbox writes using the supplied Mongo session.
    /// </summary>
    public async Task<Result> ExecuteAcceptedLeaveWfhConflictTransactionAsync(
        string leaveRequestId,
        string reason,
        Func<IClientSessionHandle, LeaveRequest, Task<Result>> applyLinkedWrites)
    {
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var actorUserId = CurrentContext.UserId(_httpContextAccessor);
        if (string.IsNullOrWhiteSpace(leaveRequestId) || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 3 or > 500 || applyLinkedWrites is null)
            return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "LEAVE_WFH_CONFLICT_REASON_REQUIRED: A leave request, linked write operation, and reason are required." };
        if (_roleService is null || !await _roleService.VerifyUserAccess(AppModule.Attendance, [Codeji.CMS.Utility.Constraints.Permission.Edit], actorUserId, companyId))
            return new Result { Success = false, StatusCode = StatusCodes.Status403Forbidden, Message = "LEAVE_WFH_CONFLICT_NOT_AUTHORIZED: Attendance edit permission is required." };

        var request = await _leave.FirstOrDefault(x => x.CompanyId == companyId && x.LeaveRequestId == leaveRequestId);
        if (request is null || request.Status != EnumsHelper.LeaveRequestStatus.Accepted)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_CONFLICT_REQUEST_CHANGED: Only an accepted leave request can be preserved during conflict resolution." };
        var policy = await _leavePolicyRepo.FirstOrDefault(x => x.CompanyId == companyId && x.Id == request.LeavePolicyId && x.Status);
        if (policy is null)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_CONFLICT_POLICY_UNAVAILABLE: The leave policy is no longer active." };

        try
        {
            using var session = await _mongoClient.StartSessionAsync();
            session.StartTransaction();
            await AcquireEmployeeWfhClaimLock(session, companyId, request.EmployeeId);
            var current = await _leave.GetCollection().Find(session,
                Builders<LeaveRequest>.Filter.Where(x => x.CompanyId == companyId && x.LeaveRequestId == leaveRequestId &&
                    x.EmployeeId == request.EmployeeId && x.Status == EnumsHelper.LeaveRequestStatus.Accepted && x.Version == request.Version))
                .FirstOrDefaultAsync();
            if (current is null)
            {
                await session.AbortTransactionAsync();
                return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_CONFLICT_REQUEST_CHANGED: The accepted leave changed before conflict resolution." };
            }
            var linkedResult = await applyLinkedWrites(session, current);
            if (!linkedResult.Success)
            {
                await session.AbortTransactionAsync();
                return linkedResult;
            }
            await session.CommitTransactionAsync();
            return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Approved leave retained and linked WFH request(s) cancelled atomically." };
        }
        catch (Exception ex) when (IsTransactionUnsupported(ex))
        {
            return new Result { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, Message = "LEAVE_WFH_CONFLICT_TRANSACTION_REQUIRED: This conflict requires MongoDB transaction support. No leave, WFH, attendance, or exception changes were applied." };
        }
        catch
        {
            return new Result { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, Message = "LEAVE_WFH_CONFLICT_FAILED: The linked conflict transaction failed. No linked changes were committed." };
        }
    }

    /// <summary>
    /// Withdraws an accepted leave request and executes source-linked attendance
    /// cleanup in the same Mongo transaction. This is reserved for the HR WFH
    /// conflict workflow; ordinary leave decisions continue through the normal API.
    /// </summary>
    public async Task<Result> WithdrawAcceptedLeaveForWfhConflictAsync(
        string leaveRequestId,
        string reason,
        Func<IClientSessionHandle, LeaveRequest, Task<Result>> applyLinkedWrites)
    {
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var actorUserId = CurrentContext.UserId(_httpContextAccessor);
        if (string.IsNullOrWhiteSpace(leaveRequestId) || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 3 or > 500 || applyLinkedWrites is null)
            return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "LEAVE_WFH_CONFLICT_REASON_REQUIRED: A leave request, linked write operation, and reason are required." };
        if (_roleService is null || !await _roleService.VerifyUserAccess(AppModule.Attendance, [Codeji.CMS.Utility.Constraints.Permission.Edit], actorUserId, companyId))
            return new Result { Success = false, StatusCode = StatusCodes.Status403Forbidden, Message = "LEAVE_WFH_CONFLICT_NOT_AUTHORIZED: Attendance edit permission is required." };

        var request = await _leave.FirstOrDefault(x => x.CompanyId == companyId && x.LeaveRequestId == leaveRequestId);
        if (request is null || request.Status != EnumsHelper.LeaveRequestStatus.Accepted)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_CONFLICT_REQUEST_CHANGED: Only an accepted leave request can be withdrawn for this conflict." };
        var policy = await _leavePolicyRepo.FirstOrDefault(x => x.CompanyId == companyId && x.Id == request.LeavePolicyId && x.Status);
        if (policy is null)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_CONFLICT_POLICY_UNAVAILABLE: The leave policy is no longer active." };
        var unpaid = IsUnpaidLeavePolicy(policy);
        var lifecycleLinked = await IsLifecycleLinkedPolicyAsync(companyId, policy.Id);
        EmployeeLeaveBalance? balance = null;
        if (!unpaid && !lifecycleLinked)
        {
            balance = await _employeeLeaveBalanceRepo.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.EmployeeId && x.LeavePolicyId == request.LeavePolicyId);
            if (balance is null || balance.Taken < request.TotalDays)
                return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_CONFLICT_BALANCE_INVALID: The consumed leave balance cannot be restored safely." };
        }

        var now = DateTime.UtcNow;
        var committed = false;
        try
        {
            using var session = await _mongoClient.StartSessionAsync();
            session.StartTransaction();
            await AcquireEmployeeWfhClaimLock(session, companyId, request.EmployeeId);

            var leaveFilter = Builders<LeaveRequest>.Filter.Where(x => x.CompanyId == companyId &&
                x.LeaveRequestId == leaveRequestId && x.EmployeeId == request.EmployeeId &&
                x.Status == EnumsHelper.LeaveRequestStatus.Accepted && x.Version == request.Version);
            var transactionRequest = await _leave.GetCollection().Find(session, leaveFilter).FirstOrDefaultAsync();
            if (transactionRequest is null)
            {
                await session.AbortTransactionAsync();
                return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_CONFLICT_REQUEST_CHANGED: The leave request changed while the conflict was being reviewed." };
            }

            if (balance is not null)
            {
                var balanceFilter = Builders<EmployeeLeaveBalance>.Filter.Where(x => x.CompanyId == companyId && x.Id == balance.Id &&
                    x.UserId == transactionRequest.EmployeeId && x.LeavePolicyId == transactionRequest.LeavePolicyId &&
                    x.Version == balance.Version && x.Taken >= transactionRequest.TotalDays);
                var balanceUpdate = Builders<EmployeeLeaveBalance>.Update
                    .Inc(x => x.Remaining, transactionRequest.TotalDays)
                    .Inc(x => x.Taken, -transactionRequest.TotalDays)
                    .Inc(x => x.Version, 1)
                    .Set(x => x.UpdatedDate, now);
                var balanceWrite = await _employeeLeaveBalanceRepo.GetCollection().UpdateOneAsync(session, balanceFilter, balanceUpdate);
                if (balanceWrite.ModifiedCount != 1)
                {
                    await session.AbortTransactionAsync();
                    return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_CONFLICT_BALANCE_CHANGED: The leave balance changed and was not restored." };
                }
            }

            var leaveUpdate = Builders<LeaveRequest>.Update
                .Set(x => x.Status, EnumsHelper.LeaveRequestStatus.WithDrawn)
                .Set(x => x.ReviewedBy, actorUserId)
                .Set(x => x.Comment, reason.Trim())
                .Set(x => x.ReviewedAt, now)
                .Set(x => x.UpdatedDate, now)
                .Set(x => x.ReconciliationStatus, "Reversed")
                .Set(x => x.ReconciliationErrorCode, null)
                .Set(x => x.ReconciliationErrorMessage, null)
                .Set(x => x.NextReconciliationAttemptAtUtc, null)
                .Set(x => x.ReversedAtUtc, now)
                .Inc(x => x.Version, 1);
            var leaveWrite = await _leave.GetCollection().UpdateOneAsync(session, leaveFilter, leaveUpdate);
            if (leaveWrite.ModifiedCount != 1)
            {
                await session.AbortTransactionAsync();
                return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_CONFLICT_REQUEST_CHANGED: The leave request changed and was not withdrawn." };
            }

            var linkedResult = await applyLinkedWrites(session, transactionRequest);
            if (!linkedResult.Success)
            {
                await session.AbortTransactionAsync();
                return linkedResult;
            }
            await session.CommitTransactionAsync();
            committed = true;

            request.Status = EnumsHelper.LeaveRequestStatus.WithDrawn;
            request.ReviewedBy = actorUserId;
            request.Comment = reason.Trim();
            request.ReviewedAt = now;
            request.Version++;
            request.ReconciliationStatus = "Reversed";
            request.ReversedAtUtc = now;
            if (lifecycleLinked && _lifecycleCases is not null)
            {
                try
                {
                    var lifecycleResult = await _lifecycleCases.CancelFromLeaveRequestSystemAsync(leaveRequestId, actorUserId, reason.Trim());
                    if (!lifecycleResult.Success)
                        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Leave and attendance changes committed. Linked lifecycle cancellation needs retry." };
                }
                catch
                {
                    return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Leave and attendance changes committed. Linked lifecycle cancellation needs retry." };
                }
            }
            await LeaveNotification(request, EnumsHelper.LeaveRequestStatus.WithDrawn, companyId, actorUserId);
            return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Leave withdrawn and linked WFH conflict resolved." };
        }
        catch (Exception ex) when (IsTransactionUnsupported(ex))
        {
            return new Result { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, Message = "LEAVE_WFH_CONFLICT_TRANSACTION_REQUIRED: This conflict requires MongoDB transaction support. No leave, balance, attendance, or exception changes were applied." };
        }
        catch
        {
            return committed
                ? new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Leave and attendance changes committed. A follow-up lifecycle or notification action needs retry." }
                : new Result { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, Message = "LEAVE_WFH_CONFLICT_FAILED: The conflict transaction failed. No linked changes were committed." };
        }
    }

    internal static bool IsValidBalance(EmployeeLeaveBalance balance) =>
        balance.TotalAllocated >= 0 && balance.Taken >= 0 && balance.Remaining >= 0 &&
        balance.TotalAllocated == balance.Taken + balance.Remaining;

    internal static bool TryCalculateAllocation(decimal totalAllocated, decimal taken, out decimal remaining)
    {
        remaining = totalAllocated - taken;
        return totalAllocated >= 0 && taken >= 0 && remaining >= 0;
    }

    private static bool IsUnpaidLeavePolicy(LeavePolicy policy) =>
        policy.PolicyType == EnumsHelper.LeavePolicyType.UnpaidLeave ||
        string.Equals(policy.Code?.Trim(), "UL", StringComparison.OrdinalIgnoreCase) &&
        !policy.Paid && policy.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.None;

    private async Task<bool> IsLifecycleLinkedPolicyAsync(string companyId, string leavePolicyId)
    {
        if (_lifecyclePolicyRepository is null || string.IsNullOrWhiteSpace(leavePolicyId)) return false;
        var today = DateOnly.FromDateTime(Codeji.CMS.Utility.Helpers.IndiaTime.Today);
        var policies = await _lifecyclePolicyRepository.GetAll(x => x.CompanyId == companyId && x.LeavePolicyId == leavePolicyId && x.IsActive && !x.IsDeleted);
        return policies.Any(x => x.EffectiveFromUtc == DateTime.MinValue || Codeji.CMS.Utility.Helpers.IndiaTime.ToBusinessDate(x.EffectiveFromUtc) <= today);
    }

    private async Task<bool> RequiresLifecycleEvidenceAsync(string companyId, string leavePolicyId)
    {
        if (_lifecyclePolicyRepository is null) return false;
        return (await _lifecyclePolicyRepository.GetAll(x => x.CompanyId == companyId && x.LeavePolicyId == leavePolicyId && x.IsActive && !x.IsDeleted))
            .Any(x => x.RequiresDocument);
    }

    private static bool IsValidLeaveEvidence(IFormFile? file) =>
        file is { Length: > 0 and <= 10 * 1024 * 1024 } &&
        new[] { "application/pdf", "image/jpeg", "image/png" }.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase);

    private async Task<Result> SaveLeaveEvidenceAsync(LeaveRequest request, IFormFile file)
    {
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var actor = CurrentContext.UserId(_httpContextAccessor);
        var root = Path.Combine(Directory.GetCurrentDirectory(), "PrivateLeaveEvidence", companyId, request.LeaveRequestId);
        Directory.CreateDirectory(root);
        var storageFile = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(root, storageFile);
        try
        {
            await using (var output = File.Create(path)) await file.CopyToAsync(output);
            await using var input = File.OpenRead(path);
            var evidence = new LeaveRequestEvidence { FileName = Path.GetFileName(file.FileName), ContentType = file.ContentType, SizeBytes = file.Length, StorageKey = storageFile, Sha256 = Convert.ToHexString(await SHA256.HashDataAsync(input)), UploadedBy = actor };
            request.Evidence.Add(evidence); request.UpdatedBy = actor; request.UpdatedDate = DateTime.UtcNow;
            var saved = await _leave.Update(Builders<LeaveRequest>.Filter.Where(x => x.CompanyId == companyId && x.LeaveRequestId == request.LeaveRequestId && x.Version == request.Version), request);
            if (saved.Success) return new Result { Success = true };
            File.Delete(path);
            return new Result { Success = false, Message = "LEAVE_EVIDENCE_VERSION_CONFLICT: Refresh and try again." };
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            return new Result { Success = false, Message = "LEAVE_EVIDENCE_SAVE_FAILED: The request was not submitted." };
        }
    }

    private async Task<(bool Success, string? Error)> SynchronizeLifecyclePolicy(
        string companyId, LeavePolicy leavePolicy, LifecycleLeavePolicySettingsRequest settings)
    {
        if (_lifecyclePolicyService is null)
            return (false, "LIFECYCLE_POLICY_SERVICE_UNAVAILABLE: Lifecycle configuration could not be synchronized.");
        if (string.IsNullOrWhiteSpace(settings.EventType))
            return (false, "LIFECYCLE_POLICY_EVENT_REQUIRED: Select a lifecycle event.");
        if (!IsSupportedSpecialLeaveAccountStatus(settings.DefaultAccountStatus))
            return (false, "LIFECYCLE_ACCOUNT_STATUS_INVALID: Special leave can use Active or TemporarilyDisabled account access.");
        if (!IsSupportedSpecialLeavePayrollStatus(settings.DefaultPayrollStatus))
            return (false, "LIFECYCLE_PAYROLL_STATUS_INVALID: Special leave can use Active, Hold, or Paused payroll handling.");

        var saved = await _lifecyclePolicyService.SavePolicyAsync(new EmployeeLifecyclePolicy
        {
            EventType = settings.EventType.Trim(),
            LeavePolicyId = leavePolicy.Id,
            EffectiveFromUtc = settings.EffectiveFromUtc ?? DateTime.MinValue,
            RequiresHrApproval = settings.RequiresHrApproval,
            RequiresReason = settings.RequiresReason,
            RequiresDocument = settings.RequiresDocument,
            RequiresExpectedReturnDate = settings.RequiresExpectedReturnDate,
            DefaultAccountStatus = NormalizeSpecialLeaveAccountStatus(settings.DefaultAccountStatus),
            DefaultPayrollStatus = NormalizeSpecialLeavePayrollStatus(settings.DefaultPayrollStatus),
            AttendanceExpected = false,
            LeaveRequestsAllowed = true,
            WorkFromHomeRequestsAllowed = false,
            IsActive = true,
            // The leave-policy form is not a lifecycle-policy version editor.
            // Resolve the current lifecycle version server-side instead of
            // rejecting a valid edit because the mapped snapshot is stale.
            Version = 0
        }, companyId, CurrentContext.UserId(_httpContextAccessor));
        return saved.Success ? (true, null) : (false, saved.Error ?? "LIFECYCLE_POLICY_SAVE_FAILED: Lifecycle configuration was not saved.");
    }

    private async Task<(bool Success, string? Error)> RetireLifecycleLinks(string companyId, string leavePolicyId)
    {
        if (_lifecyclePolicyRepository is null) return (true, null);
        var activeLinks = await _lifecyclePolicyRepository.GetAll(x =>
            x.CompanyId == companyId && x.LeavePolicyId == leavePolicyId && x.IsActive && !x.IsDeleted);
        foreach (var link in activeLinks)
        {
            link.IsActive = false;
            link.IsDeleted = true;
            link.UpdatedBy = CurrentContext.UserId(_httpContextAccessor);
            link.UpdatedDate = DateTime.UtcNow;
            var retired = await _lifecyclePolicyRepository.Update(
                Builders<EmployeeLifecyclePolicy>.Filter.Where(x =>
                    x.CompanyId == companyId && x.Id == link.Id && x.Version == link.Version), link);
            if (!retired.Success)
                return (false, "LIFECYCLE_POLICY_UNLINK_FAILED: The old lifecycle link was not retired.");
        }
        return (true, null);
    }

    private static void NormalizeLifecycleLeavePolicy(LeavePolicyRequest policy)
    {
        if (policy.Lifecycle?.Enabled != true) return;
        policy.AccrualPeriod = EnumsHelper.LeaveAccrualPeriod.None;
        policy.AccrualAmount = 0;
        policy.MaxBalance = 0;
        policy.CarryOverAllowed = false;
        policy.CarryOverLimit = null;
    }

    private static bool IsSupportedSpecialLeaveAccountStatus(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() is "Active" or "TemporarilyDisabled";

    private static bool IsSupportedSpecialLeavePayrollStatus(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() is "Active" or "Hold" or "Paused";

    private static string? NormalizeSpecialLeaveAccountStatus(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeSpecialLeavePayrollStatus(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void NormalizeLifecycleLeavePolicy(UpdateLeavePolicyRequest policy)
    {
        if (policy.Lifecycle?.Enabled != true) return;
        policy.AccrualPeriod = EnumsHelper.LeaveAccrualPeriod.None;
        policy.AccrualAmount = 0;
        policy.MaxBalance = 0;
        policy.CarryOverAllowed = false;
        policy.CarryOverLimit = null;
    }

    private static bool IsPolicyApplicableToEmployee(LeavePolicy policy, string? userId) =>
        !string.IsNullOrWhiteSpace(userId) &&
        (policy.ApplicableTo is null || policy.ApplicableTo.Count == 0 ||
         policy.ApplicableTo.Contains(userId, StringComparer.Ordinal));

    private static bool IsUnpaidLeavePolicy(UpdateLeavePolicyRequest policy) =>
        policy.PolicyType == EnumsHelper.LeavePolicyType.UnpaidLeave ||
        string.Equals(policy.Code?.Trim(), "UL", StringComparison.OrdinalIgnoreCase) &&
        !policy.Paid && policy.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.None;

    private static void NormalizeUnpaidLeavePolicy(LeavePolicyRequest policy)
    {
        if (policy.PolicyType != EnumsHelper.LeavePolicyType.UnpaidLeave) return;
        policy.Paid = false;
        policy.AccrualPeriod = EnumsHelper.LeaveAccrualPeriod.None;
        policy.AccrualAmount = 0;
        policy.MaxBalance = 0;
        policy.CarryOverAllowed = false;
        policy.CarryOverLimit = 0;
        // UL is an approval-only company entitlement, not an employee credit
        // allocation. An empty ApplicableTo list follows the established policy
        // convention for every active employee.
        policy.ApplicableTo = [];
        if (policy.HalfDayAllowed)
            policy.HalfDayAttendanceStatusCode = "HD";
    }

    private static void NormalizeUnpaidLeavePolicy(UpdateLeavePolicyRequest policy)
    {
        if (policy.PolicyType != EnumsHelper.LeavePolicyType.UnpaidLeave) return;
        policy.Paid = false;
        policy.AccrualPeriod = EnumsHelper.LeaveAccrualPeriod.None;
        policy.AccrualAmount = 0;
        policy.MaxBalance = 0;
        policy.CarryOverAllowed = false;
        policy.CarryOverLimit = 0;
        policy.ApplicableTo = [];
        if (policy.HalfDayAllowed)
            policy.HalfDayAttendanceStatusCode = "HD";
    }

    internal static bool IsPastDatedLeaveRequest(LeaveRequest request, DateTime currentDate)
    {
        var currentDay = currentDate.Date;
        return request.EndDate.Date < currentDay;
    }

    internal static string? ValidateLeaveDecisionComment(LeaveRequestUpdateDto model)
    {
        if (model.Status != EnumsHelper.LeaveRequestStatus.Rejected)
            return null;

        return string.IsNullOrWhiteSpace(model.Comment)
            ? "Rejection requires a reason."
            : null;
    }

    private Task MarkReconciliationRetryAsync(string companyId, string leaveRequestId, string? message)
    {
        var now = DateTime.UtcNow;
        return _leave.GetCollection().UpdateOneAsync(
            x => x.CompanyId == companyId && x.LeaveRequestId == leaveRequestId,
            Builders<LeaveRequest>.Update
                .Set(x => x.ReconciliationStatus, "RetryScheduled")
                .Set(x => x.ReconciliationErrorCode, "LEAVE_RECONCILIATION_FAILED")
                .Set(x => x.ReconciliationErrorMessage, message)
                .Set(x => x.LastReconciliationAttemptAtUtc, now)
                .Set(x => x.NextReconciliationAttemptAtUtc, now.AddMinutes(5))
                .Inc(x => x.ReconciliationAttempts, 1));
    }

    internal static bool IsTransactionUnsupported(Exception exception) =>
        exception is NotSupportedException ||
        (exception is MongoCommandException mongoException && (mongoException.Code == 20 || mongoException.Code == 263)) ||
        exception.Message.Contains("Transaction numbers are only allowed", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("replica set", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("standalone servers do not support transactions", StringComparison.OrdinalIgnoreCase);

    private async Task<Result> ApplyLeaveStatusWithoutTransactionAsync(
        string companyId,
        LeaveRequest leaveRequest,
        EmployeeLeaveBalance? leaveBalance,
        FilterDefinition<LeaveRequest> leaveFilter,
        UpdateDefinition<LeaveRequest> leaveUpdate,
        bool debitBalance,
        bool creditBalance,
        DateTime now)
    {
        var result = new Result { Success = false };
        var balanceChanged = false;
        decimal balanceChange = debitBalance ? -leaveRequest.TotalDays : leaveRequest.TotalDays;

        if (debitBalance || creditBalance)
        {
            var balanceFilter = Builders<EmployeeLeaveBalance>.Filter.Where(lb =>
                lb.CompanyId == companyId && lb.Id == leaveBalance!.Id &&
                (!debitBalance || lb.Remaining >= leaveRequest.TotalDays) &&
                (!creditBalance || lb.Taken >= leaveRequest.TotalDays));
            var takenChange = -balanceChange;
            var balanceUpdate = Builders<EmployeeLeaveBalance>.Update
                .Inc(lb => lb.Remaining, balanceChange)
                .Inc(lb => lb.Taken, takenChange)
                .Inc(lb => lb.Version, 1)
                .Set(lb => lb.UpdatedDate, now);
            var balanceResult = await _employeeLeaveBalanceRepo.GetCollection().UpdateOneAsync(balanceFilter, balanceUpdate);
            if (balanceResult.ModifiedCount != 1)
                return new Result { Success = false, Message = debitBalance ? "The employee no longer has sufficient leave balance. Refresh and try again." : "The leave balance could not be restored. Please contact an administrator." };
            balanceChanged = true;
        }

        var leaveResult = await _leave.GetCollection().UpdateOneAsync(leaveFilter, leaveUpdate);
        if (leaveResult.ModifiedCount == 1)
            return new Result { Success = true };

        if (balanceChanged)
        {
            var compensation = Builders<EmployeeLeaveBalance>.Update
                .Inc(lb => lb.Remaining, -balanceChange)
                .Inc(lb => lb.Taken, balanceChange)
                .Set(lb => lb.UpdatedDate, DateTime.UtcNow);
            await _employeeLeaveBalanceRepo.GetCollection().UpdateOneAsync(
                Builders<EmployeeLeaveBalance>.Filter.Where(lb => lb.CompanyId == companyId && lb.Id == leaveBalance!.Id), compensation);
        }

        return new Result { Success = false, Message = "The leave request was changed by another reviewer. Refresh and try again." };
    }

    private async Task<bool> ValidateAttendanceStatusMapping(string companyId, string? code)
    {
        // Existing policies without a mapping remain readable/editable. New approvals
        // cannot reconcile until HR assigns a valid no-time attendance status.
        if (string.IsNullOrWhiteSpace(code)) return true;
        var normalized = code.Trim().ToUpperInvariant();
        return await _attendanceStatusRepo.Exist(x => x.CompanyId == companyId && x.Code == normalized && x.IsActive && !x.RequiresTime && x.IsAvailableForLeaveManagement);
    }

    private async Task<bool> HasActiveWorkFromHomeOverlap(string companyId, string userId, DateTime startDate, DateTime endDate, bool leaveIsHalfDay, string? leaveHalfDayPeriod)
    {
        var requests = await _workFromHomeRequests.GetAll(request =>
            request.CompanyId == companyId && request.UserId == userId &&
            (request.Status == "Pending" || request.Status == "Approved" || request.Status == "Returned") &&
            request.FromDate <= endDate && request.ToDate >= startDate);

        var db = _workFromHomeRequests.GetCollection()?.Database;
        if (db is not null)
        {
            var attendanceCol = db.GetCollection<AttendanceModel>("Attendance");
            var segmentsCol = db.GetCollection<AttendanceDaySegment>("AttendanceDaySegment");

            var startDay = startDate.Date;
            var endDay = endDate.Date.AddDays(1);

            var hybridDays = await attendanceCol.Find(x =>
                x.CompanyId == companyId && x.UserId == userId &&
                x.Date >= startDay && x.Date < endDay &&
                (x.Status == "WFH+WFO" || x.RemarkCode == "WFH_WFO_COMBINATION")).ToListAsync();

            if (hybridDays.Any())
            {
                return true;
            }

            if (leaveIsHalfDay && !string.IsNullOrWhiteSpace(leaveHalfDayPeriod))
            {
                var normPeriod = leaveHalfDayPeriod.Trim().ToUpperInvariant();
                var occupiedSegments = await segmentsCol.Find(x =>
                    x.CompanyId == companyId && x.UserId == userId &&
                    x.Date >= startDay && x.Date < endDay &&
                    x.Segment == normPeriod &&
                    (x.Status == "P" || x.Status == "WFH" || x.Status == "HD")).ToListAsync();

                if (occupiedSegments.Any())
                {
                    return true;
                }
            }
        }

        return requests.Any(wfh => LeaveConflictsWithWorkFromHome(leaveIsHalfDay, leaveHalfDayPeriod, wfh.DurationType));
    }

    private async Task AcquireEmployeeWfhClaimLock(IClientSessionHandle? session, string companyId, string userId)
    {
        var lockId = WorkFromHomeService.BuildQuotaLockId(companyId, userId);
        var db = _workFromHomeRequests.GetCollection()?.Database;
        if (db is null) return;
        var collection = db.GetCollection<BsonDocument>("WorkFromHomeQuotaLock");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", lockId);
        var update = Builders<BsonDocument>.Update
            .SetOnInsert("CompanyId", companyId)
            .SetOnInsert("UserId", userId)
            .Set("UpdatedAtUtc", DateTime.UtcNow)
            .Inc("LockVersion", 1L);
        if (session is not null)
            await collection.UpdateOneAsync(session, filter, update, new UpdateOptions { IsUpsert = true });
        else
            await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        var lockDocument = session is not null
            ? await collection.Find(session, filter).FirstOrDefaultAsync()
            : await collection.Find(filter).FirstOrDefaultAsync();
        if (lockDocument is null || lockDocument.GetValue("CompanyId", BsonNull.Value).AsString != companyId ||
            lockDocument.GetValue("UserId", BsonNull.Value).AsString != userId)
            throw new InvalidOperationException("WFH_QUOTA_LOCK_TENANT_MISMATCH");
    }

    // Full-day and mixed WFH statuses own the entire date. Opposite explicit halves
    // are allowed so an approved half-day leave can coexist with half-day WFH.
    private static bool LeaveConflictsWithWorkFromHome(bool leaveIsHalfDay, string? leaveHalfDayPeriod, string wfhDurationType)
    {
        if (!leaveIsHalfDay)
            return true;

        var wfhSegment = wfhDurationType switch
        {
            "FirstHalf" => "FIRST_HALF",
            "SecondHalf" => "SECOND_HALF",
            _ => null
        };

        return wfhSegment is null || string.Equals(leaveHalfDayPeriod, wfhSegment, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Result> SyncWorkFromHomePolicy(string companyId, LeavePolicy leavePolicy, WorkFromHomePolicySettingsRequest settings)
    {
        var existing = await _workFromHomePolicies.FirstOrDefault(x => x.CompanyId == companyId && x.LeavePolicyId == leavePolicy.Id);
        if (existing is null)
        {
            var otherTypedPolicy = await _workFromHomePolicies.FirstOrDefault(x => x.CompanyId == companyId && !string.IsNullOrEmpty(x.LeavePolicyId) && x.LeavePolicyId != leavePolicy.Id);
            if (otherTypedPolicy is not null)
                return new Result { Success = false, Message = "WFH_POLICY_ALREADY_CONFIGURED: This company already has a typed WFH policy. Update that policy instead of creating a second active WFH workflow." };
            existing = await _workFromHomePolicies.FirstOrDefault(x => x.CompanyId == companyId && string.IsNullOrEmpty(x.LeavePolicyId)) ?? new WorkFromHomePolicy { CompanyId = companyId };
        }

        existing.LeavePolicyId = leavePolicy.Id;
        existing.IsEnabled = leavePolicy.Status;
        existing.MinimumAdvanceNoticeHours = 0;
        existing.ManagerApprovalRequired = settings.ApprovalRequired;
        existing.RequireReason = settings.ReasonRequired;
        existing.RequireAttachment = settings.AttachmentRequired;
        var maxWorkingDays = 7;
        if (!settings.AllowOnWeeklyOff)
        {
            var workingDays = await _workingCalendar.CountWorkingDaysAsync(companyId, new DateOnly(2026, 10, 26), new DateOnly(2026, 11, 1));
            maxWorkingDays = Math.Max(1, Math.Min(7, workingDays));
        }
        existing.MaxDaysPerWeek = Math.Min(maxWorkingDays, Math.Max(1, settings.MaxDaysPerWeek ?? 1));
        existing.MaxDaysPerMonth = settings.MaxDaysPerMonth;
        existing.AllowHalfDay = settings.AllowFirstHalf || settings.AllowSecondHalf;
        existing.AllowMixedDay = settings.AllowMixedHalfDayLeave;
        existing.AllowOnWeeklyOff = settings.AllowOnWeeklyOff;
        existing.AllowOnHoliday = settings.AllowOnHoliday;
        existing.EffectiveFrom = null;
        existing.EffectiveTo = null;
        existing.ApplyToAllEmployees = true;
        var selectedUserIds = leavePolicy.ApplicableTo;
        if (selectedUserIds is { Count: > 0 })
        {
            existing.ApplyToAllEmployees = false;
            existing.ApplicableEmployeeIds = (await _employeeRepository.GetAll(
                employee => employee.CompanyId == companyId && employee.Status && selectedUserIds.Contains(employee.UserId)))
                .Select(employee => employee.EmployeeId)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }
        else
        {
            existing.ApplicableEmployeeIds = [];
        }
        existing.FullDayAttendanceStatusCode = settings.FullDayAttendanceStatusCode.Trim().ToUpperInvariant();
        existing.HalfDayAttendanceStatusCode = settings.HalfDayAttendanceStatusCode.Trim().ToUpperInvariant();
        existing.MixedAttendanceStatusCode = settings.MixedAttendanceStatusCode?.Trim().ToUpperInvariant() ?? string.Empty;
        existing.WfhWfoAttendanceStatusCode = (settings.WfhWfoAttendanceStatusCode ?? settings.MixedAttendanceStatusCode)?.Trim().ToUpperInvariant();
        existing.WfhHdAttendanceStatusCode = settings.WfhHdAttendanceStatusCode?.Trim().ToUpperInvariant();
        existing.FullDayMinimumHours = (settings.FullDayRequiredWorkingMinutes ?? 480) / 60m;
        existing.HalfDayMinimumHours = (settings.HalfDayRequiredWorkingMinutes ?? 240) / 60m;
        existing.SameDayCutoffTime = settings.SameDayCutoffTime;
        existing.SameDayGraceMinutes = settings.SameDayGraceMinutes;
        existing.AllowLateApplicationWithApproval = settings.AllowLateApplicationWithApproval;
        existing.AllowedDaysOfWeek = settings.AllowedDaysOfWeek ?? [];
        existing.BlockedDaysOfWeek = settings.BlockedDaysOfWeek ?? [];
        existing.UpdatedDate = DateTime.UtcNow;
        return string.IsNullOrWhiteSpace(existing.PolicyId)
            ? await _workFromHomePolicies.AddOne(existing)
            : await _workFromHomePolicies.Update(Builders<WorkFromHomePolicy>.Filter.Where(x => x.CompanyId == companyId && x.PolicyId == existing.PolicyId), existing);
    }

    private async Task<bool> ValidatePolicyShape(string companyId, EnumsHelper.LeavePolicyType policyType, string? fullDayStatusCode, string? halfDayStatusCode, bool halfDayAllowed, WorkFromHomePolicySettingsRequest? wfh)
    {
        if (policyType is EnumsHelper.LeavePolicyType.Leave or EnumsHelper.LeavePolicyType.UnpaidLeave)
            return wfh is null && await ValidateAttendanceStatusMapping(companyId, fullDayStatusCode) && (!halfDayAllowed || await ValidateAttendanceStatusMapping(companyId, halfDayStatusCode));

        var maxWorkingDays = 7;
        if (wfh != null && !wfh.AllowOnWeeklyOff)
        {
            var workingDays = await _workingCalendar.CountWorkingDaysAsync(companyId, new DateOnly(2026, 10, 26), new DateOnly(2026, 11, 1));
            maxWorkingDays = Math.Max(1, Math.Min(7, workingDays));
        }

        if (policyType != EnumsHelper.LeavePolicyType.WorkFromHome || wfh is null || !wfh.AllowFullDay ||
            wfh.MaxDaysPerWeek < 1 || wfh.MaxDaysPerWeek > maxWorkingDays || wfh.MaxDaysPerMonth is < 1 or > 31 || wfh.FullDayRequiredWorkingMinutes is < 0 ||
            wfh.HalfDayRequiredWorkingMinutes is < 0 || wfh.BreakMinutes < 0 ||
            wfh.ApproverStrategy is not ("REPORTING_MANAGER" or "DEPARTMENT_HEAD" or "HR_ADMIN"))
            return false;

        var codes = new[] { wfh.FullDayAttendanceStatusCode, wfh.HalfDayAttendanceStatusCode, wfh.MixedAttendanceStatusCode, wfh.WfhWfoAttendanceStatusCode, wfh.WfhHdAttendanceStatusCode }
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim().ToUpperInvariant()).Distinct().ToList();
        if (codes.Count == 0 || (wfh.AllowFirstHalf || wfh.AllowSecondHalf) && string.IsNullOrWhiteSpace(wfh.HalfDayAttendanceStatusCode) ||
            wfh.AllowMixedHalfDayLeave && new[] { wfh.MixedAttendanceStatusCode ?? wfh.WfhWfoAttendanceStatusCode, wfh.WfhHdAttendanceStatusCode }.Any(string.IsNullOrWhiteSpace))
            return false;
        return (await _attendanceStatusRepo.GetAll(x => x.CompanyId == companyId && x.IsActive && codes.Contains(x.Code)))
            .Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == codes.Count;
    }

    private static void NormalizeLeaveAttendanceMappings(LeavePolicyRequest policy)
    {
        if (policy.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome && policy.WorkFromHome is not null)
        {
            policy.WorkFromHome.FullDayAttendanceStatusCode = NormalizeStatusCode(policy.WorkFromHome.FullDayAttendanceStatusCode) ?? string.Empty;
            policy.WorkFromHome.HalfDayAttendanceStatusCode = NormalizeStatusCode(policy.WorkFromHome.HalfDayAttendanceStatusCode) ?? string.Empty;
            policy.WorkFromHome.MixedAttendanceStatusCode = NormalizeStatusCode(policy.WorkFromHome.MixedAttendanceStatusCode);
            policy.HalfDayAllowed = policy.WorkFromHome.AllowFirstHalf || policy.WorkFromHome.AllowSecondHalf;
            policy.FullDayAttendanceStatusCode = policy.WorkFromHome.FullDayAttendanceStatusCode;
            policy.HalfDayAttendanceStatusCode = policy.HalfDayAllowed ? policy.WorkFromHome.HalfDayAttendanceStatusCode : null;
            policy.AttendanceStatusCode = policy.FullDayAttendanceStatusCode;
            return;
        }
        policy.FullDayAttendanceStatusCode = NormalizeStatusCode(policy.FullDayAttendanceStatusCode ?? policy.AttendanceStatusCode);
        policy.HalfDayAttendanceStatusCode = policy.HalfDayAllowed ? NormalizeStatusCode(policy.HalfDayAttendanceStatusCode) : null;
        policy.AttendanceStatusCode = policy.FullDayAttendanceStatusCode;
    }

    private static void NormalizeLeaveAttendanceMappings(UpdateLeavePolicyRequest policy)
    {
        if (policy.PolicyType == EnumsHelper.LeavePolicyType.WorkFromHome && policy.WorkFromHome is not null)
        {
            policy.WorkFromHome.FullDayAttendanceStatusCode = NormalizeStatusCode(policy.WorkFromHome.FullDayAttendanceStatusCode) ?? string.Empty;
            policy.WorkFromHome.HalfDayAttendanceStatusCode = NormalizeStatusCode(policy.WorkFromHome.HalfDayAttendanceStatusCode) ?? string.Empty;
            policy.WorkFromHome.MixedAttendanceStatusCode = NormalizeStatusCode(policy.WorkFromHome.MixedAttendanceStatusCode);
            policy.HalfDayAllowed = policy.WorkFromHome.AllowFirstHalf || policy.WorkFromHome.AllowSecondHalf;
            policy.FullDayAttendanceStatusCode = policy.WorkFromHome.FullDayAttendanceStatusCode;
            policy.HalfDayAttendanceStatusCode = policy.HalfDayAllowed ? policy.WorkFromHome.HalfDayAttendanceStatusCode : null;
            policy.AttendanceStatusCode = policy.FullDayAttendanceStatusCode;
            return;
        }
        policy.FullDayAttendanceStatusCode = NormalizeStatusCode(policy.FullDayAttendanceStatusCode ?? policy.AttendanceStatusCode);
        policy.HalfDayAttendanceStatusCode = policy.HalfDayAllowed ? NormalizeStatusCode(policy.HalfDayAttendanceStatusCode) : null;
        policy.AttendanceStatusCode = policy.FullDayAttendanceStatusCode;
    }

    private static string? NormalizeStatusCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    public async Task<Result<LeaveRequestSummaryResponseDto>> GetLeaveRequestSummary()
    {
        Result<LeaveRequestSummaryResponseDto> result = new();
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        // get last 30 days leave request
        var fromDate = DateTime.UtcNow.AddDays(-30);
        Expression<Func<LeaveRequest, bool>> expression = lr => lr.CompanyId == companyId && lr.CreatedDate.HasValue && lr.CreatedDate.Value.Date >= fromDate.Date;
        var currentMonthLeaveRequest = await _leave.GetAll(expression);
        Dictionary<int, int> leaveStatusSummary = new();
        currentMonthLeaveRequest.GroupBy(lr => lr.Status).ForEach(lr =>
        {
            leaveStatusSummary.Add((int)lr.Key, lr.Count());
        });

        var leavePolicyIds = currentMonthLeaveRequest.Select(lr => lr.LeavePolicyId).Distinct().ToList();
        var leavePolicyMap = (await _leavePolicyRepo.GetAll(lp => lp.CompanyId == companyId && leavePolicyIds.Contains(lp.Id)))
            .ToDictionary(lp => lp.Id, lp => lp);
        var leaveTypeSummary = currentMonthLeaveRequest.GroupBy(lr => lr.LeavePolicyId).Select(group =>
        {
            leavePolicyMap.TryGetValue(group.Key, out var leavePolicy);
            return new LeaveTypeSummary()
            {
                LeaveTypeName = leavePolicy?.Name ?? string.Empty,
                StatusValues = group.GroupBy(lr => lr.Status).ToDictionary(ls => (int)ls.Key, ls => ls.Count()),
                LeaveTypeCode = leavePolicy?.Code ?? string.Empty,
            };
        }).ToList();

        result.MethodResult = new()
        {
            TotalRequests = currentMonthLeaveRequest.Count(),
            LeaveStatusSummary = leaveStatusSummary,
            LeaveTypeSummary = leaveTypeSummary,
        };
        return result;
    }

    public async Task<Result<MonthlyTakenLeaveSummaryResponseDto>> GetMonthlyTakenLeaveSummary(int? year)
    {
        Result<MonthlyTakenLeaveSummaryResponseDto> result = new();
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var leaveRequests = (await _leave.GetAll(lr => lr.CompanyId == companyId && lr.Status == EnumsHelper.LeaveRequestStatus.Accepted && lr.StartDate.Year == year))
            .GroupBy(lr => lr.StartDate.Month)
            .Select(g => new MonthlyTakenLeaveSummaryResponseDto
            {
                Month = g.Key,
                TotalLeavesTaken = g.Count()
            }).ToList();

        // Fill missing months with 0
        result.MethodResults = Enumerable.Range(1, 12).Select(m => new MonthlyTakenLeaveSummaryResponseDto
        {
            Month = m,
            TotalLeavesTaken = leaveRequests.FirstOrDefault(x => x.Month == m)?.TotalLeavesTaken ?? 0
        }).ToList();

        return result;
    }


    // leave balance services
    public async Task<Result> UpdateEmployeeLeaveBalance(List<LeaveBalanceRequestDto> leaveBalanceRequestDto)
    {
        Result result = new();
        if (leaveBalanceRequestDto is null || leaveBalanceRequestDto.Count == 0) return new Result { Success = false, Message = "At least one leave allocation change is required." };
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var employeeIds = leaveBalanceRequestDto.Select(x => x.EmployeeId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (employeeIds.Count != 1 || !await _employeeRepository.Exist(x => x.CompanyId == companyId && x.UserId == employeeIds[0] && x.Status && !x.IsDeleted && (x.Lifecycle == null || x.Lifecycle.EmploymentStatus == null || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended)))
            return new Result { Success = false, Message = "The selected employee is not active in this company." };
        foreach (var balanceItem in leaveBalanceRequestDto)
        {
            if (string.IsNullOrWhiteSpace(balanceItem.LeavePolicyId) || balanceItem.TotalAllocated < 0)
                return new Result { Success = false, Message = "Leave policy and a non-negative total allocation are required." };
            if (balanceItem.Remove)
            {
                if (!string.IsNullOrWhiteSpace(balanceItem.LeaveBalanceId))
                {
                    var existing = await _employeeLeaveBalanceRepo.FirstOrDefault(x => x.CompanyId == companyId && x.Id == balanceItem.LeaveBalanceId && x.UserId == balanceItem.EmployeeId && x.LeavePolicyId == balanceItem.LeavePolicyId);
                    if (existing is null) return new Result { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "The requested leave allocation was not found." };
                    if (existing.Taken > 0) return new Result { Success = false, Message = "Leave allocation cannot be removed after leave has been taken." };
                    var expectedVersion = balanceItem.ExpectedVersion ?? existing.Version;
                    var deleteResult = await _employeeLeaveBalanceRepo.GetCollection().DeleteOneAsync(
                        x => x.CompanyId == companyId && x.Id == balanceItem.LeaveBalanceId &&
                             x.UserId == balanceItem.EmployeeId && x.LeavePolicyId == balanceItem.LeavePolicyId &&
                             x.Version == expectedVersion && x.Taken == 0);
                    if (deleteResult.DeletedCount != 1)
                        return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "This leave allocation was updated by another user. Refresh and try again." };
                }
                continue;
            }
            if (string.IsNullOrEmpty(balanceItem.LeaveBalanceId))
            {
                // add balance 
                var leavePolicy = await _leavePolicyRepo.FirstOrDefault(lp => lp.CompanyId == companyId && lp.Id == balanceItem.LeavePolicyId && lp.Status && lp.PolicyType == EnumsHelper.LeavePolicyType.Leave);
                if (leavePolicy == null)
                {
                    result.Success = false;
                    result.Message = "The selected leave policy is no longer available for allocation.";
                    return result;
                }
                if (await _employeeLeaveBalanceRepo.Exist(balance => balance.CompanyId == companyId && balance.UserId == balanceItem.EmployeeId && balance.LeavePolicyId == balanceItem.LeavePolicyId))
                    return new Result { Success = false, Message = "An allocation already exists for this employee and leave policy. Refresh and try again." };
                var requestedTotal = balanceItem.TotalAllocated;
                if (leavePolicy.MaxBalance.HasValue && requestedTotal > leavePolicy.MaxBalance.Value)
                {
                    result.Success = false;
                    result.Message = "The requested remaining balance exceeds this policy's maximum allocation.";
                    result.StatusCode = CustomStatusCode.LeaveBalanceLimitExceed;
                    return result;
                }
                var employeeBalance = new EmployeeLeaveBalance()
                {
                    CompanyId = companyId,
                    UserId = balanceItem.EmployeeId,
                    LeavePolicyId = balanceItem.LeavePolicyId,
                    TotalAllocated = requestedTotal,
                    Taken = 0,
                    Remaining = requestedTotal,
                    IsManualAllocation = true,
                };
                result = await _employeeLeaveBalanceRepo.AddOne(employeeBalance);
            }
            else
            {
                // update balnce
                var leaveBalance = await _employeeLeaveBalanceRepo.FirstOrDefault(elb =>
                    elb.CompanyId == companyId && elb.Id == balanceItem.LeaveBalanceId &&
                    elb.UserId == balanceItem.EmployeeId && elb.LeavePolicyId == balanceItem.LeavePolicyId);
                if (leaveBalance == null)
                {
                    result.Success = false;
                    result.StatusCode = StatusCodes.Status404NotFound;
                    result.Message = "The requested leave allocation was not found.";
                    return result;
                }
                var leavePolicy = await _leavePolicyRepo.FirstOrDefault(lp => lp.CompanyId == companyId && lp.Id == leaveBalance.LeavePolicyId && lp.Status && lp.PolicyType == EnumsHelper.LeavePolicyType.Leave);
                if (leavePolicy == null)
                {
                    result.Success = false;
                    result.Message = "The selected leave policy is no longer available for allocation.";
                    return result;
                }
                var requestedTotal = balanceItem.TotalAllocated;
                if (!TryCalculateAllocation(requestedTotal, leaveBalance.Taken, out var remaining))
                    return new Result { Success = false, Message = "Total allocation cannot be less than leave already taken." };
                if (leavePolicy.MaxBalance.HasValue && requestedTotal > leavePolicy.MaxBalance.Value)
                {
                    result.Success = false;
                    result.Message = "The requested remaining balance exceeds this policy's maximum allocation.";
                    result.StatusCode = CustomStatusCode.LeaveBalanceLimitExceed;
                    return result;
                }

                var expectedVersion = balanceItem.ExpectedVersion ?? leaveBalance.Version;
                var update = Builders<EmployeeLeaveBalance>.Update
                    .Set(lb => lb.TotalAllocated, requestedTotal)
                    .Set(lb => lb.Remaining, remaining)
                    .Set(lb => lb.IsManualAllocation, true)
                    .Set(lb => lb.UpdatedDate, DateTime.UtcNow)
                    .Inc(lb => lb.Version, 1);
                var updateResult = await _employeeLeaveBalanceRepo.GetCollection().UpdateOneAsync(
                    lb => lb.CompanyId == companyId && lb.Id == balanceItem.LeaveBalanceId &&
                          lb.UserId == balanceItem.EmployeeId && lb.LeavePolicyId == balanceItem.LeavePolicyId &&
                          lb.Version == expectedVersion,
                    update);
                result = new Result
                {
                    Success = updateResult.ModifiedCount == 1,
                    StatusCode = updateResult.ModifiedCount == 1 ? StatusCodes.Status200OK : StatusCodes.Status409Conflict,
                    Message = updateResult.ModifiedCount == 1 ? null : "This leave allocation was updated by another user. Refresh and try again."
                };
                if (!result.Success) return result;
            }
        }
        return new Result
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Leave allocation updated successfully."
        };
    }

    public async Task<Result<EmployeeLeaveBalanceResponseDto>> GetEmployeeLeaveBalance(string employeeId)
    {
        Result<EmployeeLeaveBalanceResponseDto> result = new() { Success = false };
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        bool isEmpExist = await _employeeRepository.Exist(x =>
            x.CompanyId == companyId && x.UserId == employeeId && x.Status);
        if (!isEmpExist) return result;

        // get employee balance

        IEnumerable<EmployeeLeaveBalance> employeeLeaveBalances = await _employeeLeaveBalanceRepo.GetAll(elb =>
            elb.CompanyId == companyId && elb.UserId == employeeId);
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(lp =>
            lp.CompanyId == companyId && lp.Status && lp.PolicyType == EnumsHelper.LeavePolicyType.Leave);
        var lifecyclePolicyIds = _lifecyclePolicyRepository is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : (await _lifecyclePolicyRepository.GetAll(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted && !string.IsNullOrWhiteSpace(x.LeavePolicyId)))
                .Select(x => x.LeavePolicyId!)
                .ToHashSet(StringComparer.Ordinal);

        List<EmployeeLeaveBalanceResponseDto> empLeaveBalanceResult = (
            from empLeaveBalance in employeeLeaveBalances
            join leavePolicy in leavePolicies on empLeaveBalance.LeavePolicyId equals leavePolicy.Id
            where !lifecyclePolicyIds.Contains(leavePolicy.Id)
            select new EmployeeLeaveBalanceResponseDto
            {
                LeaveBalanceId = empLeaveBalance.Id,
                LeavePolicyId = leavePolicy.Id,
                TotalAllocated = empLeaveBalance.TotalAllocated,
                Taken = empLeaveBalance.Taken,
                Remaining = empLeaveBalance.Remaining,
                MaxBalance = leavePolicy.MaxBalance,
                Version = empLeaveBalance.Version,
                Name = leavePolicy.Name,
                Code = leavePolicy.Code,
                Description = leavePolicy.Description,
                Paid = leavePolicy.Paid,
                MinNoticeDays = leavePolicy.MinNoticeDays,
                HalfDayAllowed = leavePolicy.HalfDayAllowed,
                HolidayInclusive = leavePolicy.HolidayInclusive,
                WeekendInclusive = leavePolicy.WeekendInclusive,
            }
        ).ToList();

        result.MethodResults = empLeaveBalanceResult;
        result.Success = true;
        result.TotalRecords = empLeaveBalanceResult.Count;
        return result;
    }
    public async Task<Result<AllEmployeeLeaveBalance>> GetAllEmployeeLeaveBalances(LeaveBalanceFilter filter, string companyId)
    {
        Result<AllEmployeeLeaveBalance> result = new() { Success = false };

        // This endpoint is a standard, server-paginated listing. Keep page
        // numbers one-based at the API boundary and only accept the sizes the
        // Leave Balance UI exposes.
        var pageNo = Math.Max(1, filter.PageNo);
        var pageSize = filter.PageSize is 10 or 25 or 50 ? filter.PageSize : 10;
        var lifecycleBalanceIds = _lifecyclePolicyRepository is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : (await _lifecyclePolicyRepository.GetAll(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted && !string.IsNullOrWhiteSpace(x.LeavePolicyId)))
                .Select(x => x.LeavePolicyId!)
                .ToHashSet(StringComparer.Ordinal);

        // Apply every employee-level filter before counting or paging. The
        // authenticated company ID remains part of both the employee and
        // balance filters; callers cannot select a different tenant.
        Expression<Func<EmpUser, bool>> empExpression = string.IsNullOrEmpty(filter.EmployeeName)
            ? u => u.CompanyId == companyId && u.Status && !u.IsDeleted
            : u => u.CompanyId == companyId && u.Status && !u.IsDeleted &&
                (u.FirstName.Contains(filter.EmployeeName, StringComparison.CurrentCultureIgnoreCase) || u.LastName.Contains(filter.EmployeeName, StringComparison.CurrentCultureIgnoreCase));

        if (filter.LeavePolicies is { Count: > 0 })
        {
            var employeeIdsWithSelectedPolicies = (await _employeeLeaveBalanceRepo.GetAll(lb =>
                    lb.CompanyId == companyId && filter.LeavePolicies.Contains(lb.LeavePolicyId) && !lifecycleBalanceIds.Contains(lb.LeavePolicyId)))
                .Select(lb => lb.UserId)
                .Distinct()
                .ToList();

            empExpression = empExpression.And(u => employeeIdsWithSelectedPolicies.Contains(u.UserId));
        }

        var employeeCollection = _employeeRepository.GetCollection();
        var employeeFilter = Builders<EmpUser>.Filter.Where(empExpression);
        var totalRecords = (int)await employeeCollection.CountDocumentsAsync(employeeFilter);

        var empUsers = totalRecords == 0
            ? []
            : await employeeCollection.Find(employeeFilter)
                .Sort(Builders<EmpUser>.Sort.Ascending(e => e.FirstName).Ascending(e => e.LastName).Ascending(e => e.UserId))
                .Skip((pageNo - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

        List<string> empIds = empUsers.Select(e => e.UserId).ToList();

        // get employee job roles id
        List<string> empJobRoleId = empUsers.Where(emp => emp.JobRole != null).Select(emp => emp.JobRole).Distinct().ToList();
        IEnumerable<JobTitles> jobTitles = await _jobTitleRepo.GetAll(jr => jr.CompanyId == companyId && empJobRoleId.Contains(jr.JobTitleId));

        // get employee leave balances
        Expression<Func<EmployeeLeaveBalance, bool>> leaveBalanceExpression;
        if (filter.LeavePolicies != null && filter.LeavePolicies.Count > 0)
        {
            leaveBalanceExpression = lb => lb.CompanyId == companyId && empIds.Contains(lb.UserId) && filter.LeavePolicies.Contains(lb.LeavePolicyId) && !lifecycleBalanceIds.Contains(lb.LeavePolicyId);
        }
        else
        {
            leaveBalanceExpression = lb => lb.CompanyId == companyId && empIds.Contains(lb.UserId) && !lifecycleBalanceIds.Contains(lb.LeavePolicyId);
        }
        IEnumerable<EmployeeLeaveBalance> employeeLeaveBalances = await _employeeLeaveBalanceRepo.GetAll(leaveBalanceExpression);

        // get leave policies details
        List<string> leavePolicyIds = employeeLeaveBalances.Select(elb => elb.LeavePolicyId).Distinct().ToList();
        // Employee balance records are retained for history when a policy is
        // deactivated, but inactive policies must not be exposed in the Leave
        // Balance overview.
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(lp =>
            leavePolicyIds.Contains(lp.Id) && lp.CompanyId == companyId && lp.Status);

        var empLeaveBalanceResult = empUsers.Select(emp =>
            {
                var balances = employeeLeaveBalances.Where(lb => lb.UserId == emp.UserId).Select(lb =>
                {
                    LeavePolicy? policy = leavePolicies.FirstOrDefault(lp => lp.Id == lb.LeavePolicyId);
                    if (policy is null) return null;

                    return new LeaveBalanceDetail
                    {
                        LeaveBalanceId = lb.Id,
                        LeavePolicyId = lb.LeavePolicyId,
                        LeavePolicyName = policy != null ? policy.Name : string.Empty,
                        LeavePolicyCode = policy != null ? policy.Code : string.Empty,
                        TotalAllocated = lb.TotalAllocated,
                        Taken = lb.Taken,
                        Remaining = lb.Remaining,
                        Version = lb.Version,
                        UsedLeave = lb.Taken
                    };
                }).Where(balance => balance is not null).Select(balance => balance!).ToList();

                JobTitles? jobTitle = emp.JobRole != null ? jobTitles.FirstOrDefault(jt => jt.JobTitleId == emp.JobRole) : null;

                return new AllEmployeeLeaveBalance
                {
                    EmployeeId = emp.UserId,
                    EmployeeName = emp.FirstName + " " + emp.LastName,
                    EmployeeCode = emp.EmployeeId,
                    Designation = jobTitle?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label),
                    ProfilePicture = Common.GetEmployeeImageUrl(emp.ProfileUrl),
                    LeaveBalances = balances
                };
            }).ToList();
        result.MethodResults = empLeaveBalanceResult;
        result.TotalRecords = totalRecords;
        result.Success = true;
        return result;
    }

    private async Task LeaveNotification(
        LeaveRequest leaveDomain,
        EnumsHelper.LeaveRequestStatus status,
        string companyId,
        string actorUserId,
        bool sendEmail = true)
    {
        try
        {
            var employee = await _employeeRepository.FirstOrDefault(x =>
                x.CompanyId == companyId && x.UserId == leaveDomain.EmployeeId);
            if (employee is null) return;
            var policy = await _leavePolicyRepo.FirstOrDefault(x => x.CompanyId == companyId && x.Id == leaveDomain.LeavePolicyId);
            var jobRole = string.IsNullOrWhiteSpace(employee.JobRole) ? null : await _jobTitleRepo.FirstOrDefault(x =>
                x.CompanyId == companyId && x.JobTitleId == employee.JobRole && !x.IsDeleted);
            var reviewer = string.IsNullOrWhiteSpace(leaveDomain.ReviewedBy) ? null : await _employeeRepository.FirstOrDefault(x =>
                x.CompanyId == companyId && x.UserId == leaveDomain.ReviewedBy && !x.IsDeleted);
            var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
            var leaveType = policy is null ? "Leave" : $"{policy.Name} ({policy.Code})";
            var duration = leaveDomain.IsHalfDay
                ? $"Half Day - {(string.Equals(leaveDomain.HalfDayPeriod, "SECOND_HALF", StringComparison.OrdinalIgnoreCase) ? "Second Half" : "First Half")}" : "Full Day";
            var dateRange = leaveDomain.StartDate.Date == leaveDomain.EndDate.Date
                ? leaveDomain.StartDate.ToString("dd MMM yyyy")
                : $"{leaveDomain.StartDate:dd MMM yyyy} - {leaveDomain.EndDate:dd MMM yyyy}";
            var detailRows = new List<(string Label, string? Value)>
            {
                ("Employee", employeeName), ("Employee ID", employee.EmployeeId),
                ("Job role", jobRole?.Titles?.FirstOrDefault()?.Label), ("Leave type", leaveType),
                ("Duration", duration), ("Leave date", dateRange), ("Total", $"{leaveDomain.TotalDays:0.##} day(s)"),
                ("Reason", leaveDomain.Reason), ("Status", status.ToString()),
                ("Requested on", leaveDomain.CreatedDate?.ToLocalTime().ToString("dd MMM yyyy hh:mm tt")),
            };
            if (reviewer is not null && status != EnumsHelper.LeaveRequestStatus.WithDrawn)
                detailRows.Add((status == EnumsHelper.LeaveRequestStatus.Accepted ? "Approved by" : "Reviewed by", $"{reviewer.FirstName} {reviewer.LastName}".Trim()));
            else if (status == EnumsHelper.LeaveRequestStatus.WithDrawn)
                detailRows.Add(("Withdrawn by", actorUserId == leaveDomain.EmployeeId ? employeeName : $"{reviewer?.FirstName} {reviewer?.LastName}".Trim()));
            if (leaveDomain.ReviewedAt.HasValue)
                detailRows.Add((status == EnumsHelper.LeaveRequestStatus.WithDrawn ? "Withdrawn date" : "Decision date", leaveDomain.ReviewedAt.Value.ToLocalTime().ToString("dd MMM yyyy hh:mm tt")));
            if (!string.IsNullOrWhiteSpace(leaveDomain.Comment))
                detailRows.Add((status == EnumsHelper.LeaveRequestStatus.WithDrawn ? "Remarks" : "Approver remarks", leaveDomain.Comment));
            string detailsHtml = "<table role=\"presentation\" style=\"border-collapse:collapse;width:100%;max-width:620px\">" + string.Concat(detailRows.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => $"<tr><td style=\"padding:7px 12px 7px 0;color:#5f6b7a;font-weight:600;vertical-align:top\">{WebUtility.HtmlEncode(x.Label)}</td><td style=\"padding:7px 0;vertical-align:top\">{WebUtility.HtmlEncode(x.Value)}</td></tr>")) + "</table>";

            List<EmpUser> recipients;
            string title;
            string body;
            EnumsHelper.NotificationTypes notificationType;
            EnumsHelper.MailType mailType;

            if (status == EnumsHelper.LeaveRequestStatus.Pending)
            {
                var roleIds = (await _roleRepository.GetAll(x =>
                    x.CompanyId == companyId &&
                    (x.RoleType == Convert.ToInt32(EnumsHelper.Roles.Administrator) || x.RoleType == Convert.ToInt32(EnumsHelper.Roles.HR) || x.RoleType == Convert.ToInt32(EnumsHelper.Roles.HRExecutive))))
                    .Select(x => x.RolesId)
                    .ToHashSet();
                recipients = (await _employeeRepository.GetAll(x =>
                        x.CompanyId == companyId && x.Status && roleIds.Contains(x.RoleId)))
                    .Where(x => x.UserId != leaveDomain.EmployeeId)
                    .GroupBy(x => x.UserId)
                    .Select(x => x.First())
                    .ToList();
                title = $"Leave Request - {employeeName} - {leaveType}";
                body = $"{employeeName} has submitted a leave request for {dateRange}.";
                notificationType = EnumsHelper.NotificationTypes.LeaveRequest;
                mailType = EnumsHelper.MailType.LeaveMailToHR;
            }
            else if (status == EnumsHelper.LeaveRequestStatus.WithDrawn)
            {
                bool isSelfWithdrawal = actorUserId == leaveDomain.EmployeeId;
                if (isSelfWithdrawal)
                {
                    // When an employee withdraws their own leave request, notify HR and Admin managers
                    // instead of sending an erroneous "Leave Rejected" message to the employee.
                    var roleIds = (await _roleRepository.GetAll(x =>
                        x.CompanyId == companyId &&
                        (x.RoleType == Convert.ToInt32(EnumsHelper.Roles.Administrator) || x.RoleType == Convert.ToInt32(EnumsHelper.Roles.HR) || x.RoleType == Convert.ToInt32(EnumsHelper.Roles.HRExecutive))))
                        .Select(x => x.RolesId)
                        .ToHashSet();
                    recipients = (await _employeeRepository.GetAll(x =>
                            x.CompanyId == companyId && x.Status && roleIds.Contains(x.RoleId)))
                        .Where(x => x.UserId != leaveDomain.EmployeeId)
                        .GroupBy(x => x.UserId)
                        .Select(x => x.First())
                        .ToList();
                    title = $"Leave Request Withdrawn - {employeeName} - {leaveType}";
                    body = $"{employeeName} has withdrawn their leave request for {dateRange}.";
                    notificationType = EnumsHelper.NotificationTypes.Notice;
                    mailType = EnumsHelper.MailType.LeaveMailToHR;
                }
                else
                {
                    // When Admin/HR cancels an employee's leave request
                    if (!await _middlewareService.IsUserNotificationPreferenceEnabled(
                            leaveDomain.EmployeeId, EnumsHelper.NotificationPreferenceType.LeaveStatusUpdate)) return;
                    recipients = [employee];
                    var cancellerName = reviewer != null ? $"{reviewer.FirstName} {reviewer.LastName}".Trim() : "an administrator";
                    title = $"Leave Request Cancelled - {leaveType} - {dateRange}";
                    body = $"Your leave request from {leaveDomain.StartDate:dd MMM yyyy} to {leaveDomain.EndDate:dd MMM yyyy} has been cancelled by {cancellerName}.";
                    notificationType = EnumsHelper.NotificationTypes.Notice;
                    mailType = EnumsHelper.MailType.LeaveReplyMail;
                }
            }
            else
            {
                if (!await _middlewareService.IsUserNotificationPreferenceEnabled(
                        leaveDomain.EmployeeId, EnumsHelper.NotificationPreferenceType.LeaveStatusUpdate)) return;
                recipients = [employee];
                var approved = status == EnumsHelper.LeaveRequestStatus.Accepted;
                title = $"Leave {(approved ? "Approved" : "Rejected")} - {leaveType} - {dateRange}";
                body = $"Your leave request from {leaveDomain.StartDate:dd MMM yyyy} to {leaveDomain.EndDate:dd MMM yyyy} has been {(approved ? "approved" : "rejected")}.";
                notificationType = approved
                    ? EnumsHelper.NotificationTypes.LeaveRequestApproved
                    : EnumsHelper.NotificationTypes.LeaveRequestReject;
                mailType = EnumsHelper.MailType.LeaveReplyMail;
            }

            if (recipients.Count == 0) return;
            var notification = new Notifications
            {
                NotificationId = Guid.NewGuid().ToString(),
                CompanyId = companyId,
                CreatedBy = actorUserId,
                TargetId = leaveDomain.LeaveRequestId,
                Title = title,
                Body = body,
                CreatedDateTime = DateTime.UtcNow,
                NotificationType = notificationType,
            };
            if (!(await _notificationsRepo.AddOne(notification)).Success) return;

            var userNotifications = recipients.Select(user => new UserNotifications
            {
                UserNotificationId = Guid.NewGuid().ToString(),
                CompanyId = companyId,
                UserId = user.UserId,
                NotificationId = notification.NotificationId,
                IsRead = false,
                CreatedDateTime = DateTime.UtcNow,
                IsDeleted = false,
            }).ToList();
            // Persist the inbox record before attempting optional real-time delivery.
            // A disconnected SignalR client must not prevent email delivery.
            await _userNotificationsRepo.AddMany(userNotifications);
            try
            {
                await Task.WhenAll(userNotifications.Select(item => _notificationService.SendNotificationToUser(item.UserId,
                    new NotificationViewModel
                    {
                        Title = title,
                        Body = body,
                        IsRead = false,
                        SentBy = actorUserId,
                        SentDateTime = DateTime.UtcNow,
                        TargetId = leaveDomain.LeaveRequestId,
                        NotificationTypes = notificationType,
                        UserNotificationId = item.UserNotificationId,
                    })));
            }
            catch
            {
                // The notification is already persisted and will be visible on next profile load.
            }

            if (!sendEmail) return;

            var emailBody = $"<div style=\"font-family:Arial,sans-serif;max-width:620px;margin:0 auto;color:#202938\"><h2 style=\"margin:0 0 12px\">{WebUtility.HtmlEncode(title)}</h2><p style=\"margin:0 0 16px\">{WebUtility.HtmlEncode(body)}</p>{detailsHtml}</div>";
            await Task.WhenAll(recipients
                .Where(user => user.IsEmailVerified && !string.IsNullOrWhiteSpace(user.Email))
                .Select(user => _middlewareService.EmailSendAndSave(new EmpEmailLogs
                {
                    CompanyId = companyId,
                    UserTo = user.UserId,
                    UserFrom = actorUserId,
                    Email = user.Email,
                    Subject = title,
                    Body = emailBody,
                    EmailLogType = mailType,
                })));
        }
        catch
        {
            // Notification delivery must not roll back a leave request that is already saved.
        }
    }

    public async Task EmployeeLeaveBalanceAccrual()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // get all active leavePolicy with accrual monthly or yearly
        IEnumerable<LeavePolicy> leavePolicies = await _leavePolicyRepo.GetAll(lp =>
            lp.Status && lp.PolicyType == EnumsHelper.LeavePolicyType.Leave && (lp.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Monthly || lp.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Yearly),
            withDefaultFilter: false
        );
        if (!leavePolicies.Any()) return;
        foreach (var policy in leavePolicies)
        {
            if (await IsLifecycleLinkedPolicyAsync(policy.CompanyId, policy.Id)) continue;
            // get all active employees with leave policy
            IEnumerable<EmployeeLeaveBalance> employeeLeaveBalances = await _employeeLeaveBalanceRepo.GetAll(elb => elb.CompanyId == policy.CompanyId && elb.LeavePolicyId == policy.Id, withDefaultFilter: false);
            if (policy.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Monthly && employeeLeaveBalances.Any())
            {
                foreach (EmployeeLeaveBalance empLeave in employeeLeaveBalances)
                {
                    // check if leave is already credited for this month 
                    if (!empLeave.IsManualAllocation && (!empLeave.LastAccrual.HasValue || empLeave.LastAccrual.Value < monthStart))
                    {
                        var availableSpace = policy.MaxBalance.HasValue ? Math.Max(0, policy.MaxBalance.Value - empLeave.Remaining) : policy.AccrualAmount;
                        var actualCredit = policy.MaxBalance.HasValue ? Math.Min(policy.AccrualAmount, availableSpace) : policy.AccrualAmount;
                        Expression<Func<EmployeeLeaveBalance, bool>> expression = elb => elb.CompanyId == policy.CompanyId && elb.Id == empLeave.Id && elb.Version == empLeave.Version && !elb.IsManualAllocation && (!elb.LastAccrual.HasValue || elb.LastAccrual.Value < monthStart);
                        await _employeeLeaveBalanceRepo.UpdateMany(expression, Builders<EmployeeLeaveBalance>.Update
                        .Inc(b => b.TotalAllocated, actualCredit)
                        .Inc(b => b.Remaining, actualCredit)
                        .Inc(b => b.Version, 1)
                        .Set(b => b.LastAccrual, now)
                        .Set(b => b.UpdatedDate, now));
                    }
                }
            }
            if (policy.AccrualPeriod == EnumsHelper.LeaveAccrualPeriod.Yearly && now.Month == 1)
            {
                foreach (EmployeeLeaveBalance empLeave in employeeLeaveBalances)
                {
                    if (!empLeave.IsManualAllocation && (!empLeave.LastAccrual.HasValue || empLeave.LastAccrual.Value < yearStart))
                    {
                        var carryOverLimit = policy.CarryOverLimit ?? 0m;
                        var carryForward = policy.CarryOverAllowed ? Math.Min(empLeave.Remaining, carryOverLimit) : 0m;
                        var newRemaining = carryForward + policy.AccrualAmount;
                        newRemaining = policy.MaxBalance.HasValue ? Math.Min(newRemaining, policy.MaxBalance.Value) : newRemaining;
                        Expression<Func<EmployeeLeaveBalance, bool>> expression = elb => elb.CompanyId == policy.CompanyId && elb.Id == empLeave.Id && elb.Version == empLeave.Version && !elb.IsManualAllocation && (!elb.LastAccrual.HasValue || elb.LastAccrual.Value < yearStart);
                        await _employeeLeaveBalanceRepo.UpdateMany(expression, Builders<EmployeeLeaveBalance>.Update
                        .Set(b => b.TotalAllocated, newRemaining)
                        .Set(b => b.Taken, 0)
                        .Set(b => b.Remaining, newRemaining)
                        .Inc(b => b.Version, 1)
                        .Set(b => b.LastAccrual, now)
                        .Set(b => b.UpdatedDate, now));
                    }
                }
            }
        }
    }
}

