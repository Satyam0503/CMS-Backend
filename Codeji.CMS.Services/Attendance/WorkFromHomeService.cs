using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.middlewares;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.Company;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.RegularExpressions;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq.Expressions;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.LeaveManagement;
using Codeji.CMS.DTO.Leave.LeaveRequest;

public interface IWorkFromHomeService
{
    Task<Result<WorkFromHomePolicyDto>> GetPolicy();
    Task<Result<EmployeeWfhContextDto>> GetEmployeeContext();
    Task<Result> SavePolicy(WorkFromHomePolicyDto dto);
    Task<Result<IEnumerable<WorkFromHomeEmployeeAllocationDto>>> GetEmployeeAllocations();
    Task<Result> SaveEmployeeAllocation(WorkFromHomeEmployeeAllocationDto dto);
    Task<Result<WorkFromHomeResponseDto>> Create(WorkFromHomeRequestDto dto, string idempotencyKey);
    Task<Result<WorkFromHomeResponseDto>> GetMy(WorkFromHomeRequestFilterDto? filter = null);
    Task<Result<WorkFromHomeResponseDto>> GetTeam(WorkFromHomeRequestFilterDto filter);
    Task<Result<WorkFromHomeResponseDto>> GetAll(WorkFromHomeRequestFilterDto filter);
    Task<Result> Decide(string requestId, string status, WorkFromHomeDecisionDto dto);
    Task<Result> Cancel(string requestId, WorkFromHomeCancelDto dto);
    Task<Result> CheckIn(string requestId);
    Task<Result> CheckOut(string requestId, WorkFromHomeCheckOutDto? dto = null);
    Task<Result<WorkFromHomeTimingDto>> GetTiming(string requestId);
    Task<Result> RegularizeAttendance(string requestId, WorkFromHomeRegularizeAttendanceDto dto);
    Task<Result> ConvertApprovedWfhToOffice(string requestId, WorkFromHomeOfficeConversionDto dto);
    Task<Result> AdminGrantAsync(WorkFromHomeAdminGrantDto dto);
    Task<Result> CorrectInsufficientHours(string exceptionId, WorkFromHomeHoursCorrectionDto dto);
    Task<Result> ResolveAttendanceConflict(string exceptionId, WorkFromHomeConflictResolutionDto dto);
    Task<Result<WorkFromHomeDeadLetterDto>> GetDeadLetterOutbox(int pageNo, int pageSize);
    Task<Result> ReplayDeadLetterOutbox(string eventId, WorkFromHomeDeadLetterReplayDto dto);
    Task<Result<IEnumerable<AttendanceRemarkOptionDto>>> GetRemarkOptions();
    Task<Result> SaveRemarkOption(AttendanceRemarkOptionDto dto);
}

/// <summary>
/// Owns the WFH request state machine. Attendance rows are only written for an approved
/// request and are always source-linked, so normal attendance editing cannot claim them.
/// </summary>
public sealed class WorkFromHomeService(
    IHttpContextAccessor context,
    IMongoDbRepository<WorkFromHomePolicy> policies,
    IMongoDbRepository<WorkFromHomeRequest> requests,
    IMongoDbRepository<WorkFromHomeOutboxEvent> outbox,
    IMongoDbRepository<LeaveRequest> leaveRequests,
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<Department> departments,
    IMongoDbRepository<JobTitles> jobTitles,
    IMongoDbRepository<AttendanceStatusSetting> statuses,
    IMongoDbRepository<AttendanceModel> attendance,
    IMongoDbRepository<AttendanceDaySegment> attendanceSegments,
    IMongoDbRepository<AttendancePayrollException> exceptions,
    IMongoDbRepository<MonthlyAttendanceSummary> summaries,
    IMongoDbRepository<WeeklyOffSetting> weeklyOffs,
    IMongoDbRepository<CalendarEntity> calendar,
    IMongoDbRepository<AttendanceRemarkOption> remarkOptions,
    IAttendanceEditGuard attendanceEditGuard,
    IEffectiveOfficeScheduleService effectiveSchedules,
    IAttendanceInitializationService attendanceInitialization,
    IRoleService roles,
    IMongoDbRepository<Roles> companyRoles,
    IMongoClient mongoClient,
    ILeaveManagementService leaveManagement,
    ILeaveAttendanceReconciliationService leaveAttendanceReconciliation,
    IAttendanceStatusCalculationService statusCalculation) : IWorkFromHomeService
{
    internal const int MaximumEmployeeFilterMatches = 5000;
    internal const int MaximumEmployeeSearchLength = 100;
    internal const int MaximumDepartmentFilterLength = 100;
    private static readonly TimeSpan EmployeeFilterMaxExecutionTime = TimeSpan.FromSeconds(2);

    private string CompanyId => CurrentContext.CompanyId(context);
    private string UserId => CurrentContext.UserId(context);

    private async Task<bool> IsWfhAccessibleAsync() =>
        await roles.IsModuleAccessibleForCompany(AppModule.WorkFromHome, CompanyId);

    public async Task<Result<WorkFromHomePolicyDto>> GetPolicy()
    {
        if (!await IsWfhAccessibleAsync())
            return Ok(new WorkFromHomePolicyDto { IsEnabled = false, MaxDaysPerWeek = 0 });

        var policy = await policies.FirstOrDefault(x => x.CompanyId == CompanyId);
        var maxWorkingDays = await GetMaxWorkingDaysPerWeekAsync(policy);
        var policyDto = policy is null
            ? new WorkFromHomePolicyDto { IsEnabled = true, MaxDaysPerWeek = 1, MinimumAdvanceNoticeHours = 0 }
            : Map(policy);
        policyDto.MaxDaysPerWeek = Math.Min(policyDto.MaxDaysPerWeek, maxWorkingDays);
        return Ok(policyDto);
    }

    public async Task<Result<EmployeeWfhContextDto>> GetEmployeeContext()
    {
        if (!await IsWfhAccessibleAsync())
            return Ok(new EmployeeWfhContextDto { DisabledReasonCode = "WFH_MODULE_DISABLED", IsEligible = false });

        var employee = await ActiveEmployee(UserId);
        var policy = await policies.FirstOrDefault(x => x.CompanyId == CompanyId);
        if (employee is null || policy is null || !policy.IsEnabled)
            return Ok(new EmployeeWfhContextDto { DisabledReasonCode = employee is null ? "WFH_EMPLOYEE_NOT_ELIGIBLE" : "WFH_POLICY_DISABLED" });

        var schedule = await effectiveSchedules.ResolveAsync(CompanyId, employee, DateTime.UtcNow);
        var companyNow = CompanyNow(schedule?.TimeZoneId ?? policy.TimeZoneId);
        var today = companyNow.Date;
        var officeStart = schedule?.StartTime ?? policy.OfficeStartTime;
        var week = StartOfWeek(today);
        var eligible = IsEligible(policy, employee) && IsEffectiveFor(policy, today);
        var weeklyRequests = await requests.GetAll(x => x.CompanyId == CompanyId && x.UserId == UserId &&
            (x.Status == "Pending" || x.Status == "Approved" || x.Status == "Returned") &&
            x.FromDate < week.AddDays(7) && x.ToDate >= week);
        var used = WeeklyUsage(weeklyRequests, week);
        var todayApprovedRequests = weeklyRequests
            .Where(request => request.Status == "Approved" && request.FromDate.Date <= today && request.ToDate.Date >= today)
            .OrderByDescending(request => request.CreatedDate)
            .ToList();
        var maxWorkingDays = await GetMaxWorkingDaysPerWeekAsync(policy);
        var weeklyLimit = WeeklyLimitFor(policy, employee, maxWorkingDays);
        var remaining = Math.Max(0m, weeklyLimit - used);
        var reason = !eligible || weeklyLimit == 0 ? "WFH_POLICY_NOT_ASSIGNED" : remaining == 0 ? "WFH_WEEKLY_LIMIT_USED" : null;
        var officeEnd = schedule?.EndTime ?? policy.OfficeEndTime;
        var effectiveShiftStart = officeStart ?? new TimeSpan(9, 30, 0);
        var effectiveCutoff = ResolveEffectiveCutoffTime(effectiveShiftStart, policy.SameDayCutoffTime, policy.SameDayGraceMinutes);
        var secondHalfCutoff = ResolveSecondHalfCutoffTime(officeStart, schedule?.RequiredWorkingMinutes, policy.FullDayMinimumHours, policy.SameDayGraceMinutes);
        var effectiveShiftEnd = officeEnd ?? new TimeSpan(18, 30, 0);

        var isShiftStarted = companyNow.TimeOfDay >= effectiveShiftStart;
        var isFirstHalfPassed = effectiveCutoff.HasValue && companyNow.TimeOfDay >= effectiveCutoff.Value;
        var isSecondHalfPassed = companyNow.TimeOfDay >= secondHalfCutoff;
        var isShiftEnded = companyNow.TimeOfDay >= effectiveShiftEnd;

        var allowedTodayDurations = new List<string>();
        if (!isShiftEnded && !isSecondHalfPassed)
        {
            if (!isFirstHalfPassed)
            {
                allowedTodayDurations.Add("FullDay");
                if (policy.AllowHalfDay)
                {
                    allowedTodayDurations.Add("FirstHalf");
                    allowedTodayDurations.Add("SecondHalf");
                }
            }
            else if (policy.AllowHalfDay)
            {
                allowedTodayDurations.Add("SecondHalf");
            }
        }

        return Ok(new EmployeeWfhContextDto
        {
            IsFeatureEnabled = true, IsEligible = eligible && weeklyLimit > 0, WeeklyLimit = weeklyLimit, UsedThisWeek = used,
            RemainingThisWeek = remaining, CanSchedule = reason is null, DisabledReasonCode = reason,
            NextAvailableDate = remaining == 0 ? week.AddDays(7) : null, ManagerApprovalRequired = policy.ManagerApprovalRequired,
            MinimumAdvanceNoticeHours = policy.MinimumAdvanceNoticeHours, BusinessDate = today,
            TimeZoneId = schedule?.TimeZoneId ?? policy.TimeZoneId,
            TodayApprovedRequest = todayApprovedRequests.Count == 1 ? Map(todayApprovedRequests[0]) : null,
            TodayRequestIssueCode = todayApprovedRequests.Count > 1 ? "WFH_MULTIPLE_APPROVED_REQUESTS_FOR_TODAY" : null,
            IsSameDayRequestCutoffPassed = isFirstHalfPassed,
            IsShiftStartedToday = isShiftStarted,
            IsFirstHalfPassedToday = isFirstHalfPassed,
            IsSecondHalfPassedToday = isSecondHalfPassed,
            AllowedTodayDurations = allowedTodayDurations,
            EffectiveOfficeStartTime = officeStart,
            EffectiveCutoffTime = effectiveCutoff,
            SecondHalfCutoffTime = secondHalfCutoff,
            SameDayCutoffTime = policy.SameDayCutoffTime,
            SameDayGraceMinutes = policy.SameDayGraceMinutes,
            AllowLateApplicationWithApproval = policy.AllowLateApplicationWithApproval,
            AllowedDaysOfWeek = policy.AllowedDaysOfWeek ?? [],
            BlockedDaysOfWeek = policy.BlockedDaysOfWeek ?? [],
            OfficeStartTime = policy.OfficeStartTime,
            OfficeEndTime = policy.OfficeEndTime, CheckInAvailableFrom = policy.AllowedCheckInFrom,
            CheckInAvailableUntil = policy.AllowedCheckInUntil, CheckOutAvailableFrom = policy.AllowedCheckOutFrom,
            CheckOutAvailableUntil = policy.AllowedCheckOutUntil
        });
    }

    public async Task<Result> SavePolicy(WorkFromHomePolicyDto dto)
    {
        if (!await IsWfhAccessibleAsync())
            return Fail("WFH_MODULE_DISABLED", "Work from home module is not enabled for this company.");

        if (dto is null) return Fail("WFH_INVALID_POLICY", "A WFH policy payload is required.");
        if (dto.ApplicableDepartments is null || dto.ApplicableEmployeeIds is null ||
            dto.ExcludedEmployeeIds is null || dto.ApplicableEmploymentTypes is null)
            return Fail("WFH_INVALID_POLICY", "Employee and department assignment lists are required; use empty lists when none apply.");
        var timeZoneId = string.IsNullOrWhiteSpace(dto.TimeZoneId) ? "UTC" : dto.TimeZoneId.Trim();
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { return Fail("WFH_INVALID_TIME_ZONE", "Select a recognized company time zone."); }
        catch (InvalidTimeZoneException) { return Fail("WFH_INVALID_TIME_ZONE", "The selected company time zone is invalid."); }
        if (dto.AllowManagerSelfApproval) return Fail("WFH_SELF_APPROVAL_UNSUPPORTED", "WFH self-approval is disabled. Configure an independent manager or HR approver.");
        if (dto.RequireAttachment) return Fail("WFH_ATTACHMENT_UNSUPPORTED", "WFH attachments are not supported by this request flow. Disable the attachment requirement before saving.");
        if (dto.AllowMixedDay) return Fail("WFH_MIXED_DAY_UNSUPPORTED", "Mixed WFH/WFO is not available until attendance is represented as separate half-day segments.");
        if (dto.RequireReason && dto.RequireAttachment) return Fail("WFH_INVALID_POLICY", "Attachment-only reasons are not supported.");
        var maxWorkingDays = await GetMaxWorkingDaysPerWeekAsync();
        if (dto.MaxDaysPerWeek < 1 || dto.MaxDaysPerWeek > maxWorkingDays)
            return Fail("WFH_INVALID_POLICY", $"Maximum WFH days per week must be between 1 and {maxWorkingDays} working day(s) per week based on company working schedule.");
        if (dto.SameDayGraceMinutes is < 0 or > 1440 || dto.MinimumAdvanceNoticeHours is < 0 or > 8760 ||
            dto.MaximumBackdatedDays is < 0 or > 3650 ||
            dto.FullDayMinimumHours is <= 0 or > 24 || dto.HalfDayMinimumHours is <= 0 or > 12 || dto.HalfDayMinimumHours > dto.FullDayMinimumHours ||
            dto.MaxDaysPerMonth is < 1 or > 31 || dto.MaxConsecutiveDays is < 1 or > 31 ||
            (dto.EffectiveFrom.HasValue && dto.EffectiveTo.HasValue && dto.EffectiveFrom.Value.Date > dto.EffectiveTo.Value.Date) ||
            !IsValidBusinessTime(dto.SameDayCutoffTime) || !IsValidBusinessTime(dto.OfficeStartTime) || !IsValidBusinessTime(dto.OfficeEndTime) ||
            !IsValidBusinessTime(dto.AllowedCheckInFrom) || !IsValidBusinessTime(dto.AllowedCheckInUntil) ||
            !IsValidBusinessTime(dto.AllowedCheckOutFrom) || !IsValidBusinessTime(dto.AllowedCheckOutUntil) ||
            (dto.OfficeStartTime.HasValue && dto.OfficeEndTime.HasValue && dto.OfficeStartTime >= dto.OfficeEndTime) ||
            (dto.AllowedCheckInFrom.HasValue && dto.AllowedCheckInUntil.HasValue && dto.AllowedCheckInFrom > dto.AllowedCheckInUntil) ||
            (dto.AllowedCheckOutFrom.HasValue && dto.AllowedCheckOutUntil.HasValue && dto.AllowedCheckOutFrom > dto.AllowedCheckOutUntil))
            return Fail("WFH_INVALID_POLICY", "WFH hours, quotas, date range, grace, and time windows must be valid and ordered.");

        var configuredCodes = new[] { dto.FullDayAttendanceStatusCode, dto.HalfDayAttendanceStatusCode, dto.MixedAttendanceStatusCode, dto.WfhWfoAttendanceStatusCode, dto.WfhHdAttendanceStatusCode };
        var configuredStatuses = await Task.WhenAll(configuredCodes.Select(async code => string.IsNullOrWhiteSpace(code)
            ? null
            : (await statuses.GetAll(x => x.CompanyId == CompanyId && x.Code == code && x.IsActive)).FirstOrDefault()));
        if (configuredCodes.Any(string.IsNullOrWhiteSpace) || configuredStatuses.Any(status => status is null || !status.RequiresTime))
            return Fail("WFH_INVALID_ATTENDANCE_STATUS", "WFH status codes must be active company statuses that require check-in and check-out.");
        var policy = await policies.FirstOrDefault(x => x.CompanyId == CompanyId) ?? new WorkFromHomePolicy { CompanyId = CompanyId };
        var previousVersion = policy.Version;
        Apply(policy, dto);
        policy.Version = previousVersion + 1;
        return string.IsNullOrEmpty(policy.PolicyId)
            ? await policies.AddOne(policy)
            : await policies.Update(PolicyVersionFilter(CompanyId, policy.PolicyId, previousVersion), policy);
    }

    private static FilterDefinition<WorkFromHomePolicy> PolicyVersionFilter(string companyId, string policyId, int version)
    {
        var scope = Builders<WorkFromHomePolicy>.Filter.Where(x => x.CompanyId == companyId && x.PolicyId == policyId);
        if (version != 0) return scope & Builders<WorkFromHomePolicy>.Filter.Eq(x => x.Version, version);
        // Existing tenant policies predate the additive Version field. Treat
        // missing and explicit zero as the same initial version; after this
        // compare-and-set they are stored with Version=1.
        return scope & Builders<WorkFromHomePolicy>.Filter.Or(
            Builders<WorkFromHomePolicy>.Filter.Eq(x => x.Version, 0),
            Builders<WorkFromHomePolicy>.Filter.Exists(x => x.Version, false));
    }

    private async Task AcquireWfhQuotaLock(IClientSessionHandle? session, string userId, CancellationToken cancellationToken)
    {
        // A deterministic per-tenant/user document serializes quota claims. It is
        // intentionally written in the same transaction as the request mutation;
        // competing transactions then conflict and Mongo's transaction retry runs
        // quota validation against the newly committed request set.
        var lockId = BuildQuotaLockId(CompanyId, userId);
        var db = requests.GetCollection()?.Database;
        if (db is null) return;
        var collection = db.GetCollection<BsonDocument>("WorkFromHomeQuotaLock");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", lockId);
        var update = Builders<BsonDocument>.Update
            .SetOnInsert("CompanyId", CompanyId)
            .SetOnInsert("UserId", userId)
            .Set("UpdatedAtUtc", DateTime.UtcNow)
            .Inc("LockVersion", 1L);
        if (session is not null)
            await collection.UpdateOneAsync(session, filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
        else
            await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);

        var lockDocument = session is not null
            ? await collection.Find(session, filter).FirstOrDefaultAsync(cancellationToken)
            : await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (lockDocument is null || lockDocument.GetValue("CompanyId", BsonNull.Value).AsString != CompanyId ||
            lockDocument.GetValue("UserId", BsonNull.Value).AsString != userId)
            throw new InvalidOperationException("WFH_QUOTA_LOCK_TENANT_MISMATCH");
    }

    internal static string BuildQuotaLockId(string companyId, string userId) => $"wfh:{companyId.Length}:{companyId}:{userId}";

    internal static FilterDefinition<EmpUser> BuildApprovalEmployeeFilter(string companyId, string userId) =>
        Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == userId && x.Status && !x.IsDeleted &&
            (x.Lifecycle == null || x.Lifecycle.EmploymentStatus == null || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended) &&
            (x.EmploymentStatus == null || x.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.EmploymentStatus == EmployeeLifecycleStatuses.Suspended));

    private async Task AcquireWfhPolicyQuotaLock(IClientSessionHandle? session, WorkFromHomePolicy policy, CancellationToken cancellationToken)
    {
        var filter = PolicyVersionFilter(CompanyId, policy.PolicyId, policy.Version);
        var update = Builders<WorkFromHomePolicy>.Update.Inc(x => x.QuotaLockVersion, 1L);
        var result = session is not null
            ? await policies.GetCollection().UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken)
            : await policies.GetCollection().UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.MatchedCount != 1)
            throw new InvalidOperationException("WFH_POLICY_CHANGED");
    }

    private static bool IsValidBusinessTime(TimeSpan? value) =>
        !value.HasValue || (value.Value >= TimeSpan.Zero && value.Value < TimeSpan.FromDays(1));

    public async Task<Result<IEnumerable<WorkFromHomeEmployeeAllocationDto>>> GetEmployeeAllocations()
    {
        if (!await IsWfhAccessibleAsync())
            return Ok<IEnumerable<WorkFromHomeEmployeeAllocationDto>>([]);

        var policy = await policies.FirstOrDefault(x => x.CompanyId == CompanyId);
        if (policy is null)
            return Ok<IEnumerable<WorkFromHomeEmployeeAllocationDto>>([]);

        var maxWorkingDays = await GetMaxWorkingDaysPerWeekAsync(policy);
        var today = CompanyNow(policy).Date;
        var activeEmployees = await employees.GetAll(x => x.CompanyId == CompanyId && x.Status && !x.IsDeleted && (x.Lifecycle == null || x.Lifecycle.EmploymentStatus == null || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended));
        return Ok<IEnumerable<WorkFromHomeEmployeeAllocationDto>>(activeEmployees
            .Select(employee =>
            {
                var weeklyLimit = WeeklyLimitFor(policy, employee, maxWorkingDays);
                return new WorkFromHomeEmployeeAllocationDto
                {
                    EmployeeId = employee.EmployeeId,
                    WeeklyLimit = weeklyLimit,
                    IsEligible = policy.IsEnabled && IsEffectiveFor(policy, today) && IsEligible(policy, employee) && weeklyLimit > 0
                };
            })
            .OrderBy(x => x.EmployeeId));
    }

    public async Task<Result> SaveEmployeeAllocation(WorkFromHomeEmployeeAllocationDto dto)
    {
        if (dto is null) return Fail("WFH_INVALID_EMPLOYEE_ALLOCATION", "An employee allocation payload is required.", StatusCodes.Status400BadRequest);
        if (!await IsWfhAccessibleAsync())
            return Fail("WFH_MODULE_DISABLED", "Work from home module is not enabled for this company.");

        var employeeId = dto.EmployeeId?.Trim();
        var maxWorkingDays = await GetMaxWorkingDaysPerWeekAsync();
        if (string.IsNullOrWhiteSpace(employeeId) || dto.WeeklyLimit < 0 || dto.WeeklyLimit > maxWorkingDays)
            return Fail("WFH_INVALID_EMPLOYEE_ALLOCATION", $"Provide an active employee ID and a weekly WFH limit from 0 to {maxWorkingDays} working day(s) per week.");
        if (!await employees.Exist(x => x.CompanyId == CompanyId && x.EmployeeId == employeeId && x.Status && !x.IsDeleted && (x.Lifecycle == null || x.Lifecycle.EmploymentStatus == null || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended))) return Fail("WFH_EMPLOYEE_NOT_FOUND", "The employee is not active in this company.");
        var policy = await policies.FirstOrDefault(x => x.CompanyId == CompanyId);
        if (policy is null) return Fail("WFH_POLICY_DISABLED", "Configure the company WFH policy before allocating WFH.");
        policy.EmployeeAllocations ??= [];
        policy.ExcludedEmployeeIds ??= [];
        policy.ApplicableEmployeeIds ??= [];

        // EmployeeAllocations are not only quota overrides: they are the explicit
        // per-employee allocation record. Keep a zero allocation so the UI can
        // distinguish an HR deallocation from an employee inheriting the company
        // default, and mirror it in the exclusion list used by every request path.
        policy.EmployeeAllocations.RemoveAll(x => string.Equals(x.EmployeeId, employeeId, StringComparison.Ordinal));
        policy.EmployeeAllocations.Add(new WorkFromHomeEmployeeAllocation { EmployeeId = employeeId, WeeklyLimit = dto.WeeklyLimit });
        policy.ExcludedEmployeeIds ??= [];
        policy.ApplicableEmployeeIds ??= [];
        policy.ExcludedEmployeeIds.RemoveAll(x => string.Equals(x, employeeId, StringComparison.Ordinal));
        policy.ApplicableEmployeeIds.RemoveAll(x => string.Equals(x, employeeId, StringComparison.Ordinal));
        if (dto.WeeklyLimit == 0)
            policy.ExcludedEmployeeIds.Add(employeeId);
        else
            policy.ApplicableEmployeeIds.Add(employeeId);
        var previousVersion = policy.Version;
        policy.Version++;
        policy.UpdatedBy = UserId;
        policy.UpdatedDate = DateTime.UtcNow;
        var updated = await policies.Update(PolicyVersionFilter(CompanyId, policy.PolicyId, previousVersion), policy);
        return updated.Success ? updated : Fail("WFH_POLICY_CHANGED", "The WFH policy changed while saving the employee allocation. Refresh and try again.", StatusCodes.Status409Conflict);
    }

    public async Task<Result<WorkFromHomeResponseDto>> Create(WorkFromHomeRequestDto dto, string idempotencyKey)
    {
        if (dto is null) return Fail<WorkFromHomeResponseDto>("WFH_INVALID_REQUEST", "A WFH request payload is required.");
        if (!await IsWfhAccessibleAsync()) return Fail<WorkFromHomeResponseDto>("WFH_MODULE_DISABLED", "Work from home is not enabled for this company.");
        var suppliedIdempotencyKey = idempotencyKey;
        var normalizedIdempotencyKey = NormalizeIdempotencyKey(idempotencyKey);
        if (normalizedIdempotencyKey is null)
            return Fail<WorkFromHomeResponseDto>(string.IsNullOrWhiteSpace(suppliedIdempotencyKey) ? "WFH_IDEMPOTENCY_KEY_REQUIRED" : "WFH_INVALID_IDEMPOTENCY_KEY",
                "A valid request retry key is required. Refresh the form and submit again.", StatusCodes.Status400BadRequest);
        idempotencyKey = normalizedIdempotencyKey;
        var reasonCode = NormalizeReasonCode(dto.ReasonCode);
        if (reasonCode is null) return Fail<WorkFromHomeResponseDto>(
            string.IsNullOrWhiteSpace(dto.ReasonCode) ? "WFH_REASON_CODE_REQUIRED" : "WFH_INVALID_REASON_CODE",
            "Select a supported WFH reason category.", StatusCodes.Status400BadRequest);
        var idempotencyPayloadHash = ComputeRequestPayloadHash(dto);
        var replay = await FindIdempotentRequest(idempotencyKey);
        if (replay is not null)
            return string.Equals(replay.IdempotencyPayloadHash, idempotencyPayloadHash, StringComparison.Ordinal)
                ? Ok(Map(replay))
                : Fail<WorkFromHomeResponseDto>("WFH_IDEMPOTENCY_KEY_REUSED", "This retry key was already used for a different request. Start a new submission.", StatusCodes.Status409Conflict);
        var employee = await ActiveEmployee(UserId);
        var policy = await ActivePolicy();
        if (employee is null) return Fail<WorkFromHomeResponseDto>("WFH_EMPLOYEE_NOT_ELIGIBLE", "Employee is not active in this company.");
        if (policy is null) return Fail<WorkFromHomeResponseDto>("WFH_POLICY_DISABLED", "Work from home is not enabled for this company.");
        if (!IsEligible(policy, employee)) return Fail<WorkFromHomeResponseDto>("WFH_POLICY_NOT_ASSIGNED", "This WFH policy is not assigned to your current employee profile.");
        var validation = await ValidateRequest(employee, policy, dto);
        if (validation is not null) return Fail<WorkFromHomeResponseDto>(validation.Value.Code, validation.Value.Message);

        var requestFromDate = BusinessDateUtc(dto.FromDate.ToDateTime(TimeOnly.MinValue));
        var requestToDate = BusinessDateUtc(dto.ToDate.ToDateTime(TimeOnly.MinValue));
        var schedule = await effectiveSchedules.ResolveAsync(CompanyId, employee, requestFromDate);
        var companyNow = CompanyNow(schedule?.TimeZoneId ?? policy.TimeZoneId);
        var officeStart = schedule?.StartTime ?? policy.OfficeStartTime;
        var isLateSubmission = Days(requestFromDate, requestToDate).Any(day =>
            IsSameDayWfhStartCutoffReached(day, companyNow, dto.DurationType, officeStart, policy.SameDayCutoffTime, policy.SameDayGraceMinutes, schedule?.RequiredWorkingMinutes, policy.FullDayMinimumHours));
        var approver = await ResolveApprover(employee, policy);
        var requiresApproval = policy.ManagerApprovalRequired || isLateSubmission;
        if (requiresApproval && string.IsNullOrWhiteSpace(approver)) return Fail<WorkFromHomeResponseDto>("WFH_APPROVER_NOT_CONFIGURED", "This request requires approval, but no eligible manager or HR approver is configured.");
        var request = new WorkFromHomeRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            CompanyId = CompanyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, FromDate = requestFromDate, ToDate = requestToDate,
            DurationType = dto.DurationType, ReasonCode = reasonCode, ReasonText = Clean(dto.ReasonText),
            IdempotencyKey = idempotencyKey, IdempotencyPayloadHash = idempotencyPayloadHash,
            Status = requiresApproval ? "Pending" : "Approved", ApproverUserId = approver, CreatedBy = UserId, CreatedDate = DateTime.UtcNow,
            IsLateSubmission = isLateSubmission
        };
        var initialAction = isLateSubmission ? "LateSubmissionCreated" : (requiresApproval ? "Created" : "AutoApproved");
        var auditEvent = BuildAuditOutboxEvent(request, null, request.Status, initialAction, null,
            isLateSubmission ? "Submitted after cutoff time" : null, UserId);
        var requestSubject = isLateSubmission ? "Late WFH request submitted" : (requiresApproval ? "WFH request submitted" : "Employee working from home");
        var transitionEvents = new List<WorkFromHomeOutboxEvent> { auditEvent };
        transitionEvents.AddRange(await BuildHrNotificationOutboxEventsAsync(request, employee, requestSubject, initialAction, UserId));
        if (!requiresApproval)
            transitionEvents.Add(BuildEmployeeNotificationOutboxEvent(request, employee,
                "Your Work From Home request has been auto-approved.", EnumsHelper.NotificationTypes.WorkFromHomeApproved, "AutoApproved", UserId));
        var createResult = requiresApproval
            ? await CreatePendingRequestTransaction(request, employee, policy, dto, transitionEvents)
            : await CreateAutoApprovedTransaction(request, employee, policy, dto, transitionEvents);
        if (!createResult.Success)
        {
            var committed = await FindIdempotentRequest(idempotencyKey);
            if (committed is not null)
                return string.Equals(committed.IdempotencyPayloadHash, idempotencyPayloadHash, StringComparison.Ordinal)
                    ? Ok(Map(committed))
                    : Fail<WorkFromHomeResponseDto>("WFH_IDEMPOTENCY_KEY_REUSED", "This retry key was already used for a different request. Start a new submission.", StatusCodes.Status409Conflict);
            return new Result<WorkFromHomeResponseDto> { Success = false, StatusCode = createResult.StatusCode, Message = createResult.Message };
        }
        return Ok(Map(request));
    }

    internal static string? NormalizeIdempotencyKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Any(char.IsControl)) return null;
        var key = value.Trim();
        return key.Length is >= 16 and <= 128 ? key : null;
    }

    internal static string? NormalizeReasonCode(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "PERSONAL" => "PERSONAL",
        "MEDICAL" => "MEDICAL",
        "FAMILY" => "FAMILY",
        "HOME_MAINTENANCE" => "HOME_MAINTENANCE",
        "TRAVEL_DISRUPTION" => "TRAVEL_DISRUPTION",
        "OPERATIONAL" => "OPERATIONAL",
        "MANAGER_INSTRUCTION" => "MANAGER_INSTRUCTION",
        "OTHER" => "OTHER",
        _ => null
    };

    internal static string ComputeRequestPayloadHash(WorkFromHomeRequestDto dto)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            FromDate = dto.FromDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            ToDate = dto.ToDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            DurationType = dto.DurationType?.Trim(),
            ReasonCode = dto.ReasonCode?.Trim().ToUpperInvariant(),
            ReasonText = Clean(dto.ReasonText)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    internal static Expression<Func<WorkFromHomeRequest, bool>> BuildIdempotentRequestPredicate(string companyId, string userId, string idempotencyKey) =>
        x => x.CompanyId == companyId && x.UserId == userId && x.IdempotencyKey == idempotencyKey;

    private Task<WorkFromHomeRequest?> FindIdempotentRequest(string idempotencyKey) =>
        requests.FirstOrDefault(BuildIdempotentRequestPredicate(CompanyId, UserId, idempotencyKey));

    internal static bool IsTransactionUnsupported(Exception ex) =>
        ex is NotSupportedException ||
        (ex is MongoCommandException mongoException && (mongoException.Code == 20 || mongoException.Code == 263)) ||
        ex.Message.Contains("Transaction numbers are only allowed", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("replica set", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("standalone servers do not support transactions", StringComparison.OrdinalIgnoreCase);

    private async Task<Result> CreatePendingRequestTransaction(WorkFromHomeRequest request, EmpUser employee, WorkFromHomePolicy policy, WorkFromHomeRequestDto dto, IReadOnlyCollection<WorkFromHomeOutboxEvent> transitionEvents)
    {
        try
        {
            using var session = await mongoClient.StartSessionAsync();
            await session.WithTransactionAsync(async (transaction, cancellationToken) =>
            {
                await AcquireWfhQuotaLock(transaction, request.UserId, cancellationToken);
                await AcquireWfhPolicyQuotaLock(transaction, policy, cancellationToken);
                var validation = await ValidateRequest(employee, policy, dto);
                if (validation is not null) throw new InvalidOperationException($"WFH_VALIDATION:{validation.Value.Code}:{validation.Value.Message}");
                await requests.GetCollection().InsertOneAsync(transaction, request, cancellationToken: cancellationToken);
                await InsertOutboxEventsAsync(transaction, transitionEvents, cancellationToken);
                return true;
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("WFH_VALIDATION:", StringComparison.Ordinal))
        {
            var parts = ex.Message.Split(':', 3);
            return Fail(parts.Length > 1 ? parts[1] : "WFH_VALIDATION_FAILED", parts.Length > 2 ? parts[2] : "The WFH request no longer meets current policy rules.");
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_POLICY_CHANGED")
        {
            return Fail("WFH_POLICY_CHANGED", "The WFH policy changed while this request was being submitted. Refresh and try again.", StatusCodes.Status409Conflict);
        }
        catch (Exception ex) when (IsTransactionUnsupported(ex))
        {
            return await CreatePendingRequestStandaloneAsync(request, employee, policy, dto, transitionEvents);
        }
        catch
        {
            return Fail("WFH_REQUEST_CREATE_FAILED", "The request could not be created. No quota was consumed.", StatusCodes.Status503ServiceUnavailable);
        }
        return new Result { Success = true, Message = "OK" };
    }

    private async Task<Result> CreatePendingRequestStandaloneAsync(
        WorkFromHomeRequest request, EmpUser employee, WorkFromHomePolicy policy, WorkFromHomeRequestDto dto, IReadOnlyCollection<WorkFromHomeOutboxEvent> transitionEvents)
    {
        try
        {
            await AcquireWfhQuotaLock(null, request.UserId, CancellationToken.None);
            await AcquireWfhPolicyQuotaLock(null, policy, CancellationToken.None);
            var validation = await ValidateRequest(employee, policy, dto);
            if (validation is not null) return Fail(validation.Value.Code, validation.Value.Message);
            await requests.GetCollection().InsertOneAsync(request);
            await InsertOutboxEventsAsync(null, transitionEvents, CancellationToken.None);
            return new Result { Success = true, Message = "OK" };
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_POLICY_CHANGED")
        {
            return Fail("WFH_POLICY_CHANGED", "The WFH policy changed while this request was being submitted. Refresh and try again.", StatusCodes.Status409Conflict);
        }
        catch (Exception ex)
        {
            return Fail("WFH_REQUEST_CREATE_FAILED", "The request could not be created: " + ex.Message, StatusCodes.Status503ServiceUnavailable);
        }
    }

    private async Task<Result> CreateAutoApprovedTransaction(WorkFromHomeRequest request, EmpUser employee, WorkFromHomePolicy policy, WorkFromHomeRequestDto dto, IReadOnlyCollection<WorkFromHomeOutboxEvent> transitionEvents)
    {
        var changedDates = new List<DateTime>();
        try
        {
            using var session = await mongoClient.StartSessionAsync();
            await session.WithTransactionAsync(async (transaction, cancellationToken) =>
            {
                await AcquireWfhQuotaLock(transaction, request.UserId, cancellationToken);
                await AcquireWfhPolicyQuotaLock(transaction, policy, cancellationToken);
                var validation = await ValidateRequest(employee, policy, dto);
                if (validation is not null) throw new InvalidOperationException($"WFH_VALIDATION:{validation.Value.Code}:{validation.Value.Message}");
                if (await HasActiveLeaveOverlap(request.UserId, request.FromDate, request.ToDate, request.DurationType))
                    throw new InvalidOperationException("WFH_LEAVE_OVERLAP");
                var reconciliation = await ReconcileApproved(request, employee, policy, request.Version, transaction, changedDates);
                if (!reconciliation.Success) throw new InvalidOperationException(reconciliation.Message ?? "WFH_RECONCILIATION_FAILED");
                await requests.GetCollection().InsertOneAsync(transaction, request, cancellationToken: cancellationToken);
                await InsertOutboxEventsAsync(transaction, transitionEvents, cancellationToken);
                return true;
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_LEAVE_OVERLAP")
        {
            return Fail("WFH_LEAVE_OVERLAP", "The request overlaps leave for the same half or a full day.");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("WFH_VALIDATION:", StringComparison.Ordinal))
        {
            var parts = ex.Message.Split(':', 3);
            return Fail(parts.Length > 1 ? parts[1] : "WFH_VALIDATION_FAILED", parts.Length > 2 ? parts[2] : "The WFH request no longer meets current policy rules.");
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_POLICY_CHANGED")
        {
            return Fail("WFH_POLICY_CHANGED", "The WFH policy changed while this request was being submitted. Refresh and try again.", StatusCodes.Status409Conflict);
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_ATTENDANCE_WRITE_CONFLICT")
        {
            return Fail("WFH_DATE_ALREADY_OWNED", "Attendance changed while WFH was being applied. Refresh and try again.", StatusCodes.Status409Conflict);
        }
        catch (InvalidOperationException ex)
        {
            return Fail("WFH_RECONCILIATION_FAILED", ex.Message, StatusCodes.Status409Conflict);
        }
        catch (Exception ex) when (IsTransactionUnsupported(ex))
        {
            return await CreateAutoApprovedStandaloneAsync(request, employee, policy, dto, transitionEvents, changedDates);
        }
        catch
        {
            return Fail("WFH_AUTO_APPROVAL_FAILED", "The request could not be auto-approved. No WFH attendance was applied.", StatusCodes.Status503ServiceUnavailable);
        }
        foreach (var date in changedDates.Distinct()) await InvalidateSummary(employee, date);
        return new Result { Success = true, Message = "OK" };
    }

    private async Task<Result> CreateAutoApprovedStandaloneAsync(
        WorkFromHomeRequest request, EmpUser employee, WorkFromHomePolicy policy, WorkFromHomeRequestDto dto,
        IReadOnlyCollection<WorkFromHomeOutboxEvent> transitionEvents, List<DateTime> changedDates)
    {
        try
        {
            await AcquireWfhQuotaLock(null, request.UserId, CancellationToken.None);
            await AcquireWfhPolicyQuotaLock(null, policy, CancellationToken.None);
            var validation = await ValidateRequest(employee, policy, dto);
            if (validation is not null) return Fail(validation.Value.Code, validation.Value.Message);
            if (await HasActiveLeaveOverlap(request.UserId, request.FromDate, request.ToDate, request.DurationType))
                return Fail("WFH_LEAVE_OVERLAP", "The request overlaps leave for the same half or a full day.");
            var reconciliation = await ReconcileApproved(request, employee, policy, request.Version, null, changedDates);
            if (!reconciliation.Success) return Fail(reconciliation.Message ?? "WFH_RECONCILIATION_FAILED", reconciliation.Message ?? "WFH_RECONCILIATION_FAILED", StatusCodes.Status409Conflict);
            await requests.GetCollection().InsertOneAsync(request);
            await InsertOutboxEventsAsync(null, transitionEvents, CancellationToken.None);
            foreach (var date in changedDates.Distinct()) await InvalidateSummary(employee, date);
            return new Result { Success = true, Message = "OK" };
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_POLICY_CHANGED")
        {
            return Fail("WFH_POLICY_CHANGED", "The WFH policy changed while this request was being submitted. Refresh and try again.", StatusCodes.Status409Conflict);
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_ATTENDANCE_WRITE_CONFLICT")
        {
            return Fail("WFH_DATE_ALREADY_OWNED", "Attendance changed while WFH was being applied. Refresh and try again.", StatusCodes.Status409Conflict);
        }
        catch (Exception ex)
        {
            foreach (var date in changedDates.Distinct())
            {
                await attendance.GetCollection().DeleteOneAsync(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId);
                await attendanceSegments.GetCollection().DeleteManyAsync(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId);
            }
            return Fail("WFH_AUTO_APPROVAL_FAILED", "The request could not be auto-approved: " + ex.Message, StatusCodes.Status503ServiceUnavailable);
        }
    }

    public async Task<Result<WorkFromHomeResponseDto>> GetMy(WorkFromHomeRequestFilterDto? filter = null)
    {
        if (!await IsWfhAccessibleAsync())
            return new Result<WorkFromHomeResponseDto> { Success = true, MethodResults = [], TotalRecords = 0 };
        filter ??= new WorkFromHomeRequestFilterDto();
        var pageNo = Math.Max(1, filter.PageNo);
        var pageSize = NormalizeWfhPageSize(filter.PageSize);
        var requestFilter = BuildMyRequestsFilter(CompanyId, UserId);
        var fromDate = filter.FromDate.HasValue ? BusinessDateUtc(filter.FromDate.Value.ToDateTime(TimeOnly.MinValue)) : (DateTime?)null;
        var toDate = filter.ToDate.HasValue ? BusinessDateUtc(filter.ToDate.Value.ToDateTime(TimeOnly.MinValue)) : (DateTime?)null;
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var allowedStatuses = new[] { "Pending", "Approved", "Rejected", "Returned", "Cancelled" };
            var status = filter.Status.Trim();
            if (!allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                return Fail<WorkFromHomeResponseDto>("WFH_INVALID_STATUS", "Unsupported WFH status filter.");
            requestFilter &= Builders<WorkFromHomeRequest>.Filter.Eq(request => request.Status, status);
        }
        if (fromDate.HasValue) requestFilter &= Builders<WorkFromHomeRequest>.Filter.Gte(request => request.ToDate, fromDate.Value);
        if (toDate.HasValue) requestFilter &= Builders<WorkFromHomeRequest>.Filter.Lte(request => request.FromDate, toDate.Value);
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
            return Fail<WorkFromHomeResponseDto>("WFH_INVALID_DATE_RANGE", "From date cannot be after to date.");
        var collection = requests.GetCollection();
        var total = await collection.CountDocumentsAsync(requestFilter);
        var items = await collection.Find(requestFilter).SortByDescending(request => request.CreatedDate)
            .Skip((pageNo - 1) * pageSize).Limit(pageSize).ToListAsync();
        var deptMap = await GetDepartmentNameMapAsync();
        var employee = await ActiveEmployee(UserId);
        return new Result<WorkFromHomeResponseDto>
        {
            Success = true,
            MethodResults = items.Select(request => Map(request, employee, deptMap)).ToList(),
            TotalRecords = (int)Math.Min(total, int.MaxValue)
        };
    }

    internal static int NormalizeWfhPageSize(int pageSize) => pageSize is 10 or 25 or 50 or 100 ? pageSize : 25;

    internal static FilterDefinition<WorkFromHomeRequest> BuildMyRequestsFilter(string companyId, string userId) =>
        Builders<WorkFromHomeRequest>.Filter.And(
            Builders<WorkFromHomeRequest>.Filter.Eq(request => request.CompanyId, companyId),
            Builders<WorkFromHomeRequest>.Filter.Eq(request => request.UserId, userId));

    public Task<Result<WorkFromHomeResponseDto>> GetTeam(WorkFromHomeRequestFilterDto filter) => GetPagedRequests(filter, Builders<WorkFromHomeRequest>.Filter.Eq(x => x.ApproverUserId, UserId));

    public Task<Result<WorkFromHomeResponseDto>> GetAll(WorkFromHomeRequestFilterDto filter) => GetPagedRequests(filter, Builders<WorkFromHomeRequest>.Filter.Empty);

    private async Task<Result<WorkFromHomeResponseDto>> GetPagedRequests(WorkFromHomeRequestFilterDto filter, FilterDefinition<WorkFromHomeRequest> scope)
    {
        if (filter is null)
            return Fail<WorkFromHomeResponseDto>("WFH_INVALID_FILTER", "A WFH request filter is required.", StatusCodes.Status400BadRequest);
        if (!await IsWfhAccessibleAsync())
            return new Result<WorkFromHomeResponseDto> { Success = true, MethodResults = [], TotalRecords = 0 };

        var pageNo = Math.Max(1, filter.PageNo);
        var pageSize = NormalizeWfhPageSize(filter.PageSize);
        var fromDate = filter.FromDate.HasValue ? BusinessDateUtc(filter.FromDate.Value.ToDateTime(TimeOnly.MinValue)) : (DateTime?)null;
        var toDate = filter.ToDate.HasValue ? BusinessDateUtc(filter.ToDate.Value.ToDateTime(TimeOnly.MinValue)) : (DateTime?)null;
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            return Fail<WorkFromHomeResponseDto>("WFH_INVALID_DATE_RANGE", "From date cannot be after to date.");
        var allowedStatuses = new[] { "Pending", "Approved", "Rejected", "Returned", "Cancelled" };
        var status = string.IsNullOrWhiteSpace(filter.Status) ? null : filter.Status.Trim();
        if (status is not null && !allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            return Fail<WorkFromHomeResponseDto>("WFH_INVALID_STATUS", "Unsupported WFH status filter.");
        var search = filter.EmployeeSearch?.Trim();
        var department = filter.Department?.Trim();
        if (IsEmployeeSearchTooLong(search))
            return Fail<WorkFromHomeResponseDto>("WFH_EMPLOYEE_SEARCH_TOO_LONG", $"Employee search must be {MaximumEmployeeSearchLength} characters or fewer.", StatusCodes.Status400BadRequest);
        if (IsDepartmentFilterTooLong(department))
            return Fail<WorkFromHomeResponseDto>("WFH_DEPARTMENT_FILTER_TOO_LONG", $"Department filter must be {MaximumDepartmentFilterLength} characters or fewer.", StatusCodes.Status400BadRequest);
        var requestFilter = Builders<WorkFromHomeRequest>.Filter.And(
            Builders<WorkFromHomeRequest>.Filter.Eq(request => request.CompanyId, CompanyId),
            scope);
        if (status != null) requestFilter &= Builders<WorkFromHomeRequest>.Filter.Eq(request => request.Status, status);
        if (fromDate.HasValue) requestFilter &= Builders<WorkFromHomeRequest>.Filter.Gte(request => request.ToDate, fromDate.Value);
        if (toDate.HasValue) requestFilter &= Builders<WorkFromHomeRequest>.Filter.Lte(request => request.FromDate, toDate.Value);

        var matchingEmployees = new Dictionary<string, EmpUser>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(search) || !string.IsNullOrWhiteSpace(department))
        {
            var employeeFilter = BuildTeamEmployeeFilter(CompanyId, search, department);
            List<EmpUser> employeeMatches;
            try
            {
                employeeMatches = await employees.GetCollection().Find(employeeFilter,
                        new FindOptions { MaxTime = EmployeeFilterMaxExecutionTime })
                    .Limit(MaximumEmployeeFilterMatches + 1)
                    .ToListAsync();
            }
            catch (MongoExecutionTimeoutException)
            {
                return Fail<WorkFromHomeResponseDto>("WFH_EMPLOYEE_FILTER_TIMEOUT",
                    "Employee filtering took too long. Add a more specific employee name, ID, or department and retry.",
                    StatusCodes.Status503ServiceUnavailable);
            }
            catch (MongoCommandException exception) when (exception.Code == 50)
            {
                return Fail<WorkFromHomeResponseDto>("WFH_EMPLOYEE_FILTER_TIMEOUT",
                    "Employee filtering took too long. Add a more specific employee name, ID, or department and retry.",
                    StatusCodes.Status503ServiceUnavailable);
            }
            if (IsEmployeeFilterTooBroad(employeeMatches.Count))
                return Fail<WorkFromHomeResponseDto>("WFH_EMPLOYEE_FILTER_TOO_BROAD",
                    $"The employee filter matches more than {MaximumEmployeeFilterMatches} employees. Add a more specific name, employee ID, or department filter.",
                    StatusCodes.Status400BadRequest);
            matchingEmployees = employeeMatches
                .GroupBy(employee => employee.UserId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            if (matchingEmployees.Count == 0)
                return new Result<WorkFromHomeResponseDto> { Success = true, MethodResults = [], TotalRecords = 0 };
            requestFilter &= Builders<WorkFromHomeRequest>.Filter.In(request => request.UserId, matchingEmployees.Keys);
        }

        var collection = requests.GetCollection();
        var total = await collection.CountDocumentsAsync(requestFilter);
        var items = await collection.Find(requestFilter).SortByDescending(request => request.CreatedDate)
            .Skip((pageNo - 1) * pageSize).Limit(pageSize).ToListAsync();
        if (string.IsNullOrWhiteSpace(search) && string.IsNullOrWhiteSpace(department) && items.Count > 0)
        {
            var pageUserIds = items.Select(request => request.UserId).Distinct(StringComparer.Ordinal).ToList();
            matchingEmployees = (await employees.GetCollection().Find(Builders<EmpUser>.Filter.And(
                Builders<EmpUser>.Filter.Eq(employee => employee.CompanyId, CompanyId),
                Builders<EmpUser>.Filter.In(employee => employee.UserId, pageUserIds),
                Builders<EmpUser>.Filter.Eq(employee => employee.IsDeleted, false))).ToListAsync())
                .GroupBy(employee => employee.UserId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }
        var deptMap = await GetDepartmentNameMapAsync();
        return new Result<WorkFromHomeResponseDto>
        {
            Success = true,
            TotalRecords = (int)Math.Min(total, int.MaxValue),
            MethodResults = items.Select(request => Map(request, matchingEmployees.GetValueOrDefault(request.UserId), deptMap)).ToList()
        };
    }

    internal static bool IsEmployeeFilterTooBroad(int matchCount) => matchCount > MaximumEmployeeFilterMatches;
    internal static bool IsEmployeeSearchTooLong(string? search) => (search?.Trim().Length ?? 0) > MaximumEmployeeSearchLength;
    internal static bool IsDepartmentFilterTooLong(string? department) => (department?.Trim().Length ?? 0) > MaximumDepartmentFilterLength;
    internal static bool CanDecideRequest(bool isAssignedApprover, bool hasApproveTeam, bool hasViewAll, bool hasOverride) =>
        hasApproveTeam && (isAssignedApprover || (hasViewAll && hasOverride));
    internal static FilterDefinition<EmpUser> BuildTeamEmployeeFilter(string companyId, string? search, string? department)
    {
        var filter = Builders<EmpUser>.Filter.Eq(employee => employee.CompanyId, companyId) &
            Builders<EmpUser>.Filter.Eq(employee => employee.IsDeleted, false);
        if (!string.IsNullOrWhiteSpace(department))
            filter &= Builders<EmpUser>.Filter.Regex(employee => employee.Department,
                new BsonRegularExpression($"^{Regex.Escape(department.Trim())}$", "i"));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchRegex = new BsonRegularExpression(Regex.Escape(search.Trim()), "i");
            filter &= Builders<EmpUser>.Filter.Or(
                Builders<EmpUser>.Filter.Regex(employee => employee.EmployeeId, searchRegex),
                Builders<EmpUser>.Filter.Regex(employee => employee.FirstName, searchRegex),
                Builders<EmpUser>.Filter.Regex(employee => employee.LastName, searchRegex));
        }
        return filter;
    }

    public async Task<Result> Decide(string requestId, string status, WorkFromHomeDecisionDto dto)
    {
        if (dto is null) return Fail("WFH_INVALID_DECISION", "A WFH review decision payload is required.", StatusCodes.Status400BadRequest);
        if (!await IsWfhAccessibleAsync()) return Fail("WFH_MODULE_DISABLED", "Work from home module is not enabled for this company.");
        if (status is not ("Approved" or "Rejected" or "Returned")) return Fail("WFH_INVALID_STATUS", "Unsupported WFH decision.");
        var request = await requests.FirstOrDefault(x => x.CompanyId == CompanyId && x.RequestId == requestId);
        if (request is null || request.Status != "Pending" || request.Version != dto.Version) return Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.");
        if (request.UserId == UserId) return Fail("WFH_SELF_APPROVAL_NOT_ALLOWED", "A manager cannot approve their own WFH request.");
        var isAssignedApprover = string.Equals(request.ApproverUserId, UserId, StringComparison.Ordinal);
        var hasApproveTeam = await roles.VerifyUserAccess(AppModule.WorkFromHome,
            [Codeji.CMS.Utility.Constraints.Permission.ApproveTeam], UserId, CompanyId);
        var hasViewAll = isAssignedApprover || await roles.VerifyUserAccess(AppModule.WorkFromHome,
            [Codeji.CMS.Utility.Constraints.Permission.ViewAll], UserId, CompanyId);
        var hasOverride = isAssignedApprover || await roles.VerifyUserAccess(AppModule.WorkFromHome,
            [Codeji.CMS.Utility.Constraints.Permission.Override], UserId, CompanyId);
        if (!CanDecideRequest(isAssignedApprover, hasApproveTeam, hasViewAll, hasOverride))
            return Fail("WFH_APPROVER_NOT_AUTHORIZED", "Assigned approvers need ApproveTeam. Reviewing another approver's request also requires ViewAll and Override.", StatusCodes.Status403Forbidden);

        var previous = request.Status;
        var update = new Result { Success = true };
        if (status == "Approved")
        {
            var policy = await ActivePolicy();
            var employee = await ActiveEmployee(request.UserId);
            if (policy is null) return Fail("WFH_POLICY_DISABLED", "WFH policy is disabled.");
            if (employee is null) return Fail("WFH_EMPLOYEE_NOT_ELIGIBLE", "Employee is no longer active.");
            var approval = await ApproveAndReconcileTransaction(request, employee, policy, dto);
            if (!approval.Success) return approval;
        }
        else
        {
            request.Status = status; request.Version++; request.ReviewedByUserId = UserId; request.ReviewedAt = DateTime.UtcNow;
            request.ReviewRemarksCode = dto.RemarksCode; request.ReviewRemarksText = Clean(dto.RemarksText);
            request.UpdatedBy = UserId;
            request.UpdatedDate = DateTime.UtcNow;
            var actor = UserId;
            var auditEvent = BuildAuditOutboxEvent(request, previous, status, status, dto.RemarksCode, dto.RemarksText, actor);
            var transitionEvents = new List<WorkFromHomeOutboxEvent> { auditEvent };
            var employee = await ActiveEmployee(request.UserId);
            if (employee is not null)
            {
                var title = status == "Rejected" ? "Your Work From Home request was rejected." : "Your Work From Home request was returned.";
                var notificationType = status == "Rejected" ? EnumsHelper.NotificationTypes.WorkFromHomeRejected : EnumsHelper.NotificationTypes.WorkFromHomeRequest;
                transitionEvents.Add(BuildEmployeeNotificationOutboxEvent(request, employee, title, notificationType, status, actor));
            }
            try
            {
                using var session = await mongoClient.StartSessionAsync();
                await session.WithTransactionAsync(async (transaction, cancellationToken) =>
                {
                    var saved = await requests.GetCollection().ReplaceOneAsync(transaction,
                        Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.RequestId == requestId && x.Status == previous && x.Version == dto.Version),
                        request, cancellationToken: cancellationToken);
                    if (saved.MatchedCount != 1) throw new InvalidOperationException("WFH_REQUEST_CHANGED");
                    await InsertOutboxEventsAsync(transaction, transitionEvents, cancellationToken);
                    return true;
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "WFH_REQUEST_CHANGED")
            {
                return Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.", StatusCodes.Status409Conflict);
            }
            catch (Exception ex) when (ex is MongoCommandException mongoException && mongoException.Code == 20)
            {
                var saved = await requests.GetCollection().ReplaceOneAsync(
                    Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.RequestId == requestId && x.Status == previous && x.Version == dto.Version),
                    request);
                if (saved.MatchedCount != 1) return Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.", StatusCodes.Status409Conflict);
                await InsertOutboxEventsAsync(null, transitionEvents, CancellationToken.None);
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is MongoCommandException mongoException && mongoException.Code == 20)
            {
                return Fail("WFH_TRANSACTION_REQUIRED", "WFH decisions require MongoDB transaction support. No decision was applied.", StatusCodes.Status503ServiceUnavailable);
            }
            catch
            {
                return Fail("WFH_DECISION_FAILED", "The request decision could not be completed. No decision was applied.", StatusCodes.Status503ServiceUnavailable);
            }
        }
        return update;
    }

    private async Task<Result> ApproveAndReconcileTransaction(WorkFromHomeRequest request, EmpUser employee, WorkFromHomePolicy policy, WorkFromHomeDecisionDto dto)
    {
        var previous = request.Status;
        var previousUpdatedBy = request.UpdatedBy;
        var previousUpdatedDate = request.UpdatedDate;
        request.Status = "Approved";
        request.Version++;
        request.ReviewedByUserId = UserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewRemarksCode = dto.RemarksCode;
        request.ReviewRemarksText = Clean(dto.RemarksText);
        request.UpdatedBy = UserId;
        request.UpdatedDate = DateTime.UtcNow;
        var auditEvent = BuildAuditOutboxEvent(request, previous, "Approved", "Approved", dto.RemarksCode, dto.RemarksText, UserId);
        var notificationEvent = BuildEmployeeNotificationOutboxEvent(request, employee,
            "Your Work From Home request was approved.", EnumsHelper.NotificationTypes.WorkFromHomeApproved, "Approved", UserId);
        var changedDates = new List<DateTime>();
        var employeeForInvalidation = employee;
        var committed = false;
        try
        {
            using var session = await mongoClient.StartSessionAsync();
            await session.WithTransactionAsync(async (transaction, cancellationToken) =>
            {
                await AcquireWfhQuotaLock(transaction, request.UserId, cancellationToken);
                await AcquireWfhPolicyQuotaLock(transaction, policy, cancellationToken);
                // Re-read the policy and employee from the transaction snapshot after
                // acquiring the policy/quota locks. A request may sit pending while
                // eligibility, lifecycle, or policy assignment changes.
                var currentPolicy = await policies.GetCollection()
                    .Find(transaction, x => x.CompanyId == CompanyId && x.PolicyId == policy.PolicyId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (currentPolicy is null || !currentPolicy.IsEnabled || currentPolicy.Version != policy.Version)
                    throw new InvalidOperationException("WFH_POLICY_CHANGED");
                var currentEmployee = await employees.GetCollection()
                    .Find(transaction, BuildApprovalEmployeeFilter(CompanyId, request.UserId))
                    .FirstOrDefaultAsync(cancellationToken);
                if (currentEmployee is null || currentEmployee.EmployeeId != request.EmployeeId || !IsEligible(currentPolicy, currentEmployee))
                    throw new InvalidOperationException("WFH_EMPLOYEE_NO_LONGER_ELIGIBLE");
                employeeForInvalidation = currentEmployee;
                if (await HasActiveLeaveOverlap(request.UserId, request.FromDate, request.ToDate, request.DurationType))
                    throw new InvalidOperationException("WFH_LEAVE_OVERLAP");
                var reconciliation = await ReconcileApproved(request, currentEmployee, currentPolicy, request.Version, transaction, changedDates);
                if (!reconciliation.Success) throw new InvalidOperationException(reconciliation.Message ?? "WFH_RECONCILIATION_FAILED");
                var requestWrite = await requests.GetCollection().ReplaceOneAsync(transaction,
                    Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.RequestId == request.RequestId && x.Status == previous && x.Version == dto.Version),
                    request, cancellationToken: cancellationToken);
                if (requestWrite.MatchedCount != 1) throw new InvalidOperationException("WFH_REQUEST_CHANGED");
                await InsertOutboxEventsAsync(transaction, [auditEvent, notificationEvent], cancellationToken);
                return true;
            });
            committed = true;
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_LEAVE_OVERLAP")
        {
            return Fail("WFH_LEAVE_OVERLAP", "The Work From Home request overlaps an active leave request.");
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_REQUEST_CHANGED")
        {
            return Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.", StatusCodes.Status409Conflict);
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_POLICY_CHANGED")
        {
            return Fail("WFH_POLICY_CHANGED", "The WFH policy changed while this request was under review. Refresh and retry approval.", StatusCodes.Status409Conflict);
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_EMPLOYEE_NO_LONGER_ELIGIBLE")
        {
            return Fail("WFH_EMPLOYEE_NO_LONGER_ELIGIBLE", "The employee is no longer eligible for WFH. The request remains pending and no attendance was changed.", StatusCodes.Status409Conflict);
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_ATTENDANCE_WRITE_CONFLICT")
        {
            return Fail("WFH_DATE_ALREADY_OWNED", "Attendance changed while approval was being applied. Refresh and review the request again.", StatusCodes.Status409Conflict);
        }
        catch (InvalidOperationException ex)
        {
            return Fail("WFH_RECONCILIATION_FAILED", ex.Message, StatusCodes.Status409Conflict);
        }
        catch (Exception ex) when (ex is MongoCommandException mongoException && (mongoException.Code == 20 || mongoException.Code == 263))
        {
            var standaloneResult = await ApproveAndReconcileStandaloneAsync(request, employee, policy, dto, previous, auditEvent, notificationEvent, changedDates);
            if (standaloneResult.Success) committed = true;
            return standaloneResult;
        }
        catch (Exception ex) when (ex is NotSupportedException || ex is MongoCommandException mongoException && mongoException.Code == 20)
        {
            return Fail("WFH_TRANSACTION_REQUIRED", "WFH approval requires MongoDB transaction support. The request remains pending.", StatusCodes.Status503ServiceUnavailable);
        }
        catch
        {
            return Fail("WFH_APPROVAL_FAILED", "Approval could not be completed. The request remains pending and attendance was rolled back.", StatusCodes.Status503ServiceUnavailable);
        }
        finally
        {
            if (!committed)
            {
                request.Status = "Pending";
                request.Version = dto.Version;
                request.ReviewedByUserId = null;
                request.ReviewedAt = null;
                request.ReviewRemarksCode = null;
                request.ReviewRemarksText = null;
                request.UpdatedBy = previousUpdatedBy;
                request.UpdatedDate = previousUpdatedDate;
            }
        }

        foreach (var date in changedDates.Distinct()) await InvalidateSummary(employeeForInvalidation, date);
        return new Result { Success = true, Message = "OK" };
    }

    private async Task<Result> ApproveAndReconcileStandaloneAsync(
        WorkFromHomeRequest request, EmpUser employee, WorkFromHomePolicy policy, WorkFromHomeDecisionDto dto,
        string previous, WorkFromHomeOutboxEvent auditEvent, WorkFromHomeOutboxEvent notificationEvent, List<DateTime> changedDates)
    {
        try
        {
            await AcquireWfhQuotaLock(null, request.UserId, CancellationToken.None);
            await AcquireWfhPolicyQuotaLock(null, policy, CancellationToken.None);
            var currentPolicy = await policies.FirstOrDefault(x => x.CompanyId == CompanyId && x.PolicyId == policy.PolicyId);
            if (currentPolicy is null || !currentPolicy.IsEnabled || currentPolicy.Version != policy.Version)
                return Fail("WFH_POLICY_CHANGED", "The WFH policy changed while this request was under review. Refresh and retry approval.", StatusCodes.Status409Conflict);
            var currentEmployee = await employees.GetCollection().Find(BuildApprovalEmployeeFilter(CompanyId, request.UserId)).FirstOrDefaultAsync();
            if (currentEmployee is null || currentEmployee.EmployeeId != request.EmployeeId || !IsEligible(currentPolicy, currentEmployee))
                return Fail("WFH_EMPLOYEE_NO_LONGER_ELIGIBLE", "The employee is no longer eligible for WFH. The request remains pending and no attendance was changed.", StatusCodes.Status409Conflict);
            if (await HasActiveLeaveOverlap(request.UserId, request.FromDate, request.ToDate, request.DurationType))
                return Fail("WFH_LEAVE_OVERLAP", "The Work From Home request overlaps an active leave request.");
            var reconciliation = await ReconcileApproved(request, currentEmployee, currentPolicy, request.Version, null, changedDates);
            if (!reconciliation.Success) return Fail("WFH_RECONCILIATION_FAILED", reconciliation.Message ?? "WFH_RECONCILIATION_FAILED", StatusCodes.Status409Conflict);
            var requestWrite = await requests.GetCollection().ReplaceOneAsync(
                Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.RequestId == request.RequestId && x.Status == previous && x.Version == dto.Version),
                request);
            if (requestWrite.MatchedCount != 1) return Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.", StatusCodes.Status409Conflict);
            await InsertOutboxEventsAsync(null, [auditEvent, notificationEvent], CancellationToken.None);
            foreach (var date in changedDates.Distinct()) await InvalidateSummary(currentEmployee, date);
            return new Result { Success = true, Message = "OK" };
        }
        catch (Exception ex)
        {
            return Fail("WFH_APPROVAL_FAILED", "Approval could not be completed: " + ex.Message, StatusCodes.Status503ServiceUnavailable);
        }
    }

    public async Task<Result> Cancel(string requestId, WorkFromHomeCancelDto dto)
    {
        if (dto is null) return Fail("WFH_CANCELLATION_INVALID", "A cancellation request is required.", StatusCodes.Status400BadRequest);
        if (!await IsWfhAccessibleAsync()) return Fail("WFH_MODULE_DISABLED", "Work from home module is not enabled for this company.");
        var request = await requests.FirstOrDefault(x => x.CompanyId == CompanyId && x.RequestId == requestId && x.UserId == UserId);
        if (request is null || request.Version != dto.Version) return Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.");
        if (request.Status is not ("Draft" or "Pending" or "Returned" or "Approved"))
            return Fail("WFH_CANNOT_CANCEL", "Only a pending, returned, or approved WFH request may be cancelled.");

        var employee = await ActiveEmployee(request.UserId);
        if (employee is null) return Fail("WFH_EMPLOYEE_NOT_ELIGIBLE", "The employee is no longer active in this company.");
        var policy = await policies.FirstOrDefault(x => x.CompanyId == CompanyId);
        var schedule = await effectiveSchedules.ResolveAsync(CompanyId, employee, request.FromDate);
        var companyNow = CompanyNow(schedule?.TimeZoneId ?? policy?.TimeZoneId);
        var officeStart = schedule?.StartTime ?? policy?.OfficeStartTime;
        if (request.FromDate.Date < companyNow.Date)
            return Fail("WFH_CANNOT_CANCEL_AFTER_START", "A WFH request can only be withdrawn before its first WFH day.");
        var cancelCutoff = request.DurationType == "SecondHalf"
            ? (HalfDayBoundary(officeStart, schedule?.RequiredWorkingMinutes, policy?.FullDayMinimumHours ?? 8m) ?? officeStart)
            : officeStart;
        if (request.FromDate.Date == companyNow.Date && (!cancelCutoff.HasValue || companyNow.TimeOfDay >= cancelCutoff.Value))
        {
            var label = cancelCutoff.HasValue ? DateTime.MinValue.Add(cancelCutoff.Value).ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture) : "the shift start time";
            return Fail("WFH_CANNOT_CANCEL_AFTER_START", $"Today's Work From Home request can only be withdrawn before your shift start time of {label}.");
        }

        var previousStatus = request.Status;
        WorkFromHomeOutboxEvent auditEvent;
        if (request.Status == "Approved")
        {
            // Inspect every affected business date before the first delete. A
            // clocked or locked later date must not leave earlier dates partially
            // restored when the multi-day request cannot be cancelled.
            for (var day = request.FromDate.Date; day <= request.ToDate.Date; day = day.AddDays(1))
            {
                if ((request.OfficeConversions ?? []).Any(x => x.AttendanceDate.Date == day)) continue;
                try { await attendanceEditGuard.EnsureEditableWorkingDayAsync(CompanyId, employee.UserId, day); }
                catch (InvalidOperationException ex) { return Fail("WFH_ATTENDANCE_NOT_EDITABLE", ex.Message); }
                if (request.DurationType is "FirstHalf" or "SecondHalf")
                {
                    var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : "SECOND_HALF";
                    var segment = await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1) && x.Segment == segmentName && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId);
                    var isClockedSegment = (segment?.CheckInTime is not null || segment?.CheckOutTime is not null) && !(segment?.Remarks?.StartsWith("Generated from approved half-day WFH request") == true);
                    if (isClockedSegment) return Fail("WFH_CANNOT_CANCEL_AFTER_CLOCKING", "A clocked WFH attendance segment cannot be cancelled through the employee workflow.");
                }
                else
                {
                    var row = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1));
                    var isClockedRow = (row?.CheckInTime is not null || row?.CheckOutTime is not null) && !(row?.Remarks?.StartsWith("Generated from approved WFH request") == true);
                    if (isClockedRow) return Fail("WFH_CANNOT_CANCEL_AFTER_CLOCKING", "A clocked WFH attendance record cannot be cancelled through the employee workflow.");
                }
            }
            var approvedVersion = request.Version;
            request.Status = "Cancelled";
            request.Version++;
            request.CancelledAt = DateTime.UtcNow;
            request.CancelledByUserId = UserId;
            request.UpdatedBy = UserId;
            request.UpdatedDate = DateTime.UtcNow;
            auditEvent = BuildAuditOutboxEvent(request, previousStatus, "Cancelled", "Cancelled", dto.RemarksCode, dto.RemarksText, UserId);
            var cancelEvents = await BuildCancellationNotificationOutboxEventsAsync(request, employee,
                previousStatus == "Approved" ? "Approved Work From Home request cancelled" : "Work From Home request cancelled", dto.RemarksText, UserId);
            cancelEvents.Insert(0, auditEvent);

            // Attendance removal and request cancellation commit together. A
            // clock or HR edit that races the preflight changes the row version;
            // the transaction then aborts without leaving a partially removed
            // multi-day request.
            try
            {
                using var session = await mongoClient.StartSessionAsync();
                await session.WithTransactionAsync(async (transaction, cancellationToken) =>
                {
                    foreach (var day in Days(request.FromDate, request.ToDate))
                    {
                        if ((request.OfficeConversions ?? []).Any(x => x.AttendanceDate.Date == day.Date)) continue;
                        if (request.DurationType is "FirstHalf" or "SecondHalf")
                        {
                            var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : "SECOND_HALF";
                            var segmentFilter = Builders<AttendanceDaySegment>.Filter.Where(x =>
                                x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1) &&
                                x.Segment == segmentName && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId);
                            var segment = await attendanceSegments.GetCollection().Find(transaction, segmentFilter)
                                .FirstOrDefaultAsync(cancellationToken);
                            if (segment is not null)
                            {
                                var isClockedSegment = (segment.CheckInTime is not null || segment.CheckOutTime is not null) &&
                                    segment.Remarks?.StartsWith("Generated from approved half-day WFH request") != true;
                                if (isClockedSegment) throw new InvalidOperationException("WFH_CANCEL_CLOCKED");
                                var delete = await attendanceSegments.GetCollection().DeleteOneAsync(transaction,
                                    Builders<AttendanceDaySegment>.Filter.Where(x =>
                                        x.CompanyId == CompanyId && x.UserId == employee.UserId &&
                                        x.AttendanceDaySegmentId == segment.AttendanceDaySegmentId &&
                                        x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId && x.Version == segment.Version),
                                    options: null, cancellationToken: cancellationToken);
                                if (delete.DeletedCount != 1) throw new InvalidOperationException("WFH_CANCEL_ATTENDANCE_CHANGED");

                                var dailyRow = await attendance.GetCollection().Find(transaction,
                                    Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1))).FirstOrDefaultAsync(cancellationToken);
                                if (dailyRow is not null)
                                {
                                    var remainingSegments = await attendanceSegments.GetCollection().Find(transaction,
                                        Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1) && x.AttendanceDaySegmentId != segment.AttendanceDaySegmentId)).ToListAsync(cancellationToken);
                                    if (remainingSegments.Count == 0)
                                    {
                                        if (dailyRow.SourceType == "WFH_REQUEST")
                                        {
                                            await attendance.GetCollection().DeleteOneAsync(transaction,
                                                Builders<AttendanceModel>.Filter.Where(x => x.AttendanceId == dailyRow.AttendanceId && x.CompanyId == CompanyId && x.UserId == employee.UserId),
                                                cancellationToken: cancellationToken);
                                        }
                                    }
                                    else
                                    {
                                        var opp = remainingSegments[0];
                                        dailyRow.Status = opp.Status == "P" ? "HD" : opp.Status;
                                        dailyRow.SourceType = opp.SourceType;
                                        dailyRow.SourceId = opp.SourceId;
                                        dailyRow.SourceVersion = opp.SourceVersion;
                                        dailyRow.Remarks = $"Retained companion {opp.Segment}: {opp.Remarks}";
                                        dailyRow.Version++;
                                        await attendance.GetCollection().ReplaceOneAsync(transaction,
                                            Builders<AttendanceModel>.Filter.Where(x => x.AttendanceId == dailyRow.AttendanceId && x.CompanyId == CompanyId && x.UserId == employee.UserId),
                                            dailyRow, cancellationToken: cancellationToken);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var rowFilter = Builders<AttendanceModel>.Filter.Where(x =>
                                x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1) &&
                                x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId);
                            var row = await attendance.GetCollection().Find(transaction, rowFilter).FirstOrDefaultAsync(cancellationToken);
                            if (row is not null)
                            {
                                var isClockedRow = (row.CheckInTime is not null || row.CheckOutTime is not null) &&
                                    row.Remarks?.StartsWith("Generated from approved WFH request") != true;
                                if (isClockedRow) throw new InvalidOperationException("WFH_CANCEL_CLOCKED");
                                var delete = await attendance.GetCollection().DeleteOneAsync(transaction,
                                    Builders<AttendanceModel>.Filter.Where(x =>
                                        x.CompanyId == CompanyId && x.UserId == employee.UserId && x.AttendanceId == row.AttendanceId &&
                                        x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId && x.Version == row.Version),
                                    options: null, cancellationToken: cancellationToken);
                                if (delete.DeletedCount != 1) throw new InvalidOperationException("WFH_CANCEL_ATTENDANCE_CHANGED");
                            }
                        }
                    }

                    var savedRequest = await requests.GetCollection().ReplaceOneAsync(transaction,
                        Builders<WorkFromHomeRequest>.Filter.Where(x =>
                            x.CompanyId == CompanyId && x.UserId == employee.UserId && x.RequestId == requestId &&
                            x.Status == "Approved" && x.Version == approvedVersion),
                        request, cancellationToken: cancellationToken);
                    if (savedRequest.MatchedCount != 1) throw new InvalidOperationException("WFH_CANCEL_REQUEST_CHANGED");
                    await InsertOutboxEventsAsync(transaction, cancelEvents, cancellationToken);
                    return true;
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "WFH_CANCEL_CLOCKED")
            {
                return Fail("WFH_CANNOT_CANCEL_AFTER_CLOCKING", "A clocked WFH attendance record cannot be cancelled through the employee workflow.");
            }
            catch (InvalidOperationException ex) when (ex.Message is "WFH_CANCEL_ATTENDANCE_CHANGED" or "WFH_CANCEL_REQUEST_CHANGED")
            {
                return Fail("WFH_REQUEST_CHANGED", "Attendance or the request changed while cancelling. Refresh and try again.", StatusCodes.Status409Conflict);
            }
            catch (Exception ex) when (ex is MongoCommandException mongoException && (mongoException.Code == 20 || mongoException.Code == 263))
            {
                foreach (var day in Days(request.FromDate, request.ToDate))
                {
                    if ((request.OfficeConversions ?? []).Any(x => x.AttendanceDate.Date == day.Date)) continue;
                    if (request.DurationType is "FirstHalf" or "SecondHalf")
                    {
                        var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : "SECOND_HALF";
                        var segmentFilter = Builders<AttendanceDaySegment>.Filter.Where(x =>
                            x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1) &&
                            x.Segment == segmentName && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId);
                        var segment = await attendanceSegments.GetCollection().Find(segmentFilter).FirstOrDefaultAsync();
                        if (segment is not null)
                        {
                            var isClockedSegment = (segment.CheckInTime is not null || segment.CheckOutTime is not null) &&
                                segment.Remarks?.StartsWith("Generated from approved half-day WFH request") != true;
                            if (isClockedSegment) return Fail("WFH_CANNOT_CANCEL_AFTER_CLOCKING", "A clocked WFH attendance record cannot be cancelled through the employee workflow.");
                            var delete = await attendanceSegments.GetCollection().DeleteOneAsync(
                                Builders<AttendanceDaySegment>.Filter.Where(x =>
                                    x.CompanyId == CompanyId && x.UserId == employee.UserId &&
                                    x.AttendanceDaySegmentId == segment.AttendanceDaySegmentId &&
                                    x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId && x.Version == segment.Version));
                            if (delete.DeletedCount != 1) return Fail("WFH_REQUEST_CHANGED", "Attendance or the request changed while cancelling. Refresh and try again.", StatusCodes.Status409Conflict);

                            var dailyRow = await attendance.GetCollection().Find(
                                Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1))).FirstOrDefaultAsync();
                            if (dailyRow is not null)
                            {
                                var remainingSegments = await attendanceSegments.GetCollection().Find(
                                    Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1) && x.AttendanceDaySegmentId != segment.AttendanceDaySegmentId)).ToListAsync();
                                if (remainingSegments.Count == 0)
                                {
                                    if (dailyRow.SourceType == "WFH_REQUEST")
                                    {
                                        await attendance.GetCollection().DeleteOneAsync(
                                            Builders<AttendanceModel>.Filter.Where(x => x.AttendanceId == dailyRow.AttendanceId && x.CompanyId == CompanyId && x.UserId == employee.UserId));
                                    }
                                }
                                else
                                {
                                    var opp = remainingSegments[0];
                                    dailyRow.Status = opp.Status == "P" ? "HD" : opp.Status;
                                    dailyRow.SourceType = opp.SourceType;
                                    dailyRow.SourceId = opp.SourceId;
                                    dailyRow.SourceVersion = opp.SourceVersion;
                                    dailyRow.Remarks = $"Retained companion {opp.Segment}: {opp.Remarks}";
                                    dailyRow.Version++;
                                    await attendance.GetCollection().ReplaceOneAsync(
                                        Builders<AttendanceModel>.Filter.Where(x => x.AttendanceId == dailyRow.AttendanceId && x.CompanyId == CompanyId && x.UserId == employee.UserId),
                                        dailyRow);
                                }
                            }
                        }
                    }
                    else
                    {
                        var rowFilter = Builders<AttendanceModel>.Filter.Where(x =>
                            x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1) &&
                            x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId);
                        var row = await attendance.GetCollection().Find(rowFilter).FirstOrDefaultAsync();
                        if (row is not null)
                        {
                            var isClockedRow = (row.CheckInTime is not null || row.CheckOutTime is not null) &&
                                row.Remarks?.StartsWith("Generated from approved WFH request") != true;
                            if (isClockedRow) return Fail("WFH_CANNOT_CANCEL_AFTER_CLOCKING", "A clocked WFH attendance record cannot be cancelled through the employee workflow.");
                            var delete = await attendance.GetCollection().DeleteOneAsync(
                                Builders<AttendanceModel>.Filter.Where(x =>
                                    x.CompanyId == CompanyId && x.UserId == employee.UserId && x.AttendanceId == row.AttendanceId &&
                                    x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId && x.Version == row.Version));
                            if (delete.DeletedCount != 1) return Fail("WFH_REQUEST_CHANGED", "Attendance or the request changed while cancelling. Refresh and try again.", StatusCodes.Status409Conflict);
                        }
                    }
                }

                var savedRequest = await requests.GetCollection().ReplaceOneAsync(
                    Builders<WorkFromHomeRequest>.Filter.Where(x =>
                        x.CompanyId == CompanyId && x.UserId == employee.UserId && x.RequestId == requestId &&
                        x.Status == "Approved" && x.Version == approvedVersion),
                    request);
                if (savedRequest.MatchedCount != 1) return Fail("WFH_REQUEST_CHANGED", "Attendance or the request changed while cancelling. Refresh and try again.", StatusCodes.Status409Conflict);
                await InsertOutboxEventsAsync(null, cancelEvents, CancellationToken.None);
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is MongoCommandException mongoException && mongoException.Code == 20)
            {
                return Fail("WFH_CANCELLATION_TRANSACTION_REQUIRED", "Approved WFH cancellation requires a MongoDB deployment that supports transactions. No cancellation was applied.", StatusCodes.Status503ServiceUnavailable);
            }

            foreach (var month in Days(request.FromDate, request.ToDate).Select(day => new DateTime(day.Year, day.Month, 1)).Distinct())
                await attendanceInitialization.InitializeMonthAsync(CompanyId, UserId, month);
        }
        else
        {
            request.Status = "Cancelled";
            request.Version++;
            request.CancelledAt = DateTime.UtcNow;
            request.CancelledByUserId = UserId;
            request.UpdatedBy = UserId;
            request.UpdatedDate = DateTime.UtcNow;
            auditEvent = BuildAuditOutboxEvent(request, previousStatus, "Cancelled", "Cancelled", dto.RemarksCode, dto.RemarksText, UserId);
            var cancelEvents = await BuildCancellationNotificationOutboxEventsAsync(request, employee,
                previousStatus == "Approved" ? "Approved Work From Home request cancelled" : "Work From Home request cancelled", dto.RemarksText, UserId);
            cancelEvents.Insert(0, auditEvent);
            try
            {
                using var session = await mongoClient.StartSessionAsync();
                await session.WithTransactionAsync(async (transaction, cancellationToken) =>
                {
                    var saved = await requests.GetCollection().ReplaceOneAsync(transaction,
                        Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == UserId &&
                            x.RequestId == requestId && x.Status == previousStatus && x.Version == dto.Version),
                        request, cancellationToken: cancellationToken);
                    if (saved.MatchedCount != 1) throw new InvalidOperationException("WFH_CANCEL_REQUEST_CHANGED");
                    await InsertOutboxEventsAsync(transaction, cancelEvents, cancellationToken);
                    return true;
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "WFH_CANCEL_REQUEST_CHANGED")
            {
                return Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.", StatusCodes.Status409Conflict);
            }
            catch (Exception ex) when (ex is MongoCommandException mongoException && (mongoException.Code == 20 || mongoException.Code == 263))
            {
                var saved = await requests.GetCollection().ReplaceOneAsync(
                    Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == UserId &&
                        x.RequestId == requestId && x.Status == previousStatus && x.Version == dto.Version),
                    request);
                if (saved.MatchedCount != 1) return Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.", StatusCodes.Status409Conflict);
                await InsertOutboxEventsAsync(null, cancelEvents, CancellationToken.None);
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is MongoCommandException mongoException && mongoException.Code == 20)
            {
                return Fail("WFH_CANCELLATION_TRANSACTION_REQUIRED", "WFH cancellation requires MongoDB transaction support. No cancellation was applied.", StatusCodes.Status503ServiceUnavailable);
            }
            catch
            {
                return Fail("WFH_CANCELLATION_FAILED", "The request could not be cancelled. No cancellation was applied.", StatusCodes.Status503ServiceUnavailable);
            }
        }

        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "WFH request cancelled." };
    }

    public async Task<Result> CheckIn(string requestId) => await RecordTime(requestId, true);
    public async Task<Result> CheckOut(string requestId, WorkFromHomeCheckOutDto? dto = null) => await RecordTime(requestId, false, dto);

    public async Task<Result<WorkFromHomeTimingDto>> GetTiming(string requestId)
    {
        if (!await IsWfhAccessibleAsync()) return Fail<WorkFromHomeTimingDto>("WFH_MODULE_DISABLED", "Work from home module is not enabled for this company.");
        var request = await requests.FirstOrDefault(x => x.CompanyId == CompanyId && x.RequestId == requestId && x.UserId == UserId && x.Status == "Approved");
        if (request is null) return Fail<WorkFromHomeTimingDto>("WFH_REQUEST_NOT_AVAILABLE", "No approved WFH request is available.");
        var policy = await ActivePolicy();
        var employee = await ActiveEmployee(UserId);
        var schedule = employee is null ? null : await effectiveSchedules.ResolveAsync(CompanyId, employee, DateTime.UtcNow);
        var timeZoneId = schedule?.TimeZoneId ?? policy?.TimeZoneId ?? "UTC";
        var today = CompanyNow(timeZoneId).Date;
        if (today < request.FromDate.Date || today > request.ToDate.Date) return Ok(new WorkFromHomeTimingDto());
        if ((request.OfficeConversions ?? []).Any(x => x.AttendanceDate.Date == today)) return Ok(new WorkFromHomeTimingDto { IsApprovedForToday = false });
        var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : request.DurationType == "SecondHalf" ? "SECOND_HALF" : null;
        var row = segmentName is null
            ? await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == UserId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Date >= today && x.Date < today.AddDays(1))
            : null;
        var segment = segmentName is null ? null : await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == UserId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Segment == segmentName && x.Date >= today && x.Date < today.AddDays(1));

        var existingRawRow = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == UserId && x.Date >= today && x.Date < today.AddDays(1));
        if (existingRawRow?.SourceType == "ADMIN_OVERRIDE")
            return Ok(new WorkFromHomeTimingDto { IsApprovedForToday = false });

        // Approval may have inherited placeholder punches from an auto-present row.
        // Treat those as empty for display, but never mutate attendance from this GET.
        // RecordTime clears placeholders transactionally when the employee checks in.
        var rowContainsOnlyGeneratedPlaceholders = row?.Remarks?.StartsWith("Generated from approved WFH request", StringComparison.Ordinal) == true;
        var segmentContainsOnlyGeneratedPlaceholders = segment?.Remarks?.StartsWith("Generated from approved half-day WFH request", StringComparison.Ordinal) == true;
        var currentStatus = row?.Status ?? segment?.Status;
        var checkInTime = rowContainsOnlyGeneratedPlaceholders || segmentContainsOnlyGeneratedPlaceholders
            ? null
            : row?.CheckInTime ?? segment?.CheckInTime;
        var checkOutTime = rowContainsOnlyGeneratedPlaceholders || segmentContainsOnlyGeneratedPlaceholders
            ? null
            : row?.CheckOutTime ?? segment?.CheckOutTime;
        var totalHours = rowContainsOnlyGeneratedPlaceholders || segmentContainsOnlyGeneratedPlaceholders
            ? null
            : row?.TotalHours ?? segment?.TotalHours;
        var halfDayThreshold = policy?.HalfDayMinimumHours ?? 4m;
        var isHalfDay = request.DurationType is "FirstHalf" or "SecondHalf" ||
            string.Equals(currentStatus, policy?.HalfDayAttendanceStatusCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(currentStatus, policy?.WfhHdAttendanceStatusCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(currentStatus, "WFH-HD", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(currentStatus, "HD", StringComparison.OrdinalIgnoreCase);
        var isUnderHours = checkOutTime.HasValue && totalHours.HasValue && totalHours.Value < halfDayThreshold;

        var companyNow = CompanyNow(timeZoneId);
        var currentTime = companyNow.TimeOfDay;
        var checkInFrom = schedule?.CheckInAllowedFrom ?? policy?.AllowedCheckInFrom;
        var checkOutUntil = schedule?.CheckOutAllowedUntil ?? policy?.AllowedCheckOutUntil;
        var wfhCheckInUntil = checkOutUntil ?? schedule?.EndTime ?? policy?.OfficeEndTime;
        var isCheckInOpen = true;
        string? checkInNotice = null;

        if (!checkInTime.HasValue)
        {
            if (checkInFrom.HasValue && currentTime < checkInFrom.Value)
            {
                isCheckInOpen = false;
                checkInNotice = $"WFH check-in opens at {checkInFrom.Value:hh\\:mm}.";
            }
            else if (wfhCheckInUntil.HasValue && currentTime > wfhCheckInUntil.Value)
            {
                isCheckInOpen = false;
                checkInNotice = "WFH check-in closed for today.";
            }
            else if (request.DurationType == "FirstHalf")
            {
                var halfBoundary = HalfDayBoundary(schedule?.StartTime ?? policy?.OfficeStartTime, schedule?.RequiredWorkingMinutes, policy?.FullDayMinimumHours ?? 8m) ?? new TimeSpan(13, 30, 0);
                if (currentTime > halfBoundary)
                {
                    isCheckInOpen = false;
                    checkInNotice = $"First-half WFH check-in closed at {halfBoundary:hh\\:mm}.";
                }
            }
            else if (request.DurationType == "SecondHalf")
            {
                var halfBoundary = HalfDayBoundary(schedule?.StartTime ?? policy?.OfficeStartTime, schedule?.RequiredWorkingMinutes, policy?.FullDayMinimumHours ?? 8m) ?? new TimeSpan(13, 30, 0);
                if (currentTime < halfBoundary)
                {
                    isCheckInOpen = false;
                    checkInNotice = $"Second-half WFH check-in opens at {halfBoundary:hh\\:mm}.";
                }
            }
        }

        return Ok(new WorkFromHomeTimingDto
        {
            IsApprovedForToday = true,
            TimeZoneId = timeZoneId,
            CheckInTime = checkInTime,
            CheckOutTime = checkOutTime,
            TotalHours = totalHours,
            RequiredHours = request.DurationType == "FullDay" ? policy?.FullDayMinimumHours ?? 8m : halfDayThreshold,
            HalfDayRequiredHours = halfDayThreshold,
            OfficeStartTime = policy?.OfficeStartTime,
            OfficeEndTime = policy?.OfficeEndTime,
            CheckInAvailableFrom = policy?.AllowedCheckInFrom,
            CheckInAvailableUntil = policy?.AllowedCheckInUntil,
            CheckOutAvailableFrom = policy?.AllowedCheckOutFrom,
            CheckOutAvailableUntil = policy?.AllowedCheckOutUntil,
            Status = currentStatus,
            IsHalfDay = isHalfDay,
            IsUnderHours = isUnderHours,
            IsCheckInOpen = isCheckInOpen,
            CheckInNotice = checkInNotice,
            Remarks = row?.Remarks ?? segment?.Remarks,
            RemarkCode = row?.RemarkCode
        });
    }

    public async Task<Result> CorrectInsufficientHours(string exceptionId, WorkFromHomeHoursCorrectionDto dto)
    {
        if (dto is null) return Fail("WFH_CORRECTION_INVALID", "A correction request is required.", StatusCodes.Status400BadRequest);
        var exception = await exceptions.FirstOrDefault(x => x.Id == exceptionId && x.CompanyId == CompanyId);
        // A WFH-owned row can also surface through the generic monthly
        // validators (for example, a missing check-out).  It must still be
        // corrected through the approved WFH request, never by overwriting the
        // attendance row through the normal editor.
        var supportedExceptionTypes = new[] { "WFH_INSUFFICIENT_HOURS", "MISSING_CHECKOUT", "INVALID_TIME_ORDER" };
        if (exception is null || !supportedExceptionTypes.Contains(exception.ExceptionType, StringComparer.Ordinal))
            return Fail("WFH_EXCEPTION_NOT_FOUND", "No correctable WFH attendance exception was found.", StatusCodes.Status404NotFound);
        if (exception.Status != "PENDING_REVIEW" || exception.Version != dto.ExceptionVersion)
            return Fail("WFH_EXCEPTION_CHANGED", "This exception was already changed. Refresh and try again.", StatusCodes.Status409Conflict);
        if (dto.CheckOutTime <= dto.CheckInTime)
            return Fail("WFH_INVALID_HOURS", "Check-out time must be later than check-in time.");
        if (string.IsNullOrWhiteSpace(dto.Remarks))
            return Fail("WFH_CORRECTION_REASON_REQUIRED", "A correction reason is required.");

        // Dedicated WFH exceptions carry AttendanceDate, while generic monthly
        // validators (such as MISSING_CHECKOUT) carry their dates in
        // AffectedDates. The UI corrects the first affected date, so use the
        // same date here instead of rejecting an otherwise valid WFH record.
        var attendanceDate = exception.AttendanceDate?.Date
            ?? exception.AffectedDates?.Select(date => date.Date).FirstOrDefault();
        if (!attendanceDate.HasValue || attendanceDate.Value == default)
            return Fail("WFH_EXCEPTION_DATE_MISSING", "The WFH exception does not have an affected attendance date.");
        var dateToCorrect = attendanceDate.Value;
        var employee = await EmployeeEmployedOn(exception.UserId, dateToCorrect);
        if (employee is null) return Fail("WFH_EMPLOYEE_NOT_ELIGIBLE", "The employee was not employed on the affected attendance date.");
        try { await attendanceEditGuard.EnsureEditableWorkingDayAsync(CompanyId, employee.UserId, dateToCorrect); }
        catch (InvalidOperationException ex) { return Fail("WFH_ATTENDANCE_NOT_EDITABLE", ex.Message); }

        // Generic monthly exceptions do not carry the WFH request id. Resolve the
        // source-owned attendance row first, then use its immutable SourceId to find
        // the approved request. This keeps the correction tied to its WFH source.
        var row = string.IsNullOrWhiteSpace(exception.ExistingAttendanceId)
            ? null
            : await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.AttendanceId == exception.ExistingAttendanceId && x.UserId == employee.UserId);
        if (row is null || row.SourceType != "WFH_REQUEST" || string.IsNullOrWhiteSpace(row.SourceId) ||
            (!string.IsNullOrWhiteSpace(exception.LeaveRequestId) && row.SourceId != exception.LeaveRequestId))
        {
            var endOfAttendanceDate = dateToCorrect.AddDays(1);
            row = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.SourceType == "WFH_REQUEST" && x.Date >= dateToCorrect && x.Date < endOfAttendanceDate &&
                (string.IsNullOrWhiteSpace(exception.LeaveRequestId) || x.SourceId == exception.LeaveRequestId));
        }
        AttendanceDaySegment? segment = null;
        if (row is null)
        {
            segment = string.IsNullOrWhiteSpace(exception.ExistingAttendanceId)
                ? null
                : await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.AttendanceDaySegmentId == exception.ExistingAttendanceId && x.UserId == employee.UserId);
            if (segment is null || segment.SourceType != "WFH_REQUEST" || string.IsNullOrWhiteSpace(segment.SourceId) ||
                (!string.IsNullOrWhiteSpace(exception.LeaveRequestId) && segment.SourceId != exception.LeaveRequestId))
            {
                var endOfAttendanceDate = dateToCorrect.AddDays(1);
                segment = await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.SourceType == "WFH_REQUEST" && x.Date >= dateToCorrect && x.Date < endOfAttendanceDate &&
                    (string.IsNullOrWhiteSpace(exception.LeaveRequestId) || x.SourceId == exception.LeaveRequestId));
            }
        }
        if (row is null && (segment is null || segment.SourceType != "WFH_REQUEST" || string.IsNullOrWhiteSpace(segment.SourceId)))
            return Fail("WFH_ATTENDANCE_CONFLICT", "No WFH-owned attendance record matches this employee and affected date. Refresh the exception list and verify the approved WFH request.");

        var sourceId = row?.SourceId ?? segment!.SourceId;
        var request = await requests.FirstOrDefault(x => x.CompanyId == CompanyId && x.RequestId == sourceId && x.UserId == employee.UserId && x.Status == "Approved");
        if (request is null) return Fail("WFH_REQUEST_NOT_AVAILABLE", "The approved WFH request linked to the attendance record is no longer available.");
        var policy = await ActivePolicy();
        if (policy is null) return Fail("WFH_POLICY_DISABLED", "WFH policy is disabled.");
        var schedule = await effectiveSchedules.ResolveAsync(CompanyId, employee, dateToCorrect);
        var timeZoneId = schedule?.TimeZoneId ?? policy.TimeZoneId;
        if (CompanyDate(dto.CheckInTime, timeZoneId) != dateToCorrect || CompanyDate(dto.CheckOutTime, timeZoneId) != dateToCorrect)
            return Fail("WFH_CORRECTION_DATE_INVALID", "Both corrected times must be on the affected attendance date.");

        var minimum = request.DurationType == "FullDay" ? policy.FullDayMinimumHours : policy.HalfDayMinimumHours;
        // Apply the effective office break exactly as normal attendance and WFH
        // check-out do. A 09:00-18:00 office day with a 60-minute break is eight
        // verified working hours, not nine.
        var breakMinutes = Math.Max(0, schedule?.BreakMinutes ?? row?.BreakMinutes ?? 0);
        var totalHours = Math.Round(Math.Max(0, (decimal)(dto.CheckOutTime - dto.CheckInTime).TotalHours - (breakMinutes / 60m)), 2, MidpointRounding.AwayFromZero);
        if (totalHours < minimum)
            return Fail("WFH_HOURS_STILL_INSUFFICIENT", $"Corrected WFH hours are {totalHours:0.##}; at least {minimum:0.##} hours are required.");

        if (segment is not null)
        {
            var originalSegmentVersion = segment.Version;
            segment.Status = "WFH";
            segment.CheckInTime = dto.CheckInTime; segment.CheckOutTime = dto.CheckOutTime; segment.TotalHours = totalHours;
            segment.Remarks = $"HR/Admin corrected WFH hours: {Clean(dto.Remarks)}"; segment.UpdatedAtUtc = DateTime.UtcNow; segment.Version++;
            exception.Status = "RESOLVED"; exception.Resolution = $"WFH hours corrected to {totalHours:0.##} hour(s) by HR/Admin.";
            exception.ReviewedBy = UserId; exception.ReviewedAt = DateTime.UtcNow; exception.UpdatedBy = UserId; exception.UpdatedDate = DateTime.UtcNow; exception.Version++;
            var auditEvent = BuildAuditOutboxEvent(request, request.Status, request.Status, "HoursCorrected", "WFH_HOURS_CORRECTED", dto.Remarks, UserId);

            var dailyRow = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= dateToCorrect && x.Date < dateToCorrect.AddDays(1));
            var originalDailyRowVersion = dailyRow?.Version;
            if (dailyRow is not null)
            {
                dailyRow.CheckInTime = dto.CheckInTime;
                dailyRow.CheckOutTime = dto.CheckOutTime;
                dailyRow.TotalHours = totalHours;
                dailyRow.EarlyExitCount = 0;
                dailyRow.Remarks = $"HR/Admin corrected half-day WFH hours: {Clean(dto.Remarks)}";
                dailyRow.Version++;
            }

            try
            {
                using var session = await mongoClient.StartSessionAsync();
                await session.WithTransactionAsync(async (transaction, cancellationToken) =>
                {
                    var segmentWrite = await attendanceSegments.GetCollection().ReplaceOneAsync(transaction,
                        Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.AttendanceDaySegmentId == segment.AttendanceDaySegmentId && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId && x.Version == originalSegmentVersion),
                        segment, cancellationToken: cancellationToken);
                    if (segmentWrite.MatchedCount != 1) throw new InvalidOperationException("WFH_ATTENDANCE_UPDATE_FAILED");
                    if (dailyRow is not null)
                    {
                        var dailyWrite = await attendance.GetCollection().ReplaceOneAsync(transaction,
                            Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.AttendanceId == dailyRow.AttendanceId && x.Version == originalDailyRowVersion),
                            dailyRow, cancellationToken: cancellationToken);
                        if (dailyWrite.MatchedCount != 1) throw new InvalidOperationException("WFH_ATTENDANCE_UPDATE_FAILED");
                    }
                    var exceptionWrite = await exceptions.GetCollection().ReplaceOneAsync(transaction,
                        Builders<AttendancePayrollException>.Filter.Where(x => x.Id == exception.Id && x.CompanyId == CompanyId && x.Status == "PENDING_REVIEW" && x.Version == dto.ExceptionVersion),
                        exception, cancellationToken: cancellationToken);
                    if (exceptionWrite.MatchedCount != 1) throw new InvalidOperationException("WFH_EXCEPTION_CHANGED");
                    await InsertOutboxEventsAsync(transaction, [auditEvent], cancellationToken);
                    return true;
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "WFH_ATTENDANCE_UPDATE_FAILED") { return Fail("WFH_ATTENDANCE_UPDATE_FAILED", "The half-day WFH attendance record changed before correction.", StatusCodes.Status409Conflict); }
            catch (InvalidOperationException ex) when (ex.Message == "WFH_EXCEPTION_CHANGED") { return Fail("WFH_EXCEPTION_CHANGED", "This exception was already changed. Refresh and try again.", StatusCodes.Status409Conflict); }
            catch (Exception ex) when (ex is NotSupportedException || ex is MongoCommandException mongoException && mongoException.Code == 20) { return Fail("WFH_TRANSACTION_REQUIRED", "Correcting WFH hours requires MongoDB transaction support. No attendance or exception changes were applied.", StatusCodes.Status503ServiceUnavailable); }
            catch { return Fail("WFH_CORRECTION_FAILED", "WFH hours could not be corrected. No attendance or exception changes were applied.", StatusCodes.Status503ServiceUnavailable); }
            await InvalidateSummary(employee, dateToCorrect);
            return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = $"WFH hours updated to {totalHours:0.##} hours and the exception was resolved." };
        }

        var originalRowVersion = row.Version;
        row.CheckInTime = dto.CheckInTime;
        row.CheckOutTime = dto.CheckOutTime;
        row.TotalHours = totalHours;
        row.BreakMinutes = breakMinutes;
        row.Status = policy.FullDayAttendanceStatusCode?.Trim().ToUpperInvariant() ?? "WFH";
        row.EarlyExitCount = 0;
        row.RemarkCode = "WFH_HOURS_CORRECTED";
        row.Remarks = $"HR/Admin corrected WFH hours: {Clean(dto.Remarks)}";
        row.Version++;
        exception.Status = "RESOLVED";
        exception.Resolution = $"WFH hours corrected to {totalHours:0.##} hour(s) by HR/Admin.";
        exception.ReviewedBy = UserId;
        exception.ReviewedAt = DateTime.UtcNow;
        exception.UpdatedBy = UserId;
        exception.UpdatedDate = DateTime.UtcNow;
        exception.Version++;
        var rowAuditEvent = BuildAuditOutboxEvent(request, request.Status, request.Status, "HoursCorrected", "WFH_HOURS_CORRECTED", dto.Remarks, UserId);
        try
        {
            using var session = await mongoClient.StartSessionAsync();
            await session.WithTransactionAsync(async (transaction, cancellationToken) =>
            {
                var attendanceWrite = await attendance.GetCollection().ReplaceOneAsync(transaction,
                    Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.AttendanceId == row.AttendanceId && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId && x.Version == originalRowVersion), row, cancellationToken: cancellationToken);
                if (attendanceWrite.MatchedCount != 1) throw new InvalidOperationException("WFH_ATTENDANCE_UPDATE_FAILED");
                var exceptionWrite = await exceptions.GetCollection().ReplaceOneAsync(transaction,
                    Builders<AttendancePayrollException>.Filter.Where(x => x.Id == exception.Id && x.CompanyId == CompanyId && x.Status == "PENDING_REVIEW" && x.Version == dto.ExceptionVersion), exception, cancellationToken: cancellationToken);
                if (exceptionWrite.MatchedCount != 1) throw new InvalidOperationException("WFH_EXCEPTION_CHANGED");
                await InsertOutboxEventsAsync(transaction, [rowAuditEvent], cancellationToken);
                return true;
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_ATTENDANCE_UPDATE_FAILED")
        {
            return Fail("WFH_ATTENDANCE_UPDATE_FAILED", "The WFH attendance record could not be updated.");
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_EXCEPTION_CHANGED")
        {
            return Fail("WFH_EXCEPTION_CHANGED", "This exception was already changed. Refresh and try again.", StatusCodes.Status409Conflict);
        }
        catch (Exception ex) when (ex is NotSupportedException || ex is MongoCommandException mongoException && mongoException.Code == 20)
        { return Fail("WFH_TRANSACTION_REQUIRED", "Correcting WFH hours requires MongoDB transaction support. No attendance or exception changes were applied.", StatusCodes.Status503ServiceUnavailable); }
        catch { return Fail("WFH_CORRECTION_FAILED", "WFH hours could not be corrected. No attendance or exception changes were applied.", StatusCodes.Status503ServiceUnavailable); }
        await InvalidateSummary(employee, dateToCorrect);
        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = $"WFH hours updated to {totalHours:0.##} hours and the exception was resolved." };
    }

    public async Task<Result> ResolveAttendanceConflict(string exceptionId, WorkFromHomeConflictResolutionDto dto)
    {
        if (dto is null) return Fail("WFH_CONFLICT_DECISION_INVALID", "A conflict resolution decision is required.");
        var exception = await exceptions.FirstOrDefault(x => x.Id == exceptionId && x.CompanyId == CompanyId);
        if (exception is null || exception.ExceptionType is not ("WFH_LEAVE_CONFLICT" or "WFH_ATTENDANCE_CONFLICT" or "WFH_ATTENDANCE_SEGMENT_CONFLICT" or "LEAVE_ATTENDANCE_CONFLICT" or "LEAVE_ATTENDANCE_SEGMENT_CONFLICT")) return Fail("WFH_EXCEPTION_NOT_FOUND", "No resolvable WFH conflict was found.", StatusCodes.Status404NotFound);
        if (exception.Status != "PENDING_REVIEW" || exception.Version != dto.ExceptionVersion) return Fail("WFH_EXCEPTION_CHANGED", "This exception was already changed. Refresh and try again.", StatusCodes.Status409Conflict);
        if (dto.Resolution is not ("KEEP_LEAVE_CANCEL_WFH" or "KEEP_WFH_CANCEL_LEAVE") || string.IsNullOrWhiteSpace(dto.Remarks) || dto.Remarks.Trim().Length is < 3 or > 500)
            return Fail("WFH_CONFLICT_DECISION_INVALID", "Choose a supported conflict decision and provide a 3 to 500 character reason.");
        if (string.IsNullOrWhiteSpace(exception.LeaveRequestId) || string.IsNullOrWhiteSpace(exception.WorkFromHomeRequestId))
            return Fail("WFH_CONFLICT_SOURCE_LINK_UNAVAILABLE", "This legacy conflict does not safely identify both source requests. No request, attendance, or exception was changed.", StatusCodes.Status409Conflict);
        if (!await roles.VerifyUserAccess(AppModule.Attendance, [Codeji.CMS.Utility.Constraints.Permission.Edit], UserId, CompanyId))
            return Fail("WFH_CONFLICT_NOT_AUTHORIZED", "Attendance edit permission is required to resolve this conflict.", StatusCodes.Status403Forbidden);

        var leave = await leaveRequests.FirstOrDefault(x => x.CompanyId == CompanyId && x.LeaveRequestId == exception.LeaveRequestId &&
            x.EmployeeId == exception.UserId && x.Status == EnumsHelper.LeaveRequestStatus.Accepted);
        if (leave is null) return Fail("WFH_CONFLICT_LEAVE_NOT_AVAILABLE", "The linked approved leave request is no longer available.", StatusCodes.Status409Conflict);

        if (dto.Resolution == "KEEP_LEAVE_CANCEL_WFH")
            return await ResolveKeepLeaveCancelWfhAsync(exception, leave, dto);

        var withdrawn = await leaveManagement.WithdrawAcceptedLeaveForWfhConflictAsync(leave.LeaveRequestId, dto.Remarks.Trim(), async (transaction, currentLeave) =>
        {
            var supportedTypes = new[] { "WFH_LEAVE_CONFLICT", "LEAVE_ATTENDANCE_CONFLICT", "LEAVE_ATTENDANCE_SEGMENT_CONFLICT", "LEAVE_APPLIED_FOR_LOCKED_MONTH" };
            var conflictFilter = Builders<AttendancePayrollException>.Filter.Where(x =>
                x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId && x.LeaveRequestId == currentLeave.LeaveRequestId &&
                x.Status == "PENDING_REVIEW" && supportedTypes.Contains(x.ExceptionType));
            var pendingConflicts = await exceptions.GetCollection().Find(transaction, conflictFilter).ToListAsync();
            if (pendingConflicts.Count == 0 || pendingConflicts.All(x => x.Id != exception.Id))
                return Fail("WFH_CONFLICT_CHANGED", "The linked conflict changed before the withdrawal could be applied.", StatusCodes.Status409Conflict);
            if (pendingConflicts.Any(x => x.ExceptionType == "LEAVE_APPLIED_FOR_LOCKED_MONTH"))
                return Fail("WFH_CONFLICT_LOCKED_PAYROLL", "A linked leave exception is in a locked payroll month. Unlock or reconcile that month before withdrawing the leave.", StatusCodes.Status409Conflict);
            var leaveMonths = Days(currentLeave.StartDate, currentLeave.EndDate)
                .Select(date => new DateTime(date.Year, date.Month, 1)).Distinct().ToList();
            if (await summaries.GetCollection().Find(transaction,
                    Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == CompanyId &&
                        x.UserId == currentLeave.EmployeeId && leaveMonths.Contains(x.PayrollMonth) && x.IsLocked))
                .AnyAsync())
                return Fail("WFH_CONFLICT_LOCKED_PAYROLL", "A month in the leave request is payroll-locked. Unlock or reconcile it before withdrawing the leave.", StatusCodes.Status409Conflict);
            if (pendingConflicts.Any(x => string.IsNullOrWhiteSpace(x.WorkFromHomeRequestId) || string.IsNullOrWhiteSpace(x.ExistingAttendanceId)))
                return Fail("WFH_CONFLICT_SOURCE_LINK_UNAVAILABLE", "At least one leave conflict has no verified WFH attendance source. No linked records were changed.", StatusCodes.Status409Conflict);

            var wfhRequestIds = pendingConflicts.Select(x => x.WorkFromHomeRequestId!).Distinct(StringComparer.Ordinal).ToList();
            var linkedWfhRequests = new List<WorkFromHomeRequest>();
            foreach (var linkedRequestId in wfhRequestIds)
            {
                var linkedRequest = await requests.GetCollection().Find(transaction,
                    x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId && x.RequestId == linkedRequestId && x.Status == "Approved")
                    .FirstOrDefaultAsync();
                if (linkedRequest is null)
                    return Fail("WFH_CONFLICT_SOURCE_CHANGED", "A linked WFH request is no longer approved in this company. No linked records were changed.", StatusCodes.Status409Conflict);
                linkedWfhRequests.Add(linkedRequest);
            }

            foreach (var conflict in pendingConflicts)
            {
                if (conflict.SourceVersion > currentLeave.Version)
                    return Fail("WFH_CONFLICT_SOURCE_CHANGED", "A linked conflict references a newer leave version than the approved request. No linked records were changed.", StatusCodes.Status409Conflict);
                var affectedDate = conflict.AttendanceDate?.Date;
                if (!affectedDate.HasValue || affectedDate.Value < currentLeave.StartDate.Date || affectedDate.Value > currentLeave.EndDate.Date)
                    return Fail("WFH_CONFLICT_DATE_INVALID", "A linked conflict date is outside the approved leave request.", StatusCodes.Status409Conflict);
                var sourceRequest = linkedWfhRequests.First(x => x.RequestId == conflict.WorkFromHomeRequestId);
                if (affectedDate.Value < sourceRequest.FromDate.Date || affectedDate.Value > sourceRequest.ToDate.Date ||
                    (sourceRequest.OfficeConversions ?? []).Any(x => x.AttendanceDate.Date == affectedDate.Value))
                    return Fail("WFH_CONFLICT_SOURCE_CHANGED", "The linked WFH request no longer owns the affected date.", StatusCodes.Status409Conflict);

                var row = await attendance.GetCollection().Find(transaction,
                    x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId && x.AttendanceId == conflict.ExistingAttendanceId &&
                        x.SourceType == "WFH_REQUEST" && x.SourceId == sourceRequest.RequestId && x.Date >= affectedDate.Value && x.Date < affectedDate.Value.AddDays(1))
                    .FirstOrDefaultAsync();
                var segment = row is null
                    ? await attendanceSegments.GetCollection().Find(transaction,
                        x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId && x.AttendanceDaySegmentId == conflict.ExistingAttendanceId &&
                            x.SourceType == "WFH_REQUEST" && x.SourceId == sourceRequest.RequestId && x.Date >= affectedDate.Value && x.Date < affectedDate.Value.AddDays(1))
                        .FirstOrDefaultAsync()
                    : null;
                if (row is null && segment is null)
                    return Fail("WFH_CONFLICT_SOURCE_CHANGED", "The exception no longer points to WFH-owned attendance for the same employee and date.", StatusCodes.Status409Conflict);
            }

            // Cancel the whole leave source: some non-conflicting dates may already
            // have generated leave attendance before reconciliation found a conflict.
            await attendance.GetCollection().DeleteManyAsync(transaction,
                Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                    x.SourceType == "LEAVE" && x.SourceId == currentLeave.LeaveRequestId && x.SourceVersion <= currentLeave.Version));
            await attendanceSegments.GetCollection().DeleteManyAsync(transaction,
                Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                    x.SourceType == "LEAVE" && x.SourceId == currentLeave.LeaveRequestId && x.SourceVersion <= currentLeave.Version));

            foreach (var conflict in pendingConflicts)
            {
                var expectedVersion = conflict.Version;
                conflict.Status = "RESOLVED";
                conflict.Decision = dto.Resolution;
                conflict.Resolution = $"Leave {currentLeave.LeaveRequestId} withdrawn; approved WFH retained. {dto.Remarks.Trim()}";
                conflict.ReviewedBy = UserId;
                conflict.ReviewedAt = DateTime.UtcNow;
                conflict.UpdatedBy = UserId;
                conflict.UpdatedDate = DateTime.UtcNow;
                conflict.Version++;
                var saved = await exceptions.GetCollection().ReplaceOneAsync(transaction,
                    Builders<AttendancePayrollException>.Filter.Where(x => x.Id == conflict.Id && x.CompanyId == CompanyId &&
                        x.UserId == currentLeave.EmployeeId && x.LeaveRequestId == currentLeave.LeaveRequestId &&
                        x.Status == "PENDING_REVIEW" && x.Version == expectedVersion), conflict);
                if (saved.MatchedCount != 1)
                    return Fail("WFH_CONFLICT_CHANGED", "A linked attendance exception changed during conflict resolution.", StatusCodes.Status409Conflict);
            }

            var auditEvents = linkedWfhRequests.Select(linkedRequest => BuildAuditOutboxEvent(linkedRequest,
                linkedRequest.Status, linkedRequest.Status, "LeaveConflictResolvedKeepWfh", "WFH_LEAVE_CONFLICT_RESOLVED",
                $"Leave request {currentLeave.LeaveRequestId} withdrawn. {dto.Remarks.Trim()}", UserId)).ToList();
            await InsertOutboxEventsAsync(transaction, auditEvents, CancellationToken.None);
            foreach (var month in Days(currentLeave.StartDate, currentLeave.EndDate)
                .Select(date => new DateTime(date.Year, date.Month, 1)).Distinct())
            {
                await summaries.GetCollection().DeleteManyAsync(transaction,
                    Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == CompanyId &&
                        x.UserId == currentLeave.EmployeeId && x.PayrollMonth == month && !x.IsLocked));
            }
            return new Result { Success = true };
        });
        if (!withdrawn.Success) return withdrawn;

        try
        {
            foreach (var month in Days(leave.StartDate, leave.EndDate).Select(date => new DateTime(date.Year, date.Month, 1)).Distinct())
                await attendanceInitialization.InitializeEmployeeAttendanceAsync(CompanyId, UserId, leave.EmployeeId, month);
        }
        catch
        {
            return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Approved WFH retained; linked leave and its balance were withdrawn, and conflict exceptions were resolved. Attendance summary regeneration needs retry." };
        }
        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Approved WFH retained; linked leave and its balance were withdrawn, and conflict exceptions were resolved." };
    }

    private async Task<Result> ResolveKeepLeaveCancelWfhAsync(
        AttendancePayrollException selectedException,
        LeaveRequest leave,
        WorkFromHomeConflictResolutionDto dto)
    {
        var cancelledRequests = new List<WorkFromHomeRequest>();
        var result = await leaveManagement.ExecuteAcceptedLeaveWfhConflictTransactionAsync(
            leave.LeaveRequestId, dto.Remarks.Trim(), async (transaction, currentLeave) =>
            {
                var supportedTypes = new[] { "WFH_LEAVE_CONFLICT", "LEAVE_ATTENDANCE_CONFLICT", "LEAVE_ATTENDANCE_SEGMENT_CONFLICT", "LEAVE_APPLIED_FOR_LOCKED_MONTH" };
                var pending = await exceptions.GetCollection().Find(transaction,
                    Builders<AttendancePayrollException>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                        x.LeaveRequestId == currentLeave.LeaveRequestId && x.Status == "PENDING_REVIEW")).ToListAsync();
                if (pending.Count == 0 || pending.All(x => x.Id != selectedException.Id))
                    return Fail("WFH_CONFLICT_CHANGED", "The linked conflict changed before resolution.", StatusCodes.Status409Conflict);
                if (pending.Any(x => !supportedTypes.Contains(x.ExceptionType)))
                    return Fail("WFH_CONFLICT_TYPE_UNSUPPORTED", "Another pending leave exception is not supported by this WFH conflict action.", StatusCodes.Status409Conflict);
                if (pending.Any(x => x.ExceptionType == "LEAVE_APPLIED_FOR_LOCKED_MONTH"))
                    return Fail("WFH_CONFLICT_LOCKED_PAYROLL", "A linked leave exception is in a locked payroll month. Unlock or reconcile that month first.", StatusCodes.Status409Conflict);
                if (pending.Any(x => string.IsNullOrWhiteSpace(x.WorkFromHomeRequestId) || string.IsNullOrWhiteSpace(x.ExistingAttendanceId)))
                    return Fail("WFH_CONFLICT_SOURCE_LINK_UNAVAILABLE", "At least one pending leave conflict does not identify its WFH attendance source. No records were changed.", StatusCodes.Status409Conflict);
                if (pending.Any(x => x.SourceVersion > currentLeave.Version))
                    return Fail("WFH_CONFLICT_SOURCE_CHANGED", "A pending conflict refers to a newer leave version.", StatusCodes.Status409Conflict);

                var wfhRequestIds = pending.Select(x => x.WorkFromHomeRequestId!).Distinct(StringComparer.Ordinal).ToList();
                var linkedRequests = await requests.GetCollection().Find(transaction,
                    Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                        wfhRequestIds.Contains(x.RequestId) && x.Status == "Approved")).ToListAsync();
                if (linkedRequests.Count != wfhRequestIds.Count)
                    return Fail("WFH_CONFLICT_SOURCE_CHANGED", "A linked WFH request is no longer approved in this company.", StatusCodes.Status409Conflict);

                var affectedDays = new Dictionary<string, HashSet<DateTime>>(StringComparer.Ordinal);
                foreach (var wfhRequest in linkedRequests)
                {
                    affectedDays[wfhRequest.RequestId] = Days(wfhRequest.FromDate, wfhRequest.ToDate).Select(x => x.Date).ToHashSet();
                    if (pending.Where(x => x.WorkFromHomeRequestId == wfhRequest.RequestId).Any(conflict =>
                        !conflict.AttendanceDate.HasValue || conflict.AttendanceDate.Value.Date < currentLeave.StartDate.Date ||
                        conflict.AttendanceDate.Value.Date > currentLeave.EndDate.Date ||
                        !affectedDays[wfhRequest.RequestId].Contains(conflict.AttendanceDate.Value.Date) ||
                        (wfhRequest.OfficeConversions ?? []).Any(c => c.AttendanceDate.Date == conflict.AttendanceDate.Value.Date)))
                        return Fail("WFH_CONFLICT_DATE_INVALID", "A linked conflict date is outside the overlapping, non-converted leave/WFH dates.", StatusCodes.Status409Conflict);
                }

                var touchedMonths = linkedRequests.SelectMany(x => Days(x.FromDate, x.ToDate))
                    .Concat(Days(currentLeave.StartDate, currentLeave.EndDate))
                    .Select(x => new DateTime(x.Year, x.Month, 1)).Distinct().ToList();
                if (await summaries.GetCollection().Find(transaction,
                    Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                        touchedMonths.Contains(x.PayrollMonth) && x.IsLocked)).AnyAsync())
                    return Fail("WFH_CONFLICT_LOCKED_PAYROLL", "A linked WFH or leave date is in a payroll-locked month. Unlock that month before resolving this conflict.", StatusCodes.Status409Conflict);

                foreach (var conflict in pending)
                {
                    var linkedRequest = linkedRequests.First(x => x.RequestId == conflict.WorkFromHomeRequestId);
                    var date = conflict.AttendanceDate!.Value.Date;
                    var row = await attendance.GetCollection().Find(transaction,
                        Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                            x.AttendanceId == conflict.ExistingAttendanceId && x.Date >= date && x.Date < date.AddDays(1) &&
                            x.SourceType == "WFH_REQUEST" && x.SourceId == linkedRequest.RequestId)).FirstOrDefaultAsync();
                    var segment = row is null
                        ? await attendanceSegments.GetCollection().Find(transaction,
                            Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                                x.AttendanceDaySegmentId == conflict.ExistingAttendanceId && x.Date >= date && x.Date < date.AddDays(1) &&
                                x.SourceType == "WFH_REQUEST" && x.SourceId == linkedRequest.RequestId)).FirstOrDefaultAsync()
                        : null;
                    if (row is null && segment is null)
                        return Fail("WFH_CONFLICT_SOURCE_CHANGED", "An exception no longer points to WFH-owned attendance for this employee and date.", StatusCodes.Status409Conflict);
                }

                // Cancellation is request-wide because the persisted WFH request has
                // no per-date cancellation state. Refuse to erase clocked or HR-edited
                // attendance from any other date in a multi-day request.
                foreach (var linkedRequest in linkedRequests)
                {
                    var rows = await attendance.GetCollection().Find(transaction,
                        Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                            x.SourceType == "WFH_REQUEST" && x.SourceId == linkedRequest.RequestId)).ToListAsync();
                    var segments = await attendanceSegments.GetCollection().Find(transaction,
                        Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                            x.SourceType == "WFH_REQUEST" && x.SourceId == linkedRequest.RequestId)).ToListAsync();
                    if (rows.Any(x => x.CheckInTime.HasValue || x.CheckOutTime.HasValue || x.TotalHours.HasValue ||
                                      !(x.Remarks?.StartsWith("Generated from approved WFH request", StringComparison.Ordinal) ?? false)) ||
                        segments.Any(x => x.CheckInTime.HasValue || x.CheckOutTime.HasValue || x.TotalHours.HasValue ||
                                          !(x.Remarks?.StartsWith("Generated from approved half-day WFH request", StringComparison.Ordinal) ?? false)))
                        return Fail("WFH_CONFLICT_CLOCKED_REQUEST", "A linked multi-day WFH request contains clocked or HR-edited attendance. Resolve those recorded work dates before cancelling the full request.", StatusCodes.Status409Conflict);

                    foreach (var row in rows)
                    {
                        var deleted = await attendance.GetCollection().DeleteOneAsync(transaction,
                            Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                                x.AttendanceId == row.AttendanceId && x.SourceType == "WFH_REQUEST" && x.SourceId == linkedRequest.RequestId && x.Version == row.Version));
                        if (deleted.DeletedCount != 1)
                            return Fail("WFH_CONFLICT_ATTENDANCE_CHANGED", "A WFH attendance row changed during conflict resolution.", StatusCodes.Status409Conflict);
                    }
                    foreach (var segment in segments)
                    {
                        var deleted = await attendanceSegments.GetCollection().DeleteOneAsync(transaction,
                            Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                                x.AttendanceDaySegmentId == segment.AttendanceDaySegmentId && x.SourceType == "WFH_REQUEST" &&
                                x.SourceId == linkedRequest.RequestId && x.Version == segment.Version));
                        if (deleted.DeletedCount != 1)
                            return Fail("WFH_CONFLICT_ATTENDANCE_CHANGED", "A WFH attendance segment changed during conflict resolution.", StatusCodes.Status409Conflict);
                    }

                    var expectedVersion = linkedRequest.Version;
                    var previousStatus = linkedRequest.Status;
                    linkedRequest.Status = "Cancelled";
                    linkedRequest.Version++;
                    linkedRequest.CancelledAt = DateTime.UtcNow;
                    linkedRequest.CancelledByUserId = UserId;
                    linkedRequest.UpdatedBy = UserId;
                    linkedRequest.UpdatedDate = DateTime.UtcNow;
                    linkedRequest.ReviewRemarksText = $"Cancelled to keep approved leave {currentLeave.LeaveRequestId}. {dto.Remarks.Trim()}";
                    var requestSaved = await requests.GetCollection().ReplaceOneAsync(transaction,
                        Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == currentLeave.EmployeeId &&
                            x.RequestId == linkedRequest.RequestId && x.Status == "Approved" && x.Version == expectedVersion), linkedRequest);
                    if (requestSaved.MatchedCount != 1)
                        return Fail("WFH_CONFLICT_REQUEST_CHANGED", "A linked WFH request changed during conflict resolution.", StatusCodes.Status409Conflict);
                    var auditEvent = BuildAuditOutboxEvent(linkedRequest, previousStatus, linkedRequest.Status,
                        "LeaveConflictResolvedKeepLeave", "WFH_CANCELLED_KEEP_LEAVE", dto.Remarks.Trim(), UserId);
                    await InsertOutboxEventsAsync(transaction, [auditEvent], CancellationToken.None);
                    cancelledRequests.Add(linkedRequest);
                }

                var reconciled = await leaveAttendanceReconciliation.ReconcileAcceptedLeaveInTransactionAsync(
                    transaction, CompanyId, currentLeave, UserId, CancellationToken.None);
                if (!reconciled.Success) return reconciled;

                foreach (var conflict in pending)
                {
                    var expectedVersion = conflict.Version;
                    conflict.Status = "RESOLVED";
                    conflict.Decision = dto.Resolution;
                    conflict.Resolution = $"WFH request(s) cancelled; approved leave retained. {dto.Remarks.Trim()}";
                    conflict.ReviewedBy = UserId;
                    conflict.ReviewedAt = DateTime.UtcNow;
                    conflict.UpdatedBy = UserId;
                    conflict.UpdatedDate = DateTime.UtcNow;
                    conflict.Version++;
                    var saved = await exceptions.GetCollection().ReplaceOneAsync(transaction,
                        Builders<AttendancePayrollException>.Filter.Where(x => x.Id == conflict.Id && x.CompanyId == CompanyId &&
                            x.UserId == currentLeave.EmployeeId && x.LeaveRequestId == currentLeave.LeaveRequestId &&
                            x.Status == "PENDING_REVIEW" && x.Version == expectedVersion), conflict);
                    if (saved.MatchedCount != 1)
                        return Fail("WFH_CONFLICT_CHANGED", "A linked exception changed during conflict resolution.", StatusCodes.Status409Conflict);
                }

                foreach (var month in touchedMonths)
                    await summaries.GetCollection().DeleteManyAsync(transaction,
                        Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == CompanyId &&
                            x.UserId == currentLeave.EmployeeId && x.PayrollMonth == month && !x.IsLocked));
                return new Result { Success = true };
            });

        if (!result.Success) return result;
        try
        {
            foreach (var month in cancelledRequests.SelectMany(x => Days(x.FromDate, x.ToDate))
                .Concat(Days(leave.StartDate, leave.EndDate))
                .Select(x => new DateTime(x.Year, x.Month, 1)).Distinct())
                await attendanceInitialization.InitializeMonthAsync(CompanyId, UserId, month);
        }
        catch
        {
            return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Approved leave was retained and linked WFH request(s) were cancelled. Attendance summary regeneration needs retry." };
        }
        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Approved leave was retained, leave attendance was restored, and linked WFH request(s) were cancelled." };
    }

    public async Task<Result<WorkFromHomeDeadLetterDto>> GetDeadLetterOutbox(int pageNo, int pageSize)
    {
        if (!await IsWfhAccessibleAsync())
            return Fail<WorkFromHomeDeadLetterDto>("WFH_MODULE_DISABLED", "Work from home module is not enabled for this company.", StatusCodes.Status403Forbidden);

        pageNo = Math.Max(1, pageNo);
        pageSize = pageSize is 10 or 25 or 50 or 100 ? pageSize : 25;
        var filter = BuildDeadLetterOutboxFilter(CompanyId);
        var collection = outbox.GetCollection();
        var total = await collection.CountDocumentsAsync(filter);
        var skip = (int)Math.Min((long)(pageNo - 1) * pageSize, int.MaxValue);
        var items = await collection.Find(filter)
            .SortByDescending(x => x.CreatedDate)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
        return new Result<WorkFromHomeDeadLetterDto>
        {
            Success = true,
            TotalRecords = (int)Math.Min(total, int.MaxValue),
            MethodResults = items.Select(x => new WorkFromHomeDeadLetterDto
            {
                EventId = x.EventId,
                RequestId = x.RequestId,
                EventType = x.EventType,
                CreatedDate = x.CreatedDate,
                AvailableAtUtc = x.AvailableAtUtc,
                AttemptCount = x.AttemptCount,
                ReplayCount = x.ReplayCount,
                ReplayedAtUtc = x.ReplayedAtUtc,
                ReplayedByUserId = x.ReplayedByUserId
            }).ToList()
        };
    }

    public async Task<Result> ReplayDeadLetterOutbox(string eventId, WorkFromHomeDeadLetterReplayDto dto)
    {
        if (!await IsWfhAccessibleAsync()) return Fail("WFH_MODULE_DISABLED", "Work from home module is not enabled for this company.", StatusCodes.Status403Forbidden);
        if (string.IsNullOrWhiteSpace(eventId) || dto is null || string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length is < 3 or > 500)
            return Fail("WFH_OUTBOX_REPLAY_REASON_REQUIRED", "Provide a replay reason between 3 and 500 characters.");

        var now = DateTime.UtcNow;
        var reason = Clean(dto.Reason) ?? string.Empty;
        var filter = Builders<WorkFromHomeOutboxEvent>.Filter.And(
            Builders<WorkFromHomeOutboxEvent>.Filter.Eq(x => x.CompanyId, CompanyId),
            Builders<WorkFromHomeOutboxEvent>.Filter.Eq(x => x.EventId, eventId),
            Builders<WorkFromHomeOutboxEvent>.Filter.Eq(x => x.Status, "DeadLetter"),
            Builders<WorkFromHomeOutboxEvent>.Filter.Eq(x => x.IsDeleted, false));
        var update = Builders<WorkFromHomeOutboxEvent>.Update
            .Set(x => x.Status, "Pending")
            .Set(x => x.AttemptCount, 0)
            .Set(x => x.AvailableAtUtc, now)
            .Set(x => x.ProcessingStartedAtUtc, null)
            .Set(x => x.LastError, null)
            .Set(x => x.ReplayedAtUtc, now)
            .Set(x => x.ReplayedByUserId, UserId)
            .Inc(x => x.ReplayCount, 1)
            .Push(x => x.ReplayHistory, new WorkFromHomeOutboxReplayEntry { ActorUserId = UserId, ReplayedAtUtc = now, Reason = reason })
            .Set(x => x.UpdatedBy, UserId)
            .Set(x => x.UpdatedDate, now);
        try
        {
            var result = await outbox.GetCollection().UpdateOneAsync(filter, update);
            if (result.MatchedCount != 1)
                return Fail("WFH_OUTBOX_DEAD_LETTER_NOT_FOUND", "The dead-letter event was not found for this company or was already replayed.", StatusCodes.Status404NotFound);
            return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "WFH outbox event queued for retry." };
        }
        catch
        {
            return Fail("WFH_OUTBOX_REPLAY_FAILED", "The dead-letter event could not be queued for retry.", StatusCodes.Status503ServiceUnavailable);
        }
    }

    internal static FilterDefinition<WorkFromHomeOutboxEvent> BuildDeadLetterOutboxFilter(string companyId) =>
        Builders<WorkFromHomeOutboxEvent>.Filter.Where(x => x.CompanyId == companyId && x.Status == "DeadLetter" && !x.IsDeleted);

    public async Task<Result> RegularizeAttendance(string requestId, WorkFromHomeRegularizeAttendanceDto dto)
    {
        if (dto is null) return Fail("WFH_REGULARIZATION_INVALID", "An attendance regularization request is required.", StatusCodes.Status400BadRequest);
        if (!await IsWfhAccessibleAsync()) return Fail("WFH_MODULE_DISABLED", "Work from home module is not enabled for this company.");
        var isCompanyReviewer = await roles.VerifyUserAccess(AppModule.WorkFromHome, [Codeji.CMS.Utility.Constraints.Permission.ViewAll, Codeji.CMS.Utility.Constraints.Permission.ApproveTeam], UserId, CompanyId);
        var request = await requests.FirstOrDefault(x => x.CompanyId == CompanyId && x.RequestId == requestId);
        if (request is null || request.Status != "Approved") return Fail("WFH_REQUEST_NOT_FOUND", "An approved WFH request was not found.", StatusCodes.Status404NotFound);
        if (!string.Equals(request.ApproverUserId, UserId, StringComparison.Ordinal) && !isCompanyReviewer)
            return Fail("WFH_APPROVER_NOT_AUTHORIZED", "You are not authorized to regularize this request.", StatusCodes.Status403Forbidden);

        if (string.IsNullOrWhiteSpace(dto.AttendanceStatus))
            return Fail("WFH_STATUS_REQUIRED", "Attendance status is required.");
        if (string.IsNullOrWhiteSpace(dto.Remarks))
            return Fail("WFH_REMARKS_REQUIRED", "Remarks are required for attendance regularization.");

        var statusCode = dto.AttendanceStatus.Trim().ToUpperInvariant();
        if (!await ValidCode(statusCode))
            return Fail("WFH_STATUS_INVALID", $"Attendance status '{statusCode}' is not active or valid for this company.");

        var employee = await EmployeeEmployedOn(request.UserId, request.FromDate);
        if (employee is null)
            return Fail("WFH_EMPLOYEE_NOT_FOUND", "Employee record was not found.");

        var row = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId);
        AttendanceDaySegment? segment = null;
        if (row is null)
        {
            segment = await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId);
        }

        if (row is null && segment is null)
            return Fail("WFH_ATTENDANCE_NOT_FOUND", "No WFH attendance record was found for this request.", StatusCodes.Status404NotFound);

        var targetDate = row?.Date.Date ?? segment!.Date.Date;
        try { await attendanceEditGuard.EnsureEditableWorkingDayAsync(CompanyId, employee.UserId, targetDate); }
        catch (InvalidOperationException ex) { return Fail("WFH_ATTENDANCE_NOT_EDITABLE", ex.Message); }

        var dailyRow = row ?? await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= targetDate && x.Date < targetDate.AddDays(1));
        var auditEvent = BuildAuditOutboxEvent(request, request.Status, request.Status, "AttendanceRegularized", "WFH_REGULARIZED_BY_ADMIN", dto.Remarks, UserId);
        var originalRowVersion = row?.Version;
        var originalDailyRowVersion = dailyRow?.Version;
        var originalSegmentVersion = segment?.Version;
        AttendancePayrollException? insufficientHoursException = null;
        var linkedExceptions = (await exceptions.GetAll(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.LeaveRequestId == requestId && x.Status == "PENDING_REVIEW")).ToList();
        if (row is not null)
        {
            row.Status = statusCode;
            row.RemarkCode = "WFH_REGULARIZED_BY_ADMIN";
            if (dto.TotalHours.HasValue && dto.TotalHours.Value > 0)
            {
                row.TotalHours = dto.TotalHours.Value;
            }
            row.Remarks = $"Regularized by HR/Admin as {statusCode}: {dto.Remarks.Trim()} ({requestId})";
            row.OperatorRemark = dto.Remarks.Trim();
            row.ModifiedByUserId = UserId;
            row.ModifiedAtUtc = DateTime.UtcNow;
            row.Version++;

        }
        else
        {
            segment!.Status = statusCode;
            if (dto.TotalHours.HasValue && dto.TotalHours.Value > 0)
            {
                segment.TotalHours = dto.TotalHours.Value;
            }
            segment.Remarks = $"Regularized by HR/Admin as {statusCode}: {dto.Remarks.Trim()} ({requestId})";
            segment.UpdatedAtUtc = DateTime.UtcNow;

        }
        foreach (var exc in linkedExceptions)
        {
            exc.Status = "RESOLVED";
            exc.Resolution = $"Regularized to {statusCode} by HR/Admin: {dto.Remarks.Trim()}";
            exc.ReviewedBy = UserId;
            exc.ReviewedAt = DateTime.UtcNow;
            exc.UpdatedBy = UserId;
            exc.UpdatedDate = DateTime.UtcNow;
            exc.Version++;
        }
        try
        {
            using var session = await mongoClient.StartSessionAsync();
            await session.WithTransactionAsync(async (transaction, cancellationToken) =>
            {
                if (row is not null)
                {
                    var write = await attendance.GetCollection().ReplaceOneAsync(transaction,
                        Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.AttendanceId == row.AttendanceId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Version == originalRowVersion), row, cancellationToken: cancellationToken);
                    if (write.MatchedCount != 1) throw new InvalidOperationException("WFH_ATTENDANCE_CHANGED");
                }
                else
                {
                    var write = await attendanceSegments.GetCollection().ReplaceOneAsync(transaction,
                        Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.AttendanceDaySegmentId == segment!.AttendanceDaySegmentId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Version == originalSegmentVersion), segment!, cancellationToken: cancellationToken);
                    if (write.MatchedCount != 1) throw new InvalidOperationException("WFH_ATTENDANCE_CHANGED");
                }
                foreach (var exc in linkedExceptions)
                {
                    var expectedVersion = exc.Version - 1;
                    var write = await exceptions.GetCollection().ReplaceOneAsync(transaction,
                        Builders<AttendancePayrollException>.Filter.Where(x => x.Id == exc.Id && x.CompanyId == CompanyId && x.UserId == request.UserId && x.LeaveRequestId == requestId && x.Status == "PENDING_REVIEW" && x.Version == expectedVersion), exc, cancellationToken: cancellationToken);
                    if (write.MatchedCount != 1) throw new InvalidOperationException("WFH_EXCEPTION_CHANGED");
                }
                await InsertOutboxEventsAsync(transaction, [auditEvent], cancellationToken);
                return true;
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_ATTENDANCE_CHANGED") { return Fail("WFH_ATTENDANCE_CHANGED", "Attendance changed during regularization. Refresh and try again.", StatusCodes.Status409Conflict); }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_EXCEPTION_CHANGED") { return Fail("WFH_EXCEPTION_CHANGED", "An attendance exception changed during regularization. Refresh and try again.", StatusCodes.Status409Conflict); }
        catch (Exception ex) when (ex is NotSupportedException || ex is MongoCommandException mongoException && mongoException.Code == 20) { return Fail("WFH_TRANSACTION_REQUIRED", "WFH attendance regularization requires MongoDB transaction support. No changes were applied.", StatusCodes.Status503ServiceUnavailable); }
        catch { return Fail("WFH_REGULARIZATION_FAILED", "WFH attendance could not be regularized. No changes were applied.", StatusCodes.Status503ServiceUnavailable); }
        await InvalidateSummary(employee, targetDate);

        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = $"WFH attendance regularized to {statusCode}." };
    }

    public async Task<Result> ConvertApprovedWfhToOffice(string requestId, WorkFromHomeOfficeConversionDto dto)
    {
        if (dto is null) return Fail("WFH_CONVERSION_INVALID", "An office conversion payload is required.", StatusCodes.Status400BadRequest);
        if (!await IsWfhAccessibleAsync()) return Fail("WFH_MODULE_DISABLED", "Work from home module is not enabled for this company.");
        if (string.IsNullOrWhiteSpace(dto.Remarks) || dto.Remarks.Trim().Length < 3)
            return Fail("WFH_CONVERSION_REASON_REQUIRED", "Provide at least three characters explaining the office attendance conversion.");

        var request = await requests.FirstOrDefault(x => x.CompanyId == CompanyId && x.RequestId == requestId && x.Status == "Approved");
        if (request is null) return Fail("WFH_REQUEST_NOT_FOUND", "An approved WFH request was not found.", StatusCodes.Status404NotFound);
        if (request.Version != dto.Version) return Fail("WFH_REQUEST_CHANGED", "The request changed. Refresh it and try again.", StatusCodes.Status409Conflict);

        var isCompanyReviewer = await roles.VerifyUserAccess(AppModule.WorkFromHome,
            [Codeji.CMS.Utility.Constraints.Permission.ViewAll, Codeji.CMS.Utility.Constraints.Permission.ApproveTeam], UserId, CompanyId);
        if (!string.Equals(request.ApproverUserId, UserId, StringComparison.Ordinal) && !isCompanyReviewer)
            return Fail("WFH_APPROVER_NOT_AUTHORIZED", "You are not authorized to convert this WFH attendance.", StatusCodes.Status403Forbidden);

        var day = BusinessDateUtc(dto.AttendanceDate);
        if (day < request.FromDate.Date || day > request.ToDate.Date)
            return Fail("WFH_CONVERSION_DATE_NOT_COVERED", "The selected date is outside this WFH request.");
        request.OfficeConversions ??= [];
        if (request.OfficeConversions.Any(x => x.AttendanceDate.Date == day))
            return Fail("WFH_ALREADY_CONVERTED_TO_OFFICE", "This request date has already been converted to office attendance.", StatusCodes.Status409Conflict);

        var employee = await EmployeeEmployedOn(request.UserId, day);
        if (employee is null) return Fail("WFH_EMPLOYEE_NOT_FOUND", "Employee was not employed on the selected attendance date.");
        try { await attendanceEditGuard.EnsureEditableWorkingDayAsync(CompanyId, employee.UserId, day); }
        catch (InvalidOperationException ex) { return Fail("WFH_ATTENDANCE_NOT_EDITABLE", ex.Message); }

        var policy = await ActivePolicy();
        if (policy is null) return Fail("WFH_POLICY_DISABLED", "WFH policy is disabled.");
        var schedule = await effectiveSchedules.ResolveAsync(CompanyId, employee, day);
        var timeZoneId = schedule?.TimeZoneId ?? policy.TimeZoneId;
        if (!TryResolveCompanyLocalTime(day, dto.OfficeCheckInTime, timeZoneId, out var officeCheckInUtc) ||
            !TryResolveCompanyLocalTime(day, dto.OfficeCheckOutTime, timeZoneId, out var officeCheckOutUtc) ||
            officeCheckOutUtc <= officeCheckInUtc ||
            day.Date > DateTime.UtcNow.Date ||
            (day.Date < DateTime.UtcNow.Date && officeCheckOutUtc > DateTime.UtcNow))
            return Fail("WFH_OFFICE_TIME_RANGE_INVALID", "Office check-in and check-out must be valid, ordered times on the selected company business date and cannot be in the future.");

        var breakMinutes = Math.Max(0, schedule?.BreakMinutes ?? 0);
        var officeHours = Math.Round(Math.Max(0, (decimal)(officeCheckOutUtc - officeCheckInUtc).TotalHours - breakMinutes / 60m), 2, MidpointRounding.AwayFromZero);
        var officeSchedule = schedule ?? new CompanyOfficeSchedule
        {
            CompanyId = CompanyId,
            StartTime = policy.OfficeStartTime ?? TimeSpan.FromHours(9),
            EndTime = policy.OfficeEndTime ?? TimeSpan.FromHours(18),
            RequiredWorkingMinutes = (int)(policy.FullDayMinimumHours * 60),
            BreakMinutes = breakMinutes
        };
        var officeStatusCode = statusCalculation.CalculateOfficeStatus(
            officeSchedule,
            TimeOnly.FromTimeSpan(dto.OfficeCheckInTime),
            TimeOnly.FromTimeSpan(dto.OfficeCheckOutTime),
            officeHours);
        var status = await statuses.FirstOrDefault(x => x.CompanyId == CompanyId && x.Code == officeStatusCode && x.IsActive);
        if (status is null) return Fail("WFH_OFFICE_STATUS_UNAVAILABLE", $"The active {officeStatusCode} attendance status required by the office timing calculation is not configured for this company.");

        var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : request.DurationType == "SecondHalf" ? "SECOND_HALF" : null;
        AttendanceModel? row = null;
        AttendanceDaySegment? segment = null;
        if (segmentName is null)
            row = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= day && x.Date < day.AddDays(1) && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId);
        else
            segment = await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= day && x.Date < day.AddDays(1) && x.Segment == segmentName && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId);
        if (row is null && segment is null)
            return Fail("WFH_ATTENDANCE_NOT_FOUND", "No attendance record owned by this WFH request exists for the selected date.");

        var conversion = new WorkFromHomeOfficeConversion
        {
            AttendanceDate = BusinessDateUtc(day), OfficeCheckInTime = officeCheckInUtc, OfficeCheckOutTime = officeCheckOutUtc,
            PreviousAttendanceStatus = row?.Status ?? segment!.Status,
            PreviousCheckInTime = row?.CheckInTime ?? segment?.CheckInTime,
            PreviousCheckOutTime = row?.CheckOutTime ?? segment?.CheckOutTime,
            PreviousTotalHours = row?.TotalHours ?? segment?.TotalHours,
            PreviousRemarks = row?.Remarks ?? segment?.Remarks,
            Remarks = Clean(dto.Remarks)!, ConvertedByUserId = UserId, ConvertedAtUtc = DateTime.UtcNow
        };

        var originalRequestVersion = request.Version;
        request.OfficeConversions.Add(conversion);
        request.Version++;
        request.UpdatedBy = UserId;
        request.UpdatedDate = DateTime.UtcNow;
        var auditEvent = BuildAuditOutboxEvent(request, "Approved", "Approved", "ConvertedToOffice", "WFH_CONVERTED_TO_OFFICE", conversion.Remarks, UserId);
        var notificationEvent = BuildEmployeeNotificationOutboxEvent(request, employee,
            "Your approved WFH attendance was converted to verified office attendance.", EnumsHelper.NotificationTypes.WorkFromHomeRequest, "ConvertedToOffice", UserId);
        var originalSegmentVersion = 0L;
        AttendanceModel? dailyRow = null;
        long originalDailyRowVersion = 0;
        if (row is not null)
        {
            row.Status = status.Code; row.CheckInTime = officeCheckInUtc; row.CheckOutTime = officeCheckOutUtc;
            row.TotalHours = officeHours; row.BreakMinutes = breakMinutes; row.RemarkCode = "WFH_CONVERTED_TO_OFFICE";
            row.Remarks = $"HR/Admin converted approved WFH to verified office attendance: {conversion.Remarks} ({requestId})";
            row.OperatorRemark = conversion.Remarks; row.ModifiedByUserId = UserId; row.ModifiedAtUtc = DateTime.UtcNow; row.Version++;
        }
        else
        {
            originalSegmentVersion = segment!.Version;
            segment!.Status = status.Code; segment.CheckInTime = officeCheckInUtc; segment.CheckOutTime = officeCheckOutUtc;
            segment.TotalHours = officeHours; segment.SourceVersion = request.Version; segment.Remarks = $"HR/Admin converted approved WFH to verified office attendance: {conversion.Remarks} ({requestId})";
            segment.UpdatedAtUtc = DateTime.UtcNow;
            segment.Version++;

            dailyRow = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= day && x.Date < day.AddDays(1));
            if (dailyRow is not null)
            {
                originalDailyRowVersion = dailyRow.Version;
                var oppSegName = segmentName == "FIRST_HALF" ? "SECOND_HALF" : "FIRST_HALF";
                var oppSeg = await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= day && x.Date < day.AddDays(1) && x.Segment == oppSegName);
                if (oppSeg is not null && oppSeg.SourceType == "WFH_REQUEST")
                {
                    dailyRow.Status = "WFH+WFO";
                }
                else if (oppSeg is not null && (oppSeg.Status is "P" or "PRESENT"))
                {
                    dailyRow.Status = "P";
                }
                else
                {
                    dailyRow.Status = "HD";
                }
                dailyRow.SourceType = "ADMIN_MANUAL";
                dailyRow.RemarkCode = "WFH_CONVERTED_TO_OFFICE";
                dailyRow.Remarks = $"HR/Admin converted half-day WFH to verified office attendance: {conversion.Remarks} ({requestId})";
                dailyRow.ModifiedByUserId = UserId;
                dailyRow.ModifiedAtUtc = DateTime.UtcNow;
                dailyRow.Version++;
            }
        }

        // Transactionally update the source-owned attendance and its request audit
        // record. Compare tenant, employee, source, request version, and attendance
        // version so concurrent clock/HR actions cannot be silently overwritten.
        try
        {
            using var session = await mongoClient.StartSessionAsync();
            await session.WithTransactionAsync(async (transaction, cancellationToken) =>
            {
                if (row is not null)
                {
                    var savedAttendance = await attendance.GetCollection().ReplaceOneAsync(transaction,
                        Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.AttendanceId == row.AttendanceId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Version == row.Version - 1),
                        row, cancellationToken: cancellationToken);
                    if (savedAttendance.MatchedCount != 1) throw new InvalidOperationException("WFH_CONVERSION_ATTENDANCE_CHANGED");
                }
                else
                {
                    var savedSegment = await attendanceSegments.GetCollection().ReplaceOneAsync(transaction,
                        Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.AttendanceDaySegmentId == segment!.AttendanceDaySegmentId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Version == originalSegmentVersion),
                        segment!, cancellationToken: cancellationToken);
                    if (savedSegment.MatchedCount != 1) throw new InvalidOperationException("WFH_CONVERSION_ATTENDANCE_CHANGED");
                    if (dailyRow is not null)
                    {
                        var savedDaily = await attendance.GetCollection().ReplaceOneAsync(transaction,
                            Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.AttendanceId == dailyRow.AttendanceId && x.Version == originalDailyRowVersion),
                            dailyRow, cancellationToken: cancellationToken);
                        if (savedDaily.MatchedCount != 1) throw new InvalidOperationException("WFH_CONVERSION_ATTENDANCE_CHANGED");
                    }
                }
                var savedRequest = await requests.GetCollection().ReplaceOneAsync(transaction,
                    Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.RequestId == requestId && x.Status == "Approved" && x.Version == originalRequestVersion),
                    request, cancellationToken: cancellationToken);
                if (savedRequest.MatchedCount != 1) throw new InvalidOperationException("WFH_CONVERSION_REQUEST_CHANGED");
                await InsertOutboxEventsAsync(transaction, [auditEvent, notificationEvent], cancellationToken);
                return true;
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_CONVERSION_ATTENDANCE_CHANGED")
        {
            return Fail("WFH_CONVERSION_ATTENDANCE_CHANGED", "Attendance changed while converting it. Refresh the request and try again.", StatusCodes.Status409Conflict);
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_CONVERSION_REQUEST_CHANGED")
        {
            return Fail("WFH_REQUEST_CHANGED", "The request changed while converting attendance. Refresh it and try again.", StatusCodes.Status409Conflict);
        }
        catch (Exception ex) when (ex is NotSupportedException || ex is MongoCommandException mongoException && mongoException.Code == 20)
        {
            return Fail("WFH_CONVERSION_TRANSACTION_REQUIRED", "Office conversion requires a MongoDB deployment that supports transactions. No conversion was applied.", StatusCodes.Status503ServiceUnavailable);
        }

        await InvalidateSummary(employee, day);
        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "WFH attendance converted to office attendance with an audit record." };
    }

    public async Task<Result> AdminGrantAsync(WorkFromHomeAdminGrantDto dto)
    {
        if (dto is null) return Fail("WFH_ADMIN_GRANT_INVALID", "Grant payload is required.");
        if (string.IsNullOrWhiteSpace(dto.TargetUserId)) return Fail("WFH_TARGET_USER_REQUIRED", "Target employee is required.");
        if (string.IsNullOrWhiteSpace(dto.Remarks) || dto.Remarks.Trim().Length < 3)
            return Fail("WFH_REMARKS_REQUIRED", "Provide at least three characters explaining the administrative WFH grant.");

        var hasAccess = await roles.VerifyUserAccess(AppModule.WorkFromHome,
            [Codeji.CMS.Utility.Constraints.Permission.PolicyEdit, Codeji.CMS.Utility.Constraints.Permission.ApproveTeam], UserId, CompanyId);
        if (!hasAccess)
        {
            hasAccess = await roles.VerifyUserAccess(AppModule.Attendance,
                [Codeji.CMS.Utility.Constraints.Permission.Edit, Codeji.CMS.Utility.Constraints.Permission.Override], UserId, CompanyId);
        }
        if (!hasAccess)
            return Fail("WFH_NOT_AUTHORIZED", "You do not have permission to grant administrative WFH.", StatusCodes.Status403Forbidden);

        var date = BusinessDateUtc(dto.AttendanceDate);
        var employee = (await employees.GetAll(x => x.CompanyId == CompanyId && x.UserId == dto.TargetUserId && !x.IsDeleted, withDefaultFilter: false)).FirstOrDefault();
        if (employee is null) return Fail("WFH_EMPLOYEE_NOT_FOUND", "Target employee not found.");
        if (!EmploymentPeriod.WasEmployedOn(employee, date)) return Fail("WFH_EMPLOYEE_NOT_EMPLOYED", "Employee was not employed on this date.");

        var policy = await ActivePolicy();
        if (policy is null) return Fail("WFH_POLICY_DISABLED", "WFH policy is not active for this company.");

        var workingDayGuard = await ValidateWorkingDay(employee, policy, date);
        if (workingDayGuard is not null)
            return Fail(workingDayGuard.Value.Code, workingDayGuard.Value.Message);

        var grantType = dto.GrantType?.Trim() ?? "FullDay";
        var durationType = grantType switch
        {
            "WfhWfo" => "FirstHalf",
            "WfoWfh" => "SecondHalf",
            "WfhHd" => "FirstHalf",
            "HdWfh" => "SecondHalf",
            _ => "FullDay"
        };

        // Create the pre-approved WorkFromHomeRequest
        var request = new WorkFromHomeRequest
        {
            CompanyId = CompanyId,
            UserId = employee.UserId,
            EmployeeId = employee.EmployeeId,
            FromDate = date,
            ToDate = date,
            DurationType = durationType,
            ReasonCode = "ADMIN_GRANT",
            ReasonText = dto.Remarks.Trim(),
            Status = "Approved",
            ApproverUserId = UserId,
            ReviewedByUserId = UserId,
            ReviewedAt = DateTime.UtcNow,
            ReviewRemarksCode = "ADMIN_APPROVED",
            ReviewRemarksText = dto.Remarks.Trim(),
            CreatedBy = UserId,
            CreatedDate = DateTime.UtcNow,
            Version = 1
        };

        var requestsCol = requests.GetCollection();
        await requestsCol.InsertOneAsync(request);

        // Remove prior half-day segments so this grant's attendance owns the date cleanly
        await attendanceSegments.DeleteAll(Builders<AttendanceDaySegment>.Filter.Where(x =>
            x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= date && x.Date < date.AddDays(1)));

        if (grantType is "WfhWfo" or "WfoWfh")
        {
            var compSegmentName = grantType == "WfhWfo" ? "SECOND_HALF" : "FIRST_HALF";
            var compSegment = new AttendanceDaySegment
            {
                CompanyId = CompanyId,
                UserId = employee.UserId,
                EmployeeId = employee.EmployeeId,
                Date = date,
                Segment = compSegmentName,
                Status = "P",
                SourceType = "ADMIN_MANUAL",
                Remarks = "Office segment companion to administrative WFH grant",
                UpdatedAtUtc = DateTime.UtcNow,
                Version = 1
            };
            if (dto.OfficeCheckInTime.HasValue) compSegment.CheckInTime = DateTime.SpecifyKind(date.Add(dto.OfficeCheckInTime.Value), DateTimeKind.Utc);
            if (dto.OfficeCheckOutTime.HasValue) compSegment.CheckOutTime = DateTime.SpecifyKind(date.Add(dto.OfficeCheckOutTime.Value), DateTimeKind.Utc);
            await attendanceSegments.AddOne(compSegment);
        }
        else if (grantType is "WfhHd" or "HdWfh")
        {
            var compSegmentName = grantType == "WfhHd" ? "SECOND_HALF" : "FIRST_HALF";
            var compSegment = new AttendanceDaySegment
            {
                CompanyId = CompanyId,
                UserId = employee.UserId,
                EmployeeId = employee.EmployeeId,
                Date = date,
                Segment = compSegmentName,
                Status = "HD",
                SourceType = "ADMIN_MANUAL",
                Remarks = "Half-day companion to administrative WFH grant",
                UpdatedAtUtc = DateTime.UtcNow,
                Version = 1
            };
            await attendanceSegments.AddOne(compSegment);
        }

        var changedDates = new List<DateTime>();
        var reconcileResult = await ReconcileApproved(request, employee, policy, 1, null, changedDates);
        if (!reconcileResult.Success) return reconcileResult;

        await InvalidateSummary(employee, date);
        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Work from home granted successfully with an audited record." };
    }

    private async Task<Result> RecordTime(string requestId, bool checkIn, WorkFromHomeCheckOutDto? checkOutDto = null)
    {
        var request = await requests.FirstOrDefault(x => x.CompanyId == CompanyId && x.RequestId == requestId && x.UserId == UserId && x.Status == "Approved");
        if (request is null) return Fail("WFH_CHECKIN_NOT_ALLOWED", "An approved WFH request owned by you is required.");
        var now = DateTime.UtcNow;
        var policy = await ActivePolicy(); if (policy is null) return Fail("WFH_POLICY_DISABLED", "WFH policy is disabled.");
        var employee = await ActiveEmployee(UserId);
        if (employee is null) return Fail("WFH_EMPLOYEE_NOT_ELIGIBLE", "The employee is no longer active in this company.");
        var schedule = await effectiveSchedules.ResolveAsync(CompanyId, employee, DateTime.UtcNow);
        var companyNow = CompanyNow(schedule?.TimeZoneId ?? policy.TimeZoneId);
        var day = companyNow.Date;
        if (day < request.FromDate.Date || day > request.ToDate.Date) return Fail("WFH_DATE_NOT_COVERED", "Today is not covered by this request.");
        if ((request.OfficeConversions ?? []).Any(x => x.AttendanceDate.Date == day)) return Fail("WFH_ALREADY_CONVERTED_TO_OFFICE", "Today's attendance is already recorded as office work.");
        var checkInFrom = schedule?.CheckInAllowedFrom ?? policy.AllowedCheckInFrom;
        var checkOutUntil = schedule?.CheckOutAllowedUntil ?? policy.AllowedCheckOutUntil;
        // A WFH request has already been approved for this employee and business
        // date. The ordinary office check-in cutoff (typically 10:00) classifies a
        // late arrival; it must not prevent the employee from recording a real
        // late WFH start. Keep the early boundary, and close WFH check-in only when
        // the working day itself has ended.
        var wfhCheckInUntil = checkOutUntil ?? schedule?.EndTime ?? policy.OfficeEndTime;
        if (checkIn && ((checkInFrom.HasValue && companyNow.TimeOfDay < checkInFrom) || (wfhCheckInUntil.HasValue && companyNow.TimeOfDay > wfhCheckInUntil))) return Fail("WFH_CHECKIN_NOT_OPEN", "Check-in is outside the effective WFH attendance window.");
        if (checkIn && !IsHalfDayClockInWithinRequestedSegment(request.DurationType, companyNow.TimeOfDay,
                schedule?.StartTime ?? policy.OfficeStartTime, schedule?.RequiredWorkingMinutes, policy.FullDayMinimumHours))
        {
            var halfStart = HalfDayBoundary(schedule?.StartTime ?? policy.OfficeStartTime, schedule?.RequiredWorkingMinutes, policy.FullDayMinimumHours);
            var boundary = halfStart?.ToString(@"hh\:mm") ?? "the half-day boundary";
            return Fail("WFH_CHECKIN_WRONG_HALF", request.DurationType == "SecondHalf"
                ? $"Second-half WFH check-in is available from {boundary}."
                : $"First-half WFH check-in must be recorded before {boundary}.");
        }
        var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : request.DurationType == "SecondHalf" ? "SECOND_HALF" : null;
        var row = segmentName is null ? await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == UserId && x.Date >= day && x.Date < day.AddDays(1)) : null;
        var segment = segmentName is null ? null : await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == UserId && x.Date >= day && x.Date < day.AddDays(1) && x.Segment == segmentName);
        var dailyRow = row ?? await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == UserId && x.Date >= day && x.Date < day.AddDays(1));

        if ((row is null || row.SourceType != "WFH_REQUEST" || row.SourceId != requestId) && (segment is null || segment.SourceType != "WFH_REQUEST" || segment.SourceId != requestId))
        {
            if ((request.OfficeConversions ?? []).Any(x => x.AttendanceDate.Date == day) || dailyRow?.SourceType is "ADMIN_OVERRIDE" or "ADMIN_MANUAL")
                return Fail("WFH_ALREADY_CONVERTED_TO_OFFICE", "Today's attendance was already converted to in-office work by HR.");

            var reconcileResult = await ReconcileApproved(request, employee, policy, request.Version, null, new List<DateTime>());
            if (!reconcileResult.Success)
                return Fail("WFH_RECONCILE_FAILED", reconcileResult.Message ?? "Failed to initialize WFH attendance record for timing.");

            row = segmentName is null ? await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == UserId && x.Date >= day && x.Date < day.AddDays(1)) : null;
            segment = segmentName is null ? null : await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == UserId && x.Date >= day && x.Date < day.AddDays(1) && x.Segment == segmentName);

            if ((row is null || row.SourceType != "WFH_REQUEST" || row.SourceId != requestId) && (segment is null || segment.SourceType != "WFH_REQUEST" || segment.SourceId != requestId))
                return Fail("WFH_ATTENDANCE_CONFLICT", "No WFH-owned attendance record is available for timing.");
        }
        var originalRowVersion = row?.Version;
        var originalDailyRowVersion = dailyRow?.Version;
        var originalSegmentVersion = segment?.Version;
        AttendancePayrollException? insufficientHoursException = null;

        if (row is not null && (row.Remarks?.StartsWith("Generated from approved WFH request") == true))
        {
            row.CheckInTime = null;
            row.CheckOutTime = null;
            row.TotalHours = null;
        }
        if (segment is not null && (segment.Remarks?.StartsWith("Generated from approved half-day WFH request") == true))
        {
            segment.CheckInTime = null;
            segment.CheckOutTime = null;
            segment.TotalHours = null;
        }

        var checkInTime = row?.CheckInTime ?? segment?.CheckInTime;
        var checkOutTime = row?.CheckOutTime ?? segment?.CheckOutTime;
        var auditEvent = BuildAuditOutboxEvent(request, request.Status, request.Status,
            checkIn ? "CheckIn" : "CheckOut", row?.RemarkCode ?? segment?.Status,
            checkOutDto?.Remarks, UserId);
        if (checkIn)
        {
            if (checkInTime.HasValue) return Fail("WFH_DUPLICATE_CHECK_IN", "Check-in was already recorded.");
            if (row is not null) { row.CheckInTime = now; row.Remarks = $"Checked in through employee WFH portal ({requestId})"; }
            else
            {
                segment!.CheckInTime = now;
                segment.Remarks = $"Checked in through employee WFH portal ({requestId})";
                segment.UpdatedAtUtc = now;
                if (dailyRow is not null)
                {
                    dailyRow.CheckInTime = now;
                    dailyRow.Remarks = segment.Remarks;
                    dailyRow.Version++;
                }
            }
        }
        else
        {
            if (!checkInTime.HasValue) return Fail("WFH_CHECKOUT_WITHOUT_CHECKIN", "Check-in is required before check-out.");
            if (checkOutTime.HasValue) return Fail("WFH_DUPLICATE_CHECK_OUT", "Check-out was already recorded.");

            var requiredFullDayMinutes = schedule?.RequiredWorkingMinutes ?? (int)(policy.FullDayMinimumHours * 60);
            if (requiredFullDayMinutes <= 0) requiredFullDayMinutes = 480;
            var requiredHalfDayMinutes = Math.Max(1, (int)Math.Ceiling(policy.HalfDayMinimumHours * 60m));
            if (requiredHalfDayMinutes <= 0) requiredHalfDayMinutes = 240;

            if (now < checkInTime.Value) now = checkInTime.Value;
            var elapsedMinutes = Math.Max(0, (int)(now - checkInTime.Value).TotalMinutes);
            // Deduct office break minutes only when the full shift was worked;
            // half-day or early exit WFH is not penalized with an un-taken lunch break.
            var breakMinutes = elapsedMinutes >= requiredFullDayMinutes ? Math.Max(0, schedule?.BreakMinutes ?? 0) : 0;
            var workedMinutes = Math.Max(0, elapsedMinutes - breakMinutes);
            var verifiedHours = Math.Round(workedMinutes / 60m, 2, MidpointRounding.AwayFromZero);

            var chosenType = checkOutDto?.AttendanceType?.Trim();
            var userRemarks = !string.IsNullOrWhiteSpace(checkOutDto?.Remarks) ? $" - Note: {checkOutDto.Remarks.Trim()}" : "";

            if (row is not null)
            {
                row.CheckOutTime = now;
                row.TotalHours = verifiedHours;
                row.BreakMinutes = breakMinutes;

                if (workedMinutes >= requiredFullDayMinutes)
                {
                    row.Status = policy.FullDayAttendanceStatusCode?.Trim().ToUpperInvariant() ?? "WFH";
                    row.RemarkCode = "WFH_FULL_DAY";
                    row.Remarks = $"Checked out through employee WFH portal as Full-Day ({verifiedHours:0.##} hr(s)){userRemarks} ({requestId})";
                }
                else if (workedMinutes >= requiredHalfDayMinutes)
                {
                    var oppositeSegment = segment is null ? null : await attendanceSegments.FirstOrDefault(x =>
                        x.CompanyId == CompanyId && x.UserId == UserId && x.Date >= day && x.Date < day.AddDays(1) && x.Segment != segment.Segment);

                    if (oppositeSegment is not null && oppositeSegment.SourceType == "WFH_REQUEST")
                    {
                        row.Status = policy.FullDayAttendanceStatusCode?.Trim().ToUpperInvariant() ?? "WFH";
                        row.RemarkCode = "WFH_FULL_DAY";
                        row.Remarks = $"Checked out through employee WFH portal (both halves WFH: {verifiedHours:0.##} hr(s)){userRemarks} ({requestId})";
                    }
                    else if (oppositeSegment is not null && oppositeSegment.SourceType == "LEAVE")
                    {
                        var wfhHdCode = !string.IsNullOrWhiteSpace(policy.WfhHdAttendanceStatusCode) ? policy.WfhHdAttendanceStatusCode : "WFH-HD";
                        var validWfhHd = await ValidCode(wfhHdCode);
                        row.Status = validWfhHd ? wfhHdCode.Trim().ToUpperInvariant() : "WFH-HD";
                        row.RemarkCode = "WFH_HD_COMBINATION";
                        row.Remarks = $"Checked out through employee WFH portal (WFH+HD combination: {verifiedHours:0.##} hr(s)){userRemarks} ({requestId})";
                    }
                    else if ((oppositeSegment is not null && (oppositeSegment.SourceType is "PORTAL_CLOCK" or "ADMIN_MANUAL" || oppositeSegment.Status is "P" or "PRESENT"))
                             || (row.CheckInTime.HasValue && row.SourceType != "WFH_REQUEST" && row.SourceType != "LEAVE"))
                    {
                        var wfhWfoCode = !string.IsNullOrWhiteSpace(policy.WfhWfoAttendanceStatusCode) ? policy.WfhWfoAttendanceStatusCode : "WFH+WFO";
                        var validWfhWfo = await ValidCode(wfhWfoCode);
                        row.Status = validWfhWfo ? wfhWfoCode.Trim().ToUpperInvariant() : "WFH+WFO";
                        row.RemarkCode = "WFH_WFO_COMBINATION";
                        row.Remarks = $"Checked out through employee WFH portal (WFH+WFO combination: {verifiedHours:0.##} hr(s)){userRemarks} ({requestId})";
                    }
                    else
                    {
                        var halfDayCode = !string.IsNullOrWhiteSpace(policy.HalfDayAttendanceStatusCode)
                            ? policy.HalfDayAttendanceStatusCode
                            : (!string.IsNullOrWhiteSpace(policy.WfhHdAttendanceStatusCode) ? policy.WfhHdAttendanceStatusCode : "WFH-HD");
                        var validHalfDay = await ValidCode(halfDayCode);
                        row.Status = validHalfDay ? halfDayCode.Trim().ToUpperInvariant() : "WFH-HD";
                        row.RemarkCode = "WFH_HALF_DAY";
                        row.Remarks = $"Checked out through employee WFH portal (half-day: {verifiedHours:0.##} hr(s)){userRemarks} ({requestId})";
                    }
                }
                else
                {
                    // Under-hours / Short shift (< 4 hrs, e.g. 54 min).
                    // Employee cannot claim Half-Day or Full-Day when working 54 min.
                    // Recorded with exact punches for audit, status set to A (Absent/Incomplete),
                    // flagged as WFH_EARLY_EXIT, and routed for HR/Admin regularization.
                    row.EarlyExitCount = 1;
                    row.Status = "A";
                    row.RemarkCode = "WFH_EARLY_EXIT";
                    row.Remarks = $"Checked out through employee WFH portal (early exit: {verifiedHours:0.##} hr(s) worked; below {policy.HalfDayMinimumHours:0.##}h minimum threshold){userRemarks} - Pending HR/Admin regularisation ({requestId})";

                    var existingExc = await exceptions.FirstOrDefault(x =>
                        x.CompanyId == CompanyId && x.UserId == UserId &&
                        x.AttendanceDate == day && x.LeaveRequestId == requestId &&
                        x.ExceptionType == "WFH_INSUFFICIENT_HOURS");
                    if (existingExc is null)
                    {
                        insufficientHoursException = new AttendancePayrollException
                        {
                            CompanyId = CompanyId,
                            UserId = UserId,
                            EmployeeId = employee.EmployeeId,
                            PayrollMonth = new DateTime(day.Year, day.Month, 1),
                            AttendanceDate = day,
                            AffectedDates = [day],
                            ExceptionType = "WFH_INSUFFICIENT_HOURS",
                            Severity = "BLOCKING",
                            Status = "PENDING_REVIEW",
                            LeaveRequestId = requestId,
                            ExistingAttendanceId = row.AttendanceId,
                            ExistingStatus = row.Status,
                            SourceVersion = row.SourceVersion,
                            Reason = $"WFH check-out was completed with insufficient hours ({verifiedHours:0.##}h worked; below {policy.HalfDayMinimumHours:0.##}h half-day requirement). Pending HR/Admin regularization."
                        };
                    }
                }
            }
            else
            {
                segment!.CheckOutTime = now;
                segment.TotalHours = verifiedHours;
                segment.UpdatedAtUtc = now;
                if (workedMinutes >= requiredHalfDayMinutes)
                {
                    segment.Remarks = $"Checked out through employee WFH portal ({verifiedHours:0.##} hr(s)){userRemarks} ({requestId})";
                }
                else
                {
                    segment.Status = "A";
                    segment.Remarks = $"Clocked out early ({verifiedHours:0.##} hr(s) worked; below required half-day){userRemarks} - Pending HR/Admin regularisation ({requestId})";

                    var existingExc = await exceptions.FirstOrDefault(x =>
                        x.CompanyId == CompanyId && x.UserId == UserId &&
                        x.AttendanceDate == day && x.LeaveRequestId == requestId &&
                        x.ExceptionType == "WFH_INSUFFICIENT_HOURS");
                    if (existingExc is null)
                    {
                        insufficientHoursException = new AttendancePayrollException
                        {
                            CompanyId = CompanyId,
                            UserId = UserId,
                            EmployeeId = employee.EmployeeId,
                            PayrollMonth = new DateTime(day.Year, day.Month, 1),
                            AttendanceDate = day,
                            AffectedDates = [day],
                            ExceptionType = "WFH_INSUFFICIENT_HOURS",
                            Severity = "BLOCKING",
                            Status = "PENDING_REVIEW",
                            LeaveRequestId = requestId,
                            ExistingAttendanceId = segment.AttendanceDaySegmentId,
                            ExistingStatus = segment.Status,
                            SourceVersion = segment.SourceVersion,
                            Reason = $"Half-day WFH check-out was completed with insufficient hours ({verifiedHours:0.##}h worked; below required half-day). Pending HR/Admin regularization."
                        };
                    }
                }
                if (dailyRow is not null && !checkIn)
                {
                    dailyRow.CheckInTime = segment.CheckInTime;
                    dailyRow.CheckOutTime = segment.CheckOutTime;
                    dailyRow.TotalHours = segment.TotalHours;
                    if (workedMinutes < requiredHalfDayMinutes)
                    {
                        dailyRow.Status = "A";
                        dailyRow.EarlyExitCount = 1;
                        dailyRow.RemarkCode = "WFH_EARLY_EXIT";
                        dailyRow.Remarks = segment.Remarks;
                    }
                    dailyRow.Version++;
                }
            }

        }
        if (row is not null) row.Version++;
        else segment!.Version++;
        try
        {
            using var session = await mongoClient.StartSessionAsync();
            await session.WithTransactionAsync(async (transaction, cancellationToken) =>
            {
                if (row is not null)
                {
                    var write = await attendance.GetCollection().ReplaceOneAsync(transaction,
                        Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == UserId && x.AttendanceId == row.AttendanceId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Version == originalRowVersion), row, cancellationToken: cancellationToken);
                    if (write.MatchedCount != 1) throw new InvalidOperationException("WFH_ATTENDANCE_CHANGED");
                }
                else
                {
                    var write = await attendanceSegments.GetCollection().ReplaceOneAsync(transaction,
                        Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == UserId && x.AttendanceDaySegmentId == segment!.AttendanceDaySegmentId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Version == originalSegmentVersion), segment!, cancellationToken: cancellationToken);
                    if (write.MatchedCount != 1) throw new InvalidOperationException("WFH_ATTENDANCE_CHANGED");
                    if (dailyRow is not null)
                    {
                        await attendance.GetCollection().ReplaceOneAsync(transaction,
                            Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == UserId && x.AttendanceId == dailyRow.AttendanceId && x.Version == originalDailyRowVersion),
                            dailyRow, cancellationToken: cancellationToken);
                    }
                }
                if (insufficientHoursException is not null)
                    await exceptions.GetCollection().InsertOneAsync(transaction, insufficientHoursException, cancellationToken: cancellationToken);
                await InsertOutboxEventsAsync(transaction, [auditEvent], cancellationToken);
                if (!checkIn)
                    await summaries.GetCollection().DeleteManyAsync(transaction,
                        Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == CompanyId &&
                            x.EmployeeId == employee.EmployeeId && x.PayrollMonth == new DateTime(day.Year, day.Month, 1) && !x.IsLocked),
                        options: null, cancellationToken: cancellationToken);
                return true;
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "WFH_ATTENDANCE_CHANGED")
        { return Fail("WFH_ATTENDANCE_CHANGED", "Attendance changed while recording WFH time. Refresh and try again.", StatusCodes.Status409Conflict); }
        catch (Exception ex) when (ex is MongoCommandException mongoException && (mongoException.Code == 20 || mongoException.Code == 263))
        {
            if (row is not null)
            {
                var write = await attendance.GetCollection().ReplaceOneAsync(
                    Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == UserId && x.AttendanceId == row.AttendanceId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Version == originalRowVersion), row);
                if (write.MatchedCount != 1) return Fail("WFH_ATTENDANCE_CHANGED", "Attendance changed while recording WFH time. Refresh and try again.", StatusCodes.Status409Conflict);
            }
            else
            {
                var write = await attendanceSegments.GetCollection().ReplaceOneAsync(
                    Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == UserId && x.AttendanceDaySegmentId == segment!.AttendanceDaySegmentId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Version == originalSegmentVersion), segment!);
                if (write.MatchedCount != 1) return Fail("WFH_ATTENDANCE_CHANGED", "Attendance changed while recording WFH time. Refresh and try again.", StatusCodes.Status409Conflict);
                if (dailyRow is not null)
                {
                    await attendance.GetCollection().ReplaceOneAsync(
                        Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == UserId && x.AttendanceId == dailyRow.AttendanceId && x.Version == originalDailyRowVersion),
                        dailyRow);
                }
            }
            if (insufficientHoursException is not null)
                await exceptions.GetCollection().InsertOneAsync(insufficientHoursException);
            await InsertOutboxEventsAsync(null, [auditEvent], CancellationToken.None);
            if (!checkIn)
                await summaries.GetCollection().DeleteManyAsync(
                    Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == CompanyId &&
                        x.EmployeeId == employee.EmployeeId && x.PayrollMonth == new DateTime(day.Year, day.Month, 1) && !x.IsLocked));
            return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = checkIn ? "WFH check-in recorded." : "WFH check-out recorded." };
        }
        catch (Exception ex) when (ex is NotSupportedException || ex is MongoCommandException mongoException && mongoException.Code == 20)
        { return Fail("WFH_TRANSACTION_REQUIRED", "WFH clock actions require MongoDB transaction support. No attendance changes were applied.", StatusCodes.Status503ServiceUnavailable); }
        catch
        { return Fail("WFH_CLOCK_WRITE_FAILED", "WFH time could not be recorded. No attendance changes were applied.", StatusCodes.Status503ServiceUnavailable); }
        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = checkIn ? "WFH check-in recorded." : "WFH check-out recorded." };
    }

    public async Task<Result<IEnumerable<AttendanceRemarkOptionDto>>> GetRemarkOptions()
    {
        var items = await remarkOptions.GetAll(x => x.CompanyId == CompanyId && x.IsActive);
        return new Result<IEnumerable<AttendanceRemarkOptionDto>> { Success = true, MethodResult = items.OrderBy(x => x.DisplayOrder).Select(x => new AttendanceRemarkOptionDto { RemarkOptionId = x.RemarkOptionId, Code = x.Code, DisplayName = x.DisplayName, Category = x.Category, ApplicableStatusCodes = x.ApplicableStatusCodes, RequiresAdditionalText = x.RequiresAdditionalText, IsActive = x.IsActive, DisplayOrder = x.DisplayOrder }).ToList() };
    }

    public async Task<Result> SaveRemarkOption(AttendanceRemarkOptionDto dto)
    {
        if (dto is null || dto.ApplicableStatusCodes is null)
            return Fail("ATTENDANCE_REMARK_INVALID", "A remark option and its applicable status list are required.", StatusCodes.Status400BadRequest);
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.DisplayName)) return Fail("ATTENDANCE_REMARK_INVALID", "Code and display name are required.");
        if (dto.ApplicableStatusCodes.Any(x => string.IsNullOrWhiteSpace(x) || !statuses.Exist(s => s.CompanyId == CompanyId && s.Code == x && s.IsActive).GetAwaiter().GetResult())) return Fail("ATTENDANCE_REMARK_STATUS_INVALID", "One or more applicable statuses are inactive.");
        var option = string.IsNullOrWhiteSpace(dto.RemarkOptionId) ? null : await remarkOptions.FirstOrDefault(x => x.CompanyId == CompanyId && x.RemarkOptionId == dto.RemarkOptionId);
        option ??= new AttendanceRemarkOption { CompanyId = CompanyId }; option.Code = dto.Code.Trim().ToUpperInvariant(); option.DisplayName = Clean(dto.DisplayName)!; option.Category = Clean(dto.Category)!; option.ApplicableStatusCodes = dto.ApplicableStatusCodes.Select(x => x.Trim().ToUpperInvariant()).Distinct().ToList(); option.RequiresAdditionalText = dto.RequiresAdditionalText; option.IsActive = dto.IsActive; option.DisplayOrder = dto.DisplayOrder;
        return string.IsNullOrEmpty(option.RemarkOptionId) ? await remarkOptions.AddOne(option) : await remarkOptions.Update(Builders<AttendanceRemarkOption>.Filter.Where(x => x.CompanyId == CompanyId && x.RemarkOptionId == option.RemarkOptionId), option);
    }

    private async Task<Result> ReconcileApproved(WorkFromHomeRequest request, EmpUser employee, WorkFromHomePolicy policy, int sourceVersion,
        IClientSessionHandle? transactionSession, List<DateTime> changedDates)
    {
        if (!policy.IsEnabled || !IsEligible(policy, employee))
            return Fail("WFH_POLICY_NOT_AVAILABLE", "The WFH policy is disabled or no longer assigned to this employee.");
        // Historical combined WFH+SL/WFH+CL requests have no linked leave request
        // or balance movement. Never recreate them as ordinary WFH attendance.
        if (request.DurationType is "WfhSl" or "WfhCl")
            return Fail("WFH_LEGACY_COMBINATION_REQUIRES_REVIEW", "This legacy combined WFH/leave request has no linked leave balance. Create separate opposite-half WFH and leave requests instead.");

        var code = request.DurationType switch
        {
            "Mixed" => policy.WfhWfoAttendanceStatusCode ?? policy.MixedAttendanceStatusCode,
            "WfhHd" => policy.WfhHdAttendanceStatusCode ?? policy.HalfDayAttendanceStatusCode,
            "FullDay" => policy.FullDayAttendanceStatusCode,
            _ => policy.HalfDayAttendanceStatusCode
        };
        if (string.IsNullOrWhiteSpace(code)) return Fail("WFH_INVALID_ATTENDANCE_STATUS", "The selected WFH attendance combination is not configured.");
        if (!await ValidCode(code)) return Fail("WFH_INVALID_ATTENDANCE_STATUS", "Configured WFH status is inactive.");
        var requestDates = Days(request.FromDate, request.ToDate).Select(BusinessDateUtc).ToList();
        var otherActiveRequests = (await requests.GetAll(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.RequestId != request.RequestId &&
            (x.Status == "Pending" || x.Status == "Approved" || x.Status == "Returned") &&
            x.ToDate >= request.FromDate.AddDays(-34) && x.FromDate <= request.ToDate.AddDays(34))).ToList();
        if (request.ReasonCode != "ADMIN_GRANT")
        {
            var maxWorkingDays = await GetMaxWorkingDaysPerWeekAsync(policy);
            foreach (var week in requestDates.Select(StartOfWeek).Distinct())
            {
                var existingUsage = WeeklyUsage(otherActiveRequests.Where(x => x.FromDate < week.AddDays(7) && x.ToDate >= week), week);
                var requestedUsage = WeeklyUsage(request.FromDate, request.ToDate, request.DurationType, week) -
                    (request.OfficeConversions ?? []).Count(x => StartOfWeek(x.AttendanceDate) == week) * (request.DurationType is "FirstHalf" or "SecondHalf" ? .5m : 1m);
                var weeklyLimit = WeeklyLimitFor(policy, employee, maxWorkingDays);
                if (existingUsage + requestedUsage > weeklyLimit)
                    return Fail("WFH_WEEKLY_QUOTA_EXCEEDED", $"Only {weeklyLimit} WFH day(s) are allowed per week.");
            }
            if (policy.MaxDaysPerMonth is int monthlyLimit)
            {
                foreach (var month in requestDates.Select(date => new DateTime(date.Year, date.Month, 1)).Distinct())
                {
                    var existingUsage = MonthlyUsage(otherActiveRequests.Where(x => x.FromDate < month.AddMonths(1) && x.ToDate >= month), month);
                    var requestedUsage = MonthlyUsage(request.FromDate, request.ToDate, request.DurationType, month) -
                        (request.OfficeConversions ?? []).Count(x => x.AttendanceDate.Year == month.Year && x.AttendanceDate.Month == month.Month) * (request.DurationType is "FirstHalf" or "SecondHalf" ? .5m : 1m);
                    if (existingUsage + requestedUsage > monthlyLimit)
                        return Fail("WFH_MONTHLY_QUOTA_EXCEEDED", $"Only {monthlyLimit} WFH day(s) are allowed in a calendar month.");
                }
            }
            if (policy.MaxConsecutiveDays is int consecutiveLimit &&
                await MaximumConsecutiveWfhDays(request.UserId, policy, otherActiveRequests, request.FromDate, request.ToDate) > consecutiveLimit)
                return Fail("WFH_CONSECUTIVE_LIMIT_EXCEEDED", $"The request would exceed the limit of {consecutiveLimit} consecutive WFH working day(s).");
        }
        // Validate every date and ownership conflict before changing any attendance
        // row. An approval for several days must never leave the first day written
        // when a later date is locked, on leave, or otherwise unavailable.
        foreach (var date in requestDates)
        {
            if ((request.OfficeConversions ?? []).Any(x => x.AttendanceDate.Date == date.Date)) continue;
            if (!IsEffectiveFor(policy, date)) return Fail("WFH_POLICY_NOT_EFFECTIVE", $"WFH policy is not effective on {date:dd-MMM-yyyy}.");
            if (!IsLifecycleAvailableForWfh(employee, date)) return Fail("WFH_LIFECYCLE_NOT_WORKING", $"WFH is unavailable on {date:dd-MMM-yyyy} because the employee is not in a working lifecycle state.");
            var guard = await ValidateWorkingDay(employee, policy, date); if (guard is not null) return Fail(guard.Value.Code, guard.Value.Message);
            if (request.DurationType is "FirstHalf" or "SecondHalf")
            {
                var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : "SECOND_HALF";
                var existingSegment = await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1) && x.Segment == segmentName);
                if (existingSegment is not null && !(existingSegment.SourceType == "WFH_REQUEST" && existingSegment.SourceId == request.RequestId))
                {
                    if (request.ReasonCode != "ADMIN_GRANT") return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
                }
            }
            else
            {
                var existingSegments = await attendanceSegments.GetAll(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1));
                if (existingSegments.Any() && request.ReasonCode != "ADMIN_GRANT") return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
                var row = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1));
                if (row is not null && !(row.SourceType == "WFH_REQUEST" && row.SourceId == request.RequestId) && !AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(row.SourceType))
                {
                    if (request.ReasonCode != "ADMIN_GRANT") return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
                }
            }
        }
        for (var date = BusinessDateUtc(request.FromDate); date <= BusinessDateUtc(request.ToDate); date = date.AddDays(1))
        {
            if ((request.OfficeConversions ?? []).Any(x => x.AttendanceDate.Date == date.Date)) continue;
            if (!IsEffectiveFor(policy, date)) return Fail("WFH_POLICY_NOT_EFFECTIVE", $"WFH policy is not effective on {date:dd-MMM-yyyy}.");
            if (!IsLifecycleAvailableForWfh(employee, date)) return Fail("WFH_LIFECYCLE_NOT_WORKING", $"WFH is unavailable on {date:dd-MMM-yyyy} because the employee is not in a working lifecycle state.");
            var guard = await ValidateWorkingDay(employee, policy, date); if (guard is not null) return Fail(guard.Value.Code, guard.Value.Message);
            if (request.DurationType is "FirstHalf" or "SecondHalf")
            {
                var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : "SECOND_HALF";
                var existingSegment = await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1) && x.Segment == segmentName);
                if (existingSegment is not null && !(existingSegment.SourceType == "WFH_REQUEST" && existingSegment.SourceId == request.RequestId) && request.ReasonCode != "ADMIN_GRANT")
                {
                    return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
                }
                var segment = existingSegment ?? new AttendanceDaySegment { CompanyId = CompanyId, UserId = request.UserId, EmployeeId = employee.EmployeeId, Date = date, Segment = segmentName };
                var hasSegmentPortalClock = segment.Remarks != null && (segment.Remarks.Contains("Checked in through employee WFH portal") || segment.Remarks.Contains("Checked out through employee WFH portal"));
                var previousSegmentVersion = segment.Version;
                segment.Status = "WFH"; segment.SourceType = "WFH_REQUEST"; segment.SourceId = request.RequestId; segment.SourceVersion = sourceVersion; segment.UpdatedAtUtc = DateTime.UtcNow; segment.Version++;
                if (!hasSegmentPortalClock)
                {
                    segment.CheckInTime = null;
                    segment.CheckOutTime = null;
                    segment.TotalHours = null;
                    segment.Remarks = $"Generated from approved half-day WFH request {request.RequestId}";
                }
                var segmentCollection = attendanceSegments.GetCollection();
                if (string.IsNullOrWhiteSpace(segment.AttendanceDaySegmentId))
                {
                    if (transactionSession is not null)
                        await segmentCollection.InsertOneAsync(transactionSession, segment);
                    else
                        await segmentCollection.InsertOneAsync(segment);
                }
                else
                {
                    var segFilter = request.ReasonCode == "ADMIN_GRANT"
                        ? Builders<AttendanceDaySegment>.Filter.Where(x =>
                            x.CompanyId == CompanyId && x.UserId == request.UserId && x.AttendanceDaySegmentId == segment.AttendanceDaySegmentId)
                        : Builders<AttendanceDaySegment>.Filter.Where(x =>
                            x.CompanyId == CompanyId && x.UserId == request.UserId && x.AttendanceDaySegmentId == segment.AttendanceDaySegmentId &&
                            x.Version == previousSegmentVersion);
                    var segmentWrite = transactionSession is not null
                        ? await segmentCollection.ReplaceOneAsync(transactionSession, segFilter, segment)
                        : await segmentCollection.ReplaceOneAsync(segFilter, segment);
                    if (segmentWrite.MatchedCount != 1) throw new InvalidOperationException("WFH_ATTENDANCE_WRITE_CONFLICT");
                }
                await SyncDailyAttendanceForHalfDaySegmentAsync(request, employee, policy, date, segmentName, code, sourceVersion, transactionSession);
                changedDates.Add(date);
                continue;
            }
            var existingSegments = await attendanceSegments.GetAll(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1));
            if (existingSegments.Any() && request.ReasonCode != "ADMIN_GRANT")
            {
                return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
            }
            var row = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1));
            if (row is not null && !(row.SourceType == "WFH_REQUEST" && row.SourceId == request.RequestId) && !AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(row.SourceType) && request.ReasonCode != "ADMIN_GRANT")
            {
                return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
            }
            var isNewAttendanceRow = row is null || string.IsNullOrWhiteSpace(row.AttendanceId);
            var isPortalClockedRow = row is not null && row.SourceType == "WFH_REQUEST" && row.SourceId == request.RequestId && row.Remarks != null && (row.Remarks.Contains("Checked in through employee WFH portal") || row.Remarks.Contains("Checked out through employee WFH portal"));
            row ??= new AttendanceModel { CompanyId = CompanyId, UserId = request.UserId, EmployeeId = employee.EmployeeId, Date = date };
            var previousRowVersion = row.Version;
            var previousRowSourceType = row.SourceType;
            var previousRowSourceId = row.SourceId;
            row.Status = code.Trim().ToUpperInvariant(); row.SourceType = "WFH_REQUEST"; row.SourceId = request.RequestId; row.SourceVersion = sourceVersion; row.RemarkCode = "WFH_APPROVED";
            if (isNewAttendanceRow) { row.MarkedByUserId = UserId; row.MarkedAtUtc = DateTime.UtcNow; }
            row.Version++;
            if (!isPortalClockedRow)
            {
                row.CheckInTime = null;
                row.CheckOutTime = null;
                row.TotalHours = null;
                row.Remarks = $"Generated from approved WFH request {request.RequestId}";
            }
            var attendanceCollection = attendance.GetCollection();
            if (string.IsNullOrWhiteSpace(row.AttendanceId))
            {
                if (transactionSession is not null)
                    await attendanceCollection.InsertOneAsync(transactionSession, row);
                else
                    await attendanceCollection.InsertOneAsync(row);
            }
            else
            {
                var attFilter = request.ReasonCode == "ADMIN_GRANT"
                    ? Builders<AttendanceModel>.Filter.Where(x =>
                        x.CompanyId == CompanyId && x.UserId == request.UserId && x.AttendanceId == row.AttendanceId)
                    : Builders<AttendanceModel>.Filter.Where(x =>
                        x.CompanyId == CompanyId && x.UserId == request.UserId && x.AttendanceId == row.AttendanceId &&
                        x.SourceType == previousRowSourceType && x.SourceId == previousRowSourceId && x.Version == previousRowVersion);
                var attendanceWrite = transactionSession is not null
                    ? await attendanceCollection.ReplaceOneAsync(transactionSession, attFilter, row)
                    : await attendanceCollection.ReplaceOneAsync(attFilter, row);
                if (attendanceWrite.MatchedCount != 1) throw new InvalidOperationException("WFH_ATTENDANCE_WRITE_CONFLICT");
            }
            changedDates.Add(date);
        }
        return new Result { Success = true, Message = "OK" };
    }

    private async Task SyncDailyAttendanceForHalfDaySegmentAsync(
        WorkFromHomeRequest request,
        EmpUser employee,
        WorkFromHomePolicy policy,
        DateTime date,
        string currentSegmentName,
        string currentWfhCode,
        int sourceVersion,
        IClientSessionHandle? transactionSession)
    {
        var oppositeSegmentName = currentSegmentName == "FIRST_HALF" ? "SECOND_HALF" : "FIRST_HALF";
        var oppositeSegment = await attendanceSegments.FirstOrDefault(x =>
            x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1) && x.Segment == oppositeSegmentName);

        var row = await attendance.FirstOrDefault(x =>
            x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1));

        string targetStatusCode;
        string targetRemarkCode;

        if (oppositeSegment is not null && oppositeSegment.SourceType == "WFH_REQUEST")
        {
            targetStatusCode = !string.IsNullOrWhiteSpace(policy.FullDayAttendanceStatusCode)
                ? policy.FullDayAttendanceStatusCode
                : "WFH";
            targetRemarkCode = "WFH_APPROVED";
        }
        else if (oppositeSegment is not null && (oppositeSegment.SourceType == "LEAVE" || oppositeSegment.Status is "HD" or "HALF_DAY"))
        {
            var wfhHdCode = !string.IsNullOrWhiteSpace(policy.WfhHdAttendanceStatusCode)
                ? policy.WfhHdAttendanceStatusCode
                : "WFH-HD";
            var validWfhHd = await ValidCode(wfhHdCode);
            targetStatusCode = validWfhHd ? wfhHdCode : (!string.IsNullOrWhiteSpace(policy.HalfDayAttendanceStatusCode) ? policy.HalfDayAttendanceStatusCode : "WFH-HD");
            targetRemarkCode = "WFH_HD_COMBINATION";
        }
        else if ((oppositeSegment is not null && (oppositeSegment.SourceType is "PORTAL_CLOCK" or "ADMIN_MANUAL" || oppositeSegment.Status is "P" or "PRESENT"))
                 || (row is not null && row.CheckInTime.HasValue && row.SourceType != "WFH_REQUEST" && row.SourceType != "LEAVE"))
        {
            var wfhWfoCode = !string.IsNullOrWhiteSpace(policy.WfhWfoAttendanceStatusCode)
                ? policy.WfhWfoAttendanceStatusCode
                : "WFH+WFO";
            var validWfhWfo = await ValidCode(wfhWfoCode);
            targetStatusCode = validWfhWfo ? wfhWfoCode : "WFH+WFO";
            targetRemarkCode = "WFH_WFO_COMBINATION";
        }
        else
        {
            var halfCode = !string.IsNullOrWhiteSpace(policy.HalfDayAttendanceStatusCode)
                ? policy.HalfDayAttendanceStatusCode
                : (!string.IsNullOrWhiteSpace(policy.WfhHdAttendanceStatusCode) ? policy.WfhHdAttendanceStatusCode : "WFH-HD");
            var validHalf = await ValidCode(halfCode);
            targetStatusCode = validHalf ? halfCode : "WFH-HD";
            targetRemarkCode = "WFH_HALF_DAY";
        }

        var isNewRow = row is null || string.IsNullOrWhiteSpace(row.AttendanceId);
        row ??= new AttendanceModel
        {
            CompanyId = CompanyId,
            UserId = request.UserId,
            EmployeeId = employee.EmployeeId,
            Date = date
        };

        var prevVersion = row.Version;
        var prevSourceType = row.SourceType;
        var prevSourceId = row.SourceId;
        row.Status = targetStatusCode.Trim().ToUpperInvariant();
        row.RemarkCode = targetRemarkCode;
        row.ModifiedAtUtc = DateTime.UtcNow;
        row.ModifiedByUserId = UserId;

        if (string.IsNullOrWhiteSpace(row.SourceType) || AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(row.SourceType) || request.ReasonCode == "ADMIN_GRANT")
        {
            row.SourceType = "WFH_REQUEST";
            row.SourceId = request.RequestId;
            row.SourceVersion = sourceVersion;
        }

        if (isNewRow || request.ReasonCode == "ADMIN_GRANT" || AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(prevSourceType))
        {
            row.MarkedByUserId = UserId;
            row.MarkedAtUtc = DateTime.UtcNow;
            row.Remarks = request.ReasonCode == "ADMIN_GRANT"
                ? $"Administrative WFH grant: {request.ReasonText}"
                : $"Generated from approved half-day WFH request {request.RequestId}{(string.IsNullOrWhiteSpace(request.ReasonText) ? "" : $": {request.ReasonText}")}";
        }

        var hasPortalClock = row.Remarks != null && (row.Remarks.Contains("Checked in through employee WFH portal") || row.Remarks.Contains("Checked out through employee WFH portal"));
        if (!hasPortalClock && (AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(prevSourceType) || isNewRow))
        {
            if (oppositeSegment is not null && oppositeSegment.CheckInTime.HasValue)
            {
                row.CheckInTime = oppositeSegment.CheckInTime;
                row.CheckOutTime = oppositeSegment.CheckOutTime;
                row.TotalHours = oppositeSegment.TotalHours;
            }
            else
            {
                row.CheckInTime = null;
                row.CheckOutTime = null;
                row.TotalHours = null;
            }
        }
        row.Version++;

        var attendanceCollection = attendance.GetCollection();
        if (isNewRow)
        {
            if (transactionSession is not null)
                await attendanceCollection.InsertOneAsync(transactionSession, row);
            else
                await attendanceCollection.InsertOneAsync(row);
        }
        else
        {
            var attFilter = request.ReasonCode == "ADMIN_GRANT"
                ? Builders<AttendanceModel>.Filter.Where(x =>
                    x.CompanyId == CompanyId && x.UserId == request.UserId && x.AttendanceId == row.AttendanceId)
                : Builders<AttendanceModel>.Filter.Where(x =>
                    x.CompanyId == CompanyId && x.UserId == request.UserId && x.AttendanceId == row.AttendanceId &&
                    x.SourceType == prevSourceType && x.SourceId == prevSourceId && x.Version == prevVersion);
            var write = transactionSession is not null
                ? await attendanceCollection.ReplaceOneAsync(transactionSession, attFilter, row)
                : await attendanceCollection.ReplaceOneAsync(attFilter, row);
            if (write.MatchedCount != 1) throw new InvalidOperationException("WFH_ATTENDANCE_WRITE_CONFLICT");
        }
    }

    private async Task<(string Code, string Message)?> ValidateRequest(EmpUser employee, WorkFromHomePolicy policy, WorkFromHomeRequestDto dto)
    {
        if (!HasRequiredBusinessDates(dto)) return ("WFH_DATE_REQUIRED", "Select both a start date and an end date.");
        var fromDate = BusinessDateUtc(dto.FromDate.ToDateTime(TimeOnly.MinValue));
        var toDate = BusinessDateUtc(dto.ToDate.ToDateTime(TimeOnly.MinValue));
        var schedule = await effectiveSchedules.ResolveAsync(CompanyId, employee, fromDate);
        var companyNow = CompanyNow(schedule?.TimeZoneId ?? policy.TimeZoneId);
        var officeStart = schedule?.StartTime ?? policy.OfficeStartTime;

        if (fromDate.Date > toDate.Date) return ("WFH_INVALID_DATE_RANGE", "End date cannot be before start date.");
        if ((toDate.Date - fromDate.Date).TotalDays > 30) return ("WFH_DATE_RANGE_TOO_LONG", "A single WFH request cannot exceed 31 calendar days.");
        if (fromDate.Date < companyNow.Date && !policy.AllowBackdatedRequest) return ("WFH_BACKDATED_NOT_ALLOWED", "Backdated WFH dates are not allowed.");
        if (policy.AllowBackdatedRequest && policy.MaximumBackdatedDays is int max && fromDate.Date < companyNow.Date.AddDays(-max)) return ("WFH_BACKDATED_LIMIT", "The backdated request exceeds the policy limit.");
        if (policy.MinimumAdvanceNoticeHours > 0)
        {
            var startOfWork = fromDate.Date.Add(officeStart ?? TimeSpan.FromHours(9));
            if (fromDate.Date > companyNow.Date && startOfWork < companyNow.AddHours(policy.MinimumAdvanceNoticeHours))
                return ("WFH_ADVANCE_NOTICE_REQUIRED", $"Requests must be submitted at least {policy.MinimumAdvanceNoticeHours} hour(s) in advance.");
        }
        if (policy.RequireAttachment) return ("WFH_ATTACHMENT_UNSUPPORTED", "This WFH policy requires an attachment, but the request flow cannot accept attachments. Contact HR to update the policy.");
        if (policy.RequireReason && string.IsNullOrWhiteSpace(dto.ReasonText)) return ("WFH_REASON_REQUIRED", "A reason is required.");
        // WFH+SL and WFH+CL used to be accepted as labels only: they did not
        // create or debit a real leave request. Keep the API truthful by allowing
        // only durations that have an unambiguous attendance ownership model.
        if (dto.DurationType is not ("FullDay" or "FirstHalf" or "SecondHalf" or "Mixed")) return ("WFH_INVALID_DURATION", "Invalid WFH duration. Submit a real half-day leave for the other half of the day.");
        if (dto.DurationType is "FirstHalf" or "SecondHalf" && !policy.AllowHalfDay) return ("WFH_HALF_DAY_NOT_ALLOWED", "Half day WFH is not allowed.");
        if (dto.DurationType is "FirstHalf" or "SecondHalf" && fromDate.Date != toDate.Date) return ("WFH_HALF_DAY_SINGLE_DATE", "Half-day Work From Home requests can only be submitted for a single day.");
        if (dto.DurationType == "Mixed") return ("WFH_MIXED_DAY_UNSUPPORTED", "Mixed WFH/WFO is not available until attendance is represented as separate half-day segments.");
        var activeRequests = (await requests.GetAll(x => x.CompanyId == CompanyId && x.UserId == employee.UserId &&
            (x.Status == "Pending" || x.Status == "Approved" || x.Status == "Returned") &&
            x.ToDate >= fromDate.Date.AddDays(-34) && x.FromDate <= toDate.Date.AddDays(34))).ToList();
        if (activeRequests.Any(x => WfhRequestConflicts(x, fromDate.Date, toDate.Date, dto.DurationType))) return ("WFH_OVERLAPPING_REQUEST", "The request overlaps an existing WFH request.");
        if (await HasActiveLeaveOverlap(employee.UserId, fromDate.Date, toDate.Date, dto.DurationType)) return ("WFH_LEAVE_OVERLAP", "The request overlaps leave for the same half or a full day.");
        var maxWorkingDays = await GetMaxWorkingDaysPerWeekAsync(policy);
        foreach (var week in Days(fromDate, toDate).Select(StartOfWeek).Distinct())
        {
            var usedDates = WeeklyUsage(activeRequests.Where(x => x.FromDate < week.AddDays(7) && x.ToDate >= week), week);
            var requestedDates = WeeklyUsage(fromDate, toDate, dto.DurationType, week);
            var weeklyLimit = WeeklyLimitFor(policy, employee, maxWorkingDays);
            if (usedDates + requestedDates > weeklyLimit) return ("WFH_WEEKLY_QUOTA_EXCEEDED", $"Only {weeklyLimit} WFH day(s) are allowed per week.");
        }
        if (policy.MaxDaysPerMonth is int monthlyLimit)
        {
            foreach (var month in Days(fromDate, toDate).Select(day => new DateTime(day.Year, day.Month, 1)).Distinct())
            {
                var used = MonthlyUsage(activeRequests.Where(x => x.FromDate < month.AddMonths(1) && x.ToDate >= month), month);
                var requested = MonthlyUsage(fromDate, toDate, dto.DurationType, month);
                if (used + requested > monthlyLimit) return ("WFH_MONTHLY_QUOTA_EXCEEDED", $"Only {monthlyLimit} WFH day(s) are allowed in a calendar month.");
            }
        }
        foreach (var day in Days(fromDate, toDate))
        {
            var dayName = day.DayOfWeek.ToString();
            if (policy.BlockedDaysOfWeek?.Any(x => string.Equals(x, dayName, StringComparison.OrdinalIgnoreCase)) == true)
                return ("WFH_CORE_OFFICE_DAY_BLOCKED", $"{dayName} is a mandatory in-office collaboration day. Work from home is not permitted.");

            if (policy.AllowedDaysOfWeek?.Count > 0 && !policy.AllowedDaysOfWeek.Any(x => string.Equals(x, dayName, StringComparison.OrdinalIgnoreCase)))
                return ("WFH_DAY_NOT_ALLOWED", $"Work from home is only permitted on: {string.Join(", ", policy.AllowedDaysOfWeek)}.");

            if (!IsEffectiveFor(policy, day)) return ("WFH_POLICY_NOT_EFFECTIVE", "One or more selected dates are outside the effective WFH policy period.");
            if (!IsLifecycleAvailableForWfh(employee, day)) return ("WFH_LIFECYCLE_NOT_WORKING", $"WFH is unavailable on {day:dd-MMM-yyyy} because the employee is not in a working lifecycle state.");
            var check = await ValidateWorkingDay(employee, policy, day); if (check is not null) return check;
        }
        if (policy.MaxConsecutiveDays is int consecutiveLimit)
        {
            var maxConsecutive = await MaximumConsecutiveWfhDays(employee.UserId, policy, activeRequests, fromDate.Date, toDate.Date);
            if (maxConsecutive > consecutiveLimit) return ("WFH_CONSECUTIVE_LIMIT_EXCEEDED", $"The request would exceed the limit of {consecutiveLimit} consecutive WFH working day(s).");
        }

        var officeEnd = schedule?.EndTime ?? policy.OfficeEndTime;
        var effectiveShiftStart = officeStart ?? new TimeSpan(9, 30, 0);
        var effectiveShiftEnd = officeEnd ?? new TimeSpan(18, 30, 0);
        var firstHalfCutoff = ResolveEffectiveCutoffTime(effectiveShiftStart, policy.SameDayCutoffTime, policy.SameDayGraceMinutes) ?? effectiveShiftStart;
        var secondHalfCutoff = ResolveSecondHalfCutoffTime(officeStart, schedule?.RequiredWorkingMinutes, policy.FullDayMinimumHours, policy.SameDayGraceMinutes);

        var touchesToday = Days(fromDate, toDate).Any(day => day.Date == companyNow.Date);
        if (touchesToday)
        {
            if (companyNow.TimeOfDay >= effectiveShiftEnd)
            {
                return ("WFH_SHIFT_ENDED", "Work From Home cannot be requested after working hours have ended for today.");
            }

            if (dto.DurationType is "FullDay")
            {
                if (companyNow.TimeOfDay >= firstHalfCutoff)
                {
                    var label = companyNow.Date.Add(firstHalfCutoff).ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
                    return ("WFH_SHIFT_ALREADY_STARTED", $"Work From Home for a full day must be requested before your shift start time ({label}). If applying after shift start, you may only apply for Second Half.");
                }
            }
            else if (dto.DurationType is "FirstHalf")
            {
                if (companyNow.TimeOfDay >= firstHalfCutoff)
                {
                    var label = companyNow.Date.Add(firstHalfCutoff).ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
                    return ("WFH_FIRST_HALF_PASSED", $"First-half Work From Home must be requested before your shift start time ({label}). If applying after shift start, you may only apply for Second Half.");
                }
            }
            else if (dto.DurationType is "SecondHalf")
            {
                if (companyNow.TimeOfDay >= secondHalfCutoff)
                {
                    if (!policy.AllowLateApplicationWithApproval)
                    {
                        var label = companyNow.Date.Add(secondHalfCutoff).ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
                        return ("WFH_SECOND_HALF_PASSED", $"Second-half Work From Home must be requested before the cutoff time ({label}).");
                    }
                    if (string.IsNullOrWhiteSpace(dto.ReasonText))
                    {
                        return ("WFH_LATE_REASON_REQUIRED", "A reason is required when applying for Work From Home after the cutoff time.");
                    }
                }
            }
        }
        return null;
    }

    internal static bool HasRequiredBusinessDates(WorkFromHomeRequestDto dto) =>
        dto is not null && dto.FromDate != default && dto.ToDate != default;

    internal static bool WfhRequestConflicts(WorkFromHomeRequest existing, DateTime fromDate, DateTime toDate, string durationType)
    {
        if (fromDate.Date > existing.ToDate.Date || toDate.Date < existing.FromDate.Date)
            return false;

        // If both are single-day requests on the exact same date and occupy opposite halves, they do not conflict.
        if (fromDate.Date == toDate.Date && existing.FromDate.Date == existing.ToDate.Date && fromDate.Date == existing.FromDate.Date)
        {
            var isOppositeHalves = (durationType == "FirstHalf" && existing.DurationType == "SecondHalf") ||
                                   (durationType == "SecondHalf" && existing.DurationType == "FirstHalf");
            if (isOppositeHalves)
                return false;
        }

        return true;
    }

    private async Task<bool> HasActiveLeaveOverlap(string userId, DateTime fromDate, DateTime toDate, string wfhDurationType)
    {
        var activeLeaves = await leaveRequests.GetAll(request =>
            request.CompanyId == CompanyId && request.EmployeeId == userId &&
            (request.Status == EnumsHelper.LeaveRequestStatus.Pending || request.Status == EnumsHelper.LeaveRequestStatus.Accepted) &&
            request.StartDate <= toDate && request.EndDate >= fromDate);
        return activeLeaves.Any(leave => LeaveConflictsWithWfh(leave, wfhDurationType));
    }

    // Full-day requests own both halves. A half-day leave and WFH can coexist only
    // when they explicitly occupy opposite attendance segments.
    internal static bool LeaveConflictsWithWfh(LeaveRequest leave, string wfhDurationType)
    {
        if (!leave.IsHalfDay)
            return true;

        var wfhSegment = wfhDurationType switch
        {
            "FirstHalf" => "FIRST_HALF",
            "SecondHalf" => "SECOND_HALF",
            _ => null
        };

        return wfhSegment is null || string.Equals(leave.HalfDayPeriod, wfhSegment, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(string Code, string Message)?> ValidateWorkingDay(EmpUser employee, WorkFromHomePolicy policy, DateTime date)
    {
        var month = new DateTime(date.Year, date.Month, 1);
        if (await summaries.Exist(x => x.CompanyId == CompanyId && x.EmployeeId == employee.EmployeeId && x.PayrollMonth == month && x.IsLocked)) return ("WFH_DATE_LOCKED", "Attendance month is locked.");
        var off = await weeklyOffs.FirstOrDefault(x => x.CompanyId == CompanyId);
        if (!policy.AllowOnWeeklyOff && (off?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday]).Contains((int)date.DayOfWeek)) return ("WFH_WEEKLY_OFF_NOT_ALLOWED", "WFH is not allowed on a weekly off.");
        var holidays = await calendar.GetAll(x => x.CompanyId == CompanyId && x.Type == EnumsHelper.CalendarItem.Holiday &&
            // A ±1 day window tolerates a legacy row stored as an IST-midnight instant
            // converted to UTC; CalendarDateHelpers.MatchesDate makes the exact call.
            (x.Recurring || (x.Date >= date.Date.AddDays(-1) && x.Date <= date.Date.AddDays(1))));
        if (!policy.AllowOnHoliday && holidays.Any(x => CalendarDateHelpers.MatchesDate(x, date))) return ("WFH_HOLIDAY_NOT_ALLOWED", "WFH is not allowed on a holiday.");
        return null;
    }

    private async Task<string> ResolveApprover(EmpUser employee, WorkFromHomePolicy policy)
    {
        var manager = await ActiveEmployee(employee.ReportingManager);
        if (manager is not null && manager.UserId != employee.UserId) return manager.UserId;
        // A manager's own request escalates to the manager's manager; no role-name fallback is trusted here.
        return manager is null ? string.Empty : (await ActiveEmployee(manager.ReportingManager))?.UserId ?? string.Empty;
    }

    private async Task<EmpUser?> ActiveEmployee(string userId) => string.IsNullOrWhiteSpace(userId) ? null : await employees.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == userId && x.Status && !x.IsDeleted && (x.Lifecycle == null || x.Lifecycle.EmploymentStatus == null || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended) && (x.EmploymentStatus == null || x.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.EmploymentStatus == EmployeeLifecycleStatuses.Suspended));
    private async Task<EmpUser?> EmployeeEmployedOn(string userId, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var employee = await employees.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == userId && !x.IsDeleted);
        return employee is not null && EmploymentPeriod.WasEmployedOn(employee, date) ? employee : null;
    }

    // Half-day WFH is source-owned by one attendance segment.  Use the effective
    // schedule's working minutes (not its elapsed span, which can include lunch)
    // so a 09:00 start and 480-minute day changes ownership at 13:00.
    public static bool IsHalfDayClockInWithinRequestedSegment(string durationType, TimeSpan clockInTime,
        TimeSpan? officeStartTime, int? requiredWorkingMinutes, decimal policyFullDayHours)
    {
        var boundary = HalfDayBoundary(officeStartTime, requiredWorkingMinutes, policyFullDayHours);
        if (!boundary.HasValue || durationType is not ("FirstHalf" or "SecondHalf")) return true;
        return durationType == "FirstHalf" ? clockInTime < boundary.Value : clockInTime >= boundary.Value;
    }

    internal static TimeSpan? HalfDayBoundary(TimeSpan? officeStartTime, int? requiredWorkingMinutes, decimal policyFullDayHours)
    {
        if (!officeStartTime.HasValue) return null;
        var fullDayMinutes = requiredWorkingMinutes.GetValueOrDefault();
        if (fullDayMinutes <= 0) fullDayMinutes = (int)Math.Round(policyFullDayHours * 60m, MidpointRounding.AwayFromZero);
        return fullDayMinutes > 0 ? officeStartTime.Value.Add(TimeSpan.FromMinutes(fullDayMinutes / 2d)) : null;
    }

    private async Task<WorkFromHomePolicy?> ActivePolicy() =>
        !await IsWfhAccessibleAsync() ? null : await policies.FirstOrDefault(x => x.CompanyId == CompanyId && x.IsEnabled);
    private async Task<bool> ValidCode(string code) => !string.IsNullOrWhiteSpace(code) && await statuses.Exist(x => x.CompanyId == CompanyId && x.Code == code.Trim().ToUpperInvariant() && x.IsActive);
    private async Task InvalidateSummary(EmpUser employee, DateTime day) => await summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == CompanyId && x.EmployeeId == employee.EmployeeId && x.PayrollMonth == new DateTime(day.Year, day.Month, 1) && !x.IsLocked));
    private static string DisplayName(IEnumerable<MultilingualModel>? titles) =>
        titles?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Label))?.Label?.Trim() ?? "Not assigned";
    private static string WfhRequestEmail(string subject, EmpUser employee, WorkFromHomeRequest request, string department, string jobTitle)
    {
        var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
        var dateRange = request.FromDate.Date == request.ToDate.Date ? request.FromDate.ToString("dddd, dd MMMM yyyy") : $"{request.FromDate:dd MMM yyyy} - {request.ToDate:dd MMM yyyy}";
        var details = new List<(string Label, string Value)>
        {
            ("Employee", employeeName), ("Employee ID", employee.EmployeeId), ("Department", department), ("Job role", jobTitle),
            ("WFH date", dateRange), ("Duration", DurationLabel(request.DurationType)), ("Reason category", ReasonLabel(request.ReasonCode))
        };
        if (!string.IsNullOrWhiteSpace(request.ReasonText)) details.Add(("Employee note", request.ReasonText.Trim()));
        return RequestEmailLayout(subject, $"A Work From Home request has been submitted by <strong>{WebUtility.HtmlEncode(employeeName)}</strong> and is awaiting review.", details, "Pending review");
    }
    private static string WfhStatusEmail(string subject, EmpUser employee, WorkFromHomeRequest request)
    {
        var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
        var dateRange = request.FromDate.Date == request.ToDate.Date ? request.FromDate.ToString("dddd, dd MMMM yyyy") : $"{request.FromDate:dd MMM yyyy} - {request.ToDate:dd MMM yyyy}";
        var details = new List<(string Label, string Value)> { ("WFH date", dateRange), ("Duration", DurationLabel(request.DurationType)), ("Request status", request.Status) };
        if (!string.IsNullOrWhiteSpace(request.ReviewRemarksText)) details.Add(("Reviewer note", request.ReviewRemarksText.Trim()));
        return RequestEmailLayout(subject, $"Hello {WebUtility.HtmlEncode(employeeName)}, your Work From Home request has been updated.", details, request.Status);
    }
    private static string WfhCancellationEmail(string subject, EmpUser employee, WorkFromHomeRequest request, string? remarksText)
    {
        var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
        var dateRange = request.FromDate.Date == request.ToDate.Date ? request.FromDate.ToString("dddd, dd MMMM yyyy") : $"{request.FromDate:dd MMM yyyy} - {request.ToDate:dd MMM yyyy}";
        var details = new List<(string Label, string Value)>
        {
            ("Employee", employeeName), ("Employee ID", employee.EmployeeId),
            ("WFH date", dateRange), ("Duration", DurationLabel(request.DurationType)), ("Request status", "Cancelled")
        };
        if (!string.IsNullOrWhiteSpace(remarksText)) details.Add(("Cancellation note", remarksText.Trim()));
        return RequestEmailLayout(subject, $"Work From Home for <strong>{WebUtility.HtmlEncode(employeeName)}</strong> has been cancelled.", details, "Cancelled");
    }
    private static string RequestEmailLayout(string title, string introduction, IEnumerable<(string Label, string Value)> details, string status)
    {
        var rows = string.Concat(details.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => $"<tr><td style=\"width:36%;padding:10px 14px;color:#64748b;font-weight:600;border-bottom:1px solid #e7edf4;vertical-align:top\">{WebUtility.HtmlEncode(x.Label)}</td><td style=\"padding:10px 0;border-bottom:1px solid #e7edf4;color:#1e293b;vertical-align:top\">{WebUtility.HtmlEncode(x.Value)}</td></tr>"));
        return $"<div style=\"font-family:Arial,Helvetica,sans-serif;color:#1e293b\"><h1 style=\"font-size:22px;line-height:1.3;margin:0 0 12px;color:#172554\">{WebUtility.HtmlEncode(title)}</h1><p style=\"margin:0 0 20px;line-height:1.6\">{introduction}</p><div style=\"display:inline-block;padding:7px 12px;margin:0 0 18px;background:#eef2ff;border-radius:999px;color:#4338ca;font-size:13px;font-weight:700\">{WebUtility.HtmlEncode(status)}</div><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;border-collapse:collapse;border-top:1px solid #e7edf4\">{rows}</table><p style=\"margin:20px 0 0;color:#64748b;font-size:13px;line-height:1.5\">Please review the request in Codeji CMS to take the next action.</p></div>";
    }
    private WorkFromHomeOutboxEvent BuildAuditOutboxEvent(WorkFromHomeRequest request, string? previous, string current, string action, string? code, string? text, string actorUserId)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId))
            request.RequestId = Guid.NewGuid().ToString();
        var eventId = Guid.NewGuid().ToString("N");
        return new WorkFromHomeOutboxEvent
        {
            EventId = eventId,
            CompanyId = CompanyId,
            CreatedBy = actorUserId,
            CreatedDate = DateTime.UtcNow,
            RequestId = request.RequestId,
            DedupeKey = $"audit:{request.RequestId}:{eventId}",
            EventType = "AUDIT",
            ActorUserId = actorUserId,
            PreviousStatus = previous,
            NewStatus = current,
            Action = action,
            RemarksCode = code,
            RemarksText = Clean(text),
            RequestVersion = request.Version,
            Status = "Pending",
            AvailableAtUtc = DateTime.UtcNow
        };
    }

    private WorkFromHomeOutboxEvent BuildNotificationOutboxEvent(
        WorkFromHomeRequest request, string actorUserId, string recipientUserId, string title, string body,
        EnumsHelper.NotificationTypes notificationType, string actionKey,
        string? recipientEmail = null, string? emailBody = null, EnumsHelper.MailType? emailType = null)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId))
            request.RequestId = Guid.NewGuid().ToString();
        var eventId = Guid.NewGuid().ToString("N");
        return new WorkFromHomeOutboxEvent
        {
            EventId = eventId,
            CompanyId = CompanyId,
            CreatedBy = actorUserId,
            CreatedDate = DateTime.UtcNow,
            RequestId = request.RequestId,
            DedupeKey = $"notify:{request.RequestId}:{request.Version}:{actionKey}:{recipientUserId}",
            EventType = "NOTIFICATION",
            ActorUserId = actorUserId,
            RecipientUserId = recipientUserId,
            Title = title,
            Body = body,
            NotificationType = (int)notificationType,
            SendEmail = !string.IsNullOrWhiteSpace(recipientEmail) && emailType.HasValue,
            RecipientEmail = recipientEmail,
            EmailSubject = title,
            EmailBody = emailBody,
            EmailType = emailType.HasValue ? (int)emailType.Value : null,
            Status = "Pending",
            AvailableAtUtc = DateTime.UtcNow
        };
    }

    private async Task<List<WorkFromHomeOutboxEvent>> BuildHrNotificationOutboxEventsAsync(
        WorkFromHomeRequest request, EmpUser employee, string subject, string actionKey, string actorUserId)
    {
        var roleIds = (await companyRoles.GetAll(x => x.CompanyId == CompanyId && !x.IsDeleted &&
            (x.RoleType == (int)EnumsHelper.Roles.Administrator || x.RoleType == (int)EnumsHelper.Roles.HR || x.RoleType == (int)EnumsHelper.Roles.HRExecutive), withDefaultFilter: false))
            .Select(x => x.RolesId).ToHashSet(StringComparer.Ordinal);
        var recipients = (await employees.GetAll(x => x.CompanyId == CompanyId && x.Status && !x.IsDeleted && roleIds.Contains(x.RoleId), withDefaultFilter: false)).ToList();
        if (!string.IsNullOrWhiteSpace(request.ApproverUserId))
        {
            var approver = (await employees.GetAll(x => x.CompanyId == CompanyId && x.UserId == request.ApproverUserId && x.Status && !x.IsDeleted, withDefaultFilter: false)).FirstOrDefault();
            if (approver is not null && recipients.All(x => x.UserId != approver.UserId)) recipients.Add(approver);
        }
        recipients = recipients.Where(x => x.UserId != actorUserId).GroupBy(x => x.UserId, StringComparer.Ordinal).Select(x => x.First()).ToList();
        var department = string.IsNullOrWhiteSpace(employee.Department)
            ? "Not assigned"
            : DisplayName((await departments.FirstOrDefault(x => x.CompanyId == CompanyId && x.DepartmentId == employee.Department && !x.IsDeleted))?.Titles);
        var jobTitle = string.IsNullOrWhiteSpace(employee.JobRole)
            ? "Not assigned"
            : DisplayName((await jobTitles.FirstOrDefault(x => x.CompanyId == CompanyId && x.JobTitleId == employee.JobRole && !x.IsDeleted))?.Titles);
        var body = $"{employee.FirstName} {employee.LastName} ({employee.EmployeeId}) - {request.FromDate:dd MMM}";
        var emailBody = WfhRequestEmail(subject, employee, request, department, jobTitle);
        return recipients.Select(recipient => BuildNotificationOutboxEvent(
            request, actorUserId, recipient.UserId, subject, body,
            EnumsHelper.NotificationTypes.WorkFromHomeRequest, actionKey,
            recipient.IsEmailVerified ? recipient.Email : null, emailBody, EnumsHelper.MailType.LeaveMailToHR)).ToList();
    }

    private async Task<List<WorkFromHomeOutboxEvent>> BuildCancellationNotificationOutboxEventsAsync(
        WorkFromHomeRequest request, EmpUser employee, string subject, string? remarksText, string actorUserId)
    {
        var roleIds = (await companyRoles.GetAll(x => x.CompanyId == CompanyId && !x.IsDeleted &&
            (x.RoleType == (int)EnumsHelper.Roles.Administrator || x.RoleType == (int)EnumsHelper.Roles.HR || x.RoleType == (int)EnumsHelper.Roles.HRExecutive), withDefaultFilter: false))
            .Select(x => x.RolesId).ToHashSet(StringComparer.Ordinal);
        var recipients = (await employees.GetAll(x => x.CompanyId == CompanyId && x.Status && !x.IsDeleted && roleIds.Contains(x.RoleId), withDefaultFilter: false)).ToList();
        if (!string.IsNullOrWhiteSpace(request.ApproverUserId))
        {
            var approver = (await employees.GetAll(x => x.CompanyId == CompanyId && x.UserId == request.ApproverUserId && x.Status && !x.IsDeleted, withDefaultFilter: false)).FirstOrDefault();
            if (approver is not null && recipients.All(x => x.UserId != approver.UserId)) recipients.Add(approver);
        }
        recipients = recipients.Where(x => x.UserId != actorUserId).GroupBy(x => x.UserId, StringComparer.Ordinal).Select(x => x.First()).ToList();
        var dateRange = request.FromDate.Date == request.ToDate.Date ? request.FromDate.ToString("dd MMM yyyy") : $"{request.FromDate:dd MMM yyyy} - {request.ToDate:dd MMM yyyy}";
        var note = !string.IsNullOrWhiteSpace(remarksText) ? $" Reason: {remarksText.Trim()}" : string.Empty;
        var body = $"{employee.FirstName} {employee.LastName} ({employee.EmployeeId}) cancelled WFH for {dateRange}.{note}";
        var emailBody = WfhCancellationEmail(subject, employee, request, remarksText);
        return recipients.Select(recipient => BuildNotificationOutboxEvent(request, actorUserId, recipient.UserId,
            subject, body, EnumsHelper.NotificationTypes.WorkFromHomeRequest, "Cancelled",
            recipient.IsEmailVerified ? recipient.Email : null, emailBody, EnumsHelper.MailType.LeaveReplyMail)).ToList();
    }

    private WorkFromHomeOutboxEvent BuildEmployeeNotificationOutboxEvent(
        WorkFromHomeRequest request, EmpUser employee, string subject, EnumsHelper.NotificationTypes type, string actionKey, string actorUserId)
    {
        var dateRange = request.FromDate.Date == request.ToDate.Date ? request.FromDate.ToString("dd MMM yyyy") : $"{request.FromDate:dd MMM yyyy} - {request.ToDate:dd MMM yyyy}";
        var note = !string.IsNullOrWhiteSpace(request.ReviewRemarksText) ? $" Note: {request.ReviewRemarksText.Trim()}" : string.Empty;
        var body = $"WFH dates: {dateRange}.{note}";
        return BuildNotificationOutboxEvent(request, actorUserId, employee.UserId, subject, body, type, actionKey,
            employee.IsEmailVerified ? employee.Email : null, WfhStatusEmail(subject, employee, request), EnumsHelper.MailType.LeaveReplyMail);
    }

    private Task InsertOutboxEventsAsync(IClientSessionHandle? session, IReadOnlyCollection<WorkFromHomeOutboxEvent> events, CancellationToken cancellationToken) =>
        events.Count == 0
            ? Task.CompletedTask
            : session is not null
                ? outbox.GetCollection().InsertManyAsync(session, events, cancellationToken: cancellationToken)
                : outbox.GetCollection().InsertManyAsync(events, cancellationToken: cancellationToken);

    private static IEnumerable<DateTime> Days(DateTime from, DateTime to) { for (var d = from.Date; d <= to.Date; d = d.AddDays(1)) yield return d; }
    private static DateTime StartOfWeek(DateTime value) => value.Date.AddDays(-(((int)value.DayOfWeek + 6) % 7));
    private static bool IsEligible(WorkFromHomePolicy policy, EmpUser employee)
    {
        if ((policy.ExcludedEmployeeIds ?? []).Contains(employee.EmployeeId, StringComparer.Ordinal)) return false;
        // A positive employee allocation is an explicit WFH assignment, even if
        // the company policy does not otherwise apply to all employees.
        if ((policy.EmployeeAllocations ?? []).Any(x => string.Equals(x.EmployeeId, employee.EmployeeId, StringComparison.Ordinal) && x.WeeklyLimit > 0)) return true;
        if ((policy.ApplicableEmployeeIds ?? []).Contains(employee.EmployeeId, StringComparer.Ordinal)) return true;
        if ((policy.ApplicableDepartments ?? []).Contains(employee.Department, StringComparer.OrdinalIgnoreCase)) return true;
        if (employee.EmploymentType.HasValue && (policy.ApplicableEmploymentTypes ?? []).Contains(employee.EmploymentType.Value)) return true;
        return policy.ApplyToAllEmployees;
    }
    /// <summary>
    /// Temporary availability states, including UnauthorizedAbsence, do not end
    /// employment and therefore must remain eligible for WFH submission/approval.
    /// Legacy records without Lifecycle continue to use Status and EmploymentStatus.
    /// </summary>
    internal static bool IsCurrentEmployeeForWorkflow(EmpUser employee)
    {
        if (employee.Lifecycle is not null && !string.IsNullOrWhiteSpace(employee.Lifecycle.EmploymentStatus))
            return employee.Lifecycle.IsCurrentEmployee();
        return EmploymentExitRules.IsCurrentEmployee(employee);
    }
    internal static bool IsLifecycleAvailableForWfh(EmpUser employee, DateTime date)
        => EmploymentPeriod.ResolveAvailabilityOn(employee, date) == EmployeeLifecycleStatuses.Working;
    private async Task<int> GetMaxWorkingDaysPerWeekAsync(WorkFromHomePolicy? policy = null)
    {
        if (policy?.AllowOnWeeklyOff == true) return 7;
        var weeklyOff = await weeklyOffs.FirstOrDefault(x => x.CompanyId == CompanyId);
        var offDays = (weeklyOff?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday])
            .Distinct()
            .Where(d => d is >= 0 and <= 6)
            .ToList();
        return Math.Max(1, Math.Min(7, 7 - offDays.Count));
    }

    private static int WeeklyLimitFor(WorkFromHomePolicy policy, EmpUser employee, int maxWorkingDays = 7)
    {
        var raw = (policy.EmployeeAllocations ?? []).FirstOrDefault(x => string.Equals(x.EmployeeId, employee.EmployeeId, StringComparison.Ordinal))?.WeeklyLimit ?? policy.MaxDaysPerWeek;
        return Math.Max(0, Math.Min(raw, maxWorkingDays));
    }
    // Pending and returned requests reserve capacity, matching the existing
    // submission rule. Rejected and cancelled records remain historical only.
    private static bool CountsTowardsWeeklyAllowance(string status) => status is "Pending" or "Approved" or "Returned";
    private static decimal WeeklyUsage(IEnumerable<WorkFromHomeRequest> requests, DateTime week) => requests.Sum(request =>
    {
        var unitsPerDay = request.DurationType is "FirstHalf" or "SecondHalf" ? .5m : 1m;
        var convertedDates = (request.OfficeConversions ?? []).Select(x => x.AttendanceDate.Date).ToHashSet();
        return Days(request.FromDate, request.ToDate).Count(day => day >= week && day < week.AddDays(7) && !convertedDates.Contains(day.Date)) * unitsPerDay;
    });
    private static decimal WeeklyUsage(DateTime fromDate, DateTime toDate, string durationType, DateTime week)
    {
        var unitsPerDay = durationType is "FirstHalf" or "SecondHalf" ? .5m : 1m;
        return Days(fromDate, toDate).Count(day => day >= week && day < week.AddDays(7)) * unitsPerDay;
    }
    private static decimal MonthlyUsage(IEnumerable<WorkFromHomeRequest> requests, DateTime month) => requests.Sum(request =>
    {
        var unitsPerDay = request.DurationType is "FirstHalf" or "SecondHalf" ? .5m : 1m;
        var convertedDates = (request.OfficeConversions ?? []).Select(x => x.AttendanceDate.Date).ToHashSet();
        return Days(request.FromDate, request.ToDate).Count(day => day.Year == month.Year && day.Month == month.Month && !convertedDates.Contains(day.Date)) * unitsPerDay;
    });
    private static decimal MonthlyUsage(DateTime fromDate, DateTime toDate, string durationType, DateTime month)
    {
        var unitsPerDay = durationType is "FirstHalf" or "SecondHalf" ? .5m : 1m;
        return Days(fromDate, toDate).Count(day => day.Year == month.Year && day.Month == month.Month) * unitsPerDay;
    }
    internal static decimal MonthlyUsageForPeriod(DateTime fromDate, DateTime toDate, string durationType, DateTime month) =>
        MonthlyUsage(fromDate, toDate, durationType, month);
    private async Task<int> MaximumConsecutiveWfhDays(string userId, WorkFromHomePolicy policy, IEnumerable<WorkFromHomeRequest> activeRequests, DateTime requestedFrom, DateTime requestedTo)
    {
        var windowStart = requestedFrom.Date.AddDays(-34);
        var windowEnd = requestedTo.Date.AddDays(34);
        var wfhDays = new HashSet<DateTime>();
        foreach (var request in activeRequests)
        {
            var convertedDates = (request.OfficeConversions ?? []).Select(x => x.AttendanceDate.Date).ToHashSet();
            foreach (var day in Days(request.FromDate, request.ToDate).Where(day => day >= windowStart && day <= windowEnd && !convertedDates.Contains(day.Date)))
                wfhDays.Add(day.Date);
        }
        foreach (var day in Days(requestedFrom, requestedTo)) wfhDays.Add(day.Date);

        var weeklyOff = await weeklyOffs.FirstOrDefault(x => x.CompanyId == CompanyId);
        var offDays = weeklyOff?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday];
        var holidays = await calendar.GetAll(x => x.CompanyId == CompanyId && x.Type == EnumsHelper.CalendarItem.Holiday &&
            (x.Recurring || (x.Date >= windowStart.AddDays(-1) && x.Date <= windowEnd.AddDays(1))));
        var currentStreak = 0;
        var maximumStreak = 0;
        foreach (var day in Days(windowStart, windowEnd))
        {
            var isWeeklyOff = offDays.Contains((int)day.DayOfWeek) && !policy.AllowOnWeeklyOff;
            var isHoliday = !policy.AllowOnHoliday && holidays.Any(item => CalendarDateHelpers.MatchesDate(item, day));
            if (isWeeklyOff || isHoliday) continue;
            if (wfhDays.Contains(day.Date))
            {
                currentStreak++;
                maximumStreak = Math.Max(maximumStreak, currentStreak);
            }
            else currentStreak = 0;
        }
        return maximumStreak;
    }
    internal static decimal WeeklyUsageForPeriod(DateTime fromDate, DateTime toDate, string durationType, DateTime week) => WeeklyUsage(fromDate, toDate, durationType, week);
    internal static TimeSpan? ResolveEffectiveCutoffTime(TimeSpan? baseTime, TimeSpan? customCutoffTime, int graceMinutes)
    {
        var effectiveBase = customCutoffTime ?? baseTime;
        if (effectiveBase.HasValue)
            return effectiveBase.Value.Add(TimeSpan.FromMinutes(Math.Max(0, graceMinutes)));
        return null;
    }

    internal static TimeSpan ResolveSecondHalfCutoffTime(TimeSpan? officeStartTime, int? requiredWorkingMinutes, decimal fullDayHours, int graceMinutes)
    {
        var effectiveOfficeStart = officeStartTime ?? new TimeSpan(9, 30, 0);
        var secondHalfStart = HalfDayBoundary(officeStartTime, requiredWorkingMinutes, fullDayHours) ?? effectiveOfficeStart.Add(TimeSpan.FromHours(4));
        return secondHalfStart.Add(TimeSpan.FromMinutes(Math.Max(0, graceMinutes)));
    }

    internal static bool IsSameDayWfhStartCutoffReached(DateTime requestedDate, DateTime companyNow, TimeSpan? officeStartTime) =>
        officeStartTime.HasValue && requestedDate.Date == companyNow.Date && companyNow.TimeOfDay >= officeStartTime.Value;

    internal static bool IsSameDayWfhStartCutoffReached(DateTime requestedDate, DateTime companyNow, string durationType, TimeSpan? officeStartTime, TimeSpan? customCutoffTime, int graceMinutes, int? requiredWorkingMinutes, decimal fullDayHours)
    {
        if (requestedDate.Date != companyNow.Date)
            return requestedDate.Date < companyNow.Date;

        var effectiveOfficeStart = officeStartTime ?? new TimeSpan(9, 30, 0);
        var firstHalfCutoff = (customCutoffTime ?? effectiveOfficeStart).Add(TimeSpan.FromMinutes(Math.Max(0, graceMinutes)));

        if (durationType == "SecondHalf")
        {
            var secondHalfStart = HalfDayBoundary(officeStartTime, requiredWorkingMinutes, fullDayHours) ?? effectiveOfficeStart.Add(TimeSpan.FromHours(4));
            var secondHalfCutoff = secondHalfStart.Add(TimeSpan.FromMinutes(Math.Max(0, graceMinutes)));
            return companyNow.TimeOfDay >= secondHalfCutoff;
        }

        return companyNow.TimeOfDay >= firstHalfCutoff;
    }
    public static DateTime BusinessDateUtc(DateTime value) => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    private static bool IsEffectiveFor(WorkFromHomePolicy policy, DateTime date) => (!policy.EffectiveFrom.HasValue || date.Date >= policy.EffectiveFrom.Value.Date) && (!policy.EffectiveTo.HasValue || date.Date <= policy.EffectiveTo.Value.Date);
    private static DateTime CompanyNow(WorkFromHomePolicy policy) => CompanyNow(policy.TimeZoneId);
    private static DateTime CompanyNow(string? timeZoneId)
    {
        try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId)); }
        catch (TimeZoneNotFoundException) { return DateTime.UtcNow; }
        catch (InvalidTimeZoneException) { return DateTime.UtcNow; }
    }
    private static DateTime CompanyDate(DateTime value, string? timeZoneId)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        try { return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId)).Date; }
        catch (TimeZoneNotFoundException) { return utc.Date; }
        catch (InvalidTimeZoneException) { return utc.Date; }
    }
    private static bool TryResolveCompanyLocalTime(DateTime businessDate, TimeSpan time, string? timeZoneId, out DateTime utc)
    {
        utc = default;
        if (time < TimeSpan.Zero || time >= TimeSpan.FromDays(1)) return false;
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId);
            var local = DateTime.SpecifyKind(businessDate.Date.Add(time), DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(local) || zone.IsAmbiguousTime(local)) return false;
            utc = TimeZoneInfo.ConvertTimeToUtc(local, zone);
            return true;
        }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }
    private static string ReasonLabel(string? code) => (code ?? "OTHER").Trim().ToUpperInvariant() switch { "PERSONAL" => "Personal", "MEDICAL" => "Medical", "FAMILY" => "Family", "HOME_MAINTENANCE" => "Home maintenance", "TRAVEL_DISRUPTION" => "Travel disruption", "OPERATIONAL" => "Operational", "MANAGER_INSTRUCTION" => "Manager instruction", _ => "Other" };
    private static string DurationLabel(string? duration) => duration switch { "FirstHalf" => "First half", "SecondHalf" => "Second half", _ => "Full day" };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, 500)];
    private static Result Fail(string code, string message, int statusCode = StatusCodes.Status400BadRequest) => new() { Success = false, StatusCode = statusCode, Message = $"{code}: {message}" };
    private static Result<T> Fail<T>(string code, string message, int statusCode = StatusCodes.Status200OK) => new() { Success = false, StatusCode = statusCode, Message = $"{code}: {message}" };
    private static Result<T> Ok<T>(T value) => new() { Success = true, MethodResult = value };
    private static WorkFromHomePolicyDto Map(WorkFromHomePolicy x) => new() { IsEnabled=x.IsEnabled, MaxDaysPerMonth=x.MaxDaysPerMonth, MaxDaysPerWeek=x.MaxDaysPerWeek, MaxConsecutiveDays=x.MaxConsecutiveDays, MinimumAdvanceNoticeHours=x.MinimumAdvanceNoticeHours, AllowBackdatedRequest=x.AllowBackdatedRequest, MaximumBackdatedDays=x.MaximumBackdatedDays, AllowHalfDay=x.AllowHalfDay, AllowMixedDay=x.AllowMixedDay, ManagerApprovalRequired=x.ManagerApprovalRequired, AllowManagerSelfApproval=x.AllowManagerSelfApproval, AllowOnWeeklyOff=x.AllowOnWeeklyOff, AllowOnHoliday=x.AllowOnHoliday, RequireReason=x.RequireReason, RequireAttachment=x.RequireAttachment, ApplyToAllEmployees=x.ApplyToAllEmployees, ApplicableDepartments=x.ApplicableDepartments ?? [], ApplicableEmployeeIds=x.ApplicableEmployeeIds ?? [], ExcludedEmployeeIds=x.ExcludedEmployeeIds ?? [], ApplicableEmploymentTypes=x.ApplicableEmploymentTypes ?? [], EffectiveFrom=x.EffectiveFrom, EffectiveTo=x.EffectiveTo, AllowedCheckInFrom=x.AllowedCheckInFrom, AllowedCheckInUntil=x.AllowedCheckInUntil, AllowedCheckOutFrom=x.AllowedCheckOutFrom, AllowedCheckOutUntil=x.AllowedCheckOutUntil, OfficeStartTime=x.OfficeStartTime, OfficeEndTime=x.OfficeEndTime, TimeZoneId=x.TimeZoneId, FullDayMinimumHours=x.FullDayMinimumHours, HalfDayMinimumHours=x.HalfDayMinimumHours, FullDayAttendanceStatusCode=x.FullDayAttendanceStatusCode, HalfDayAttendanceStatusCode=x.HalfDayAttendanceStatusCode, MixedAttendanceStatusCode=x.MixedAttendanceStatusCode, WfhWfoAttendanceStatusCode=x.WfhWfoAttendanceStatusCode, WfhHdAttendanceStatusCode=x.WfhHdAttendanceStatusCode, SameDayCutoffTime=x.SameDayCutoffTime, SameDayGraceMinutes=x.SameDayGraceMinutes, AllowLateApplicationWithApproval=x.AllowLateApplicationWithApproval, AllowedDaysOfWeek=x.AllowedDaysOfWeek ?? [], BlockedDaysOfWeek=x.BlockedDaysOfWeek ?? [] };
    private async Task<Dictionary<string, string>> GetDepartmentNameMapAsync()
    {
        try
        {
            var companyDepts = await departments.GetAll(x => x.CompanyId == CompanyId && !x.IsDeleted, withDefaultFilter: false);
            return companyDepts
                .Where(d => !string.IsNullOrWhiteSpace(d.DepartmentId))
                .ToDictionary(
                    d => d.DepartmentId,
                    d => DisplayName(d.Titles),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static WorkFromHomeResponseDto Map(WorkFromHomeRequest x, EmpUser? employee = null, IReadOnlyDictionary<string, string>? departmentMap = null) => new() {
        RequestId=x.RequestId,
        UserId=x.UserId,
        EmployeeId=x.EmployeeId,
        FromDate=ResolveBusinessDateOnly(x.FromDate),
        ToDate=ResolveBusinessDateOnly(x.ToDate),
        DurationType=x.DurationType,
        ReasonCode=x.ReasonCode,
        ReasonText=x.ReasonText,
        Status=x.Status,
        ApproverUserId=x.ApproverUserId,
        Version=x.Version,
        ReviewRemarksText=x.ReviewRemarksText,
        CreatedDate=x.CreatedDate,
        ReviewedByUserId=x.ReviewedByUserId,
        ReviewedAt=x.ReviewedAt,
        IsLateSubmission=x.IsLateSubmission,
        OfficeConvertedDates=(x.OfficeConversions ?? []).Select(conversion => conversion.AttendanceDate).ToList(),
        EmployeeName = employee is null ? string.Empty : $"{employee.FirstName} {employee.LastName}".Trim(),
        Department = employee is not null && !string.IsNullOrWhiteSpace(employee.Department)
            ? (departmentMap != null && departmentMap.TryGetValue(employee.Department, out var resolvedName) && !string.IsNullOrWhiteSpace(resolvedName) && !string.Equals(resolvedName, "Not assigned", StringComparison.OrdinalIgnoreCase)
                ? resolvedName
                : (employee.Department.Contains('-') ? null : employee.Department))
            : null
    };

    internal static DateOnly ResolveBusinessDateOnly(DateTime date, string? timeZoneId = null)
    {
        var tz = string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Kolkata" : timeZoneId;
        var resolved = CompanyDate(DateTime.SpecifyKind(date, DateTimeKind.Utc), tz);
        return DateOnly.FromDateTime(resolved);
    }
    private static void Apply(WorkFromHomePolicy p, WorkFromHomePolicyDto d) { p.IsEnabled=d.IsEnabled;p.MaxDaysPerMonth=d.MaxDaysPerMonth;p.MaxDaysPerWeek=d.MaxDaysPerWeek;p.MaxConsecutiveDays=d.MaxConsecutiveDays;p.MinimumAdvanceNoticeHours=d.MinimumAdvanceNoticeHours;p.AllowBackdatedRequest=d.AllowBackdatedRequest;p.MaximumBackdatedDays=d.MaximumBackdatedDays;p.AllowHalfDay=d.AllowHalfDay;p.AllowMixedDay=d.AllowMixedDay;p.ManagerApprovalRequired=d.ManagerApprovalRequired;p.AllowManagerSelfApproval=d.AllowManagerSelfApproval;p.AllowOnWeeklyOff=d.AllowOnWeeklyOff;p.AllowOnHoliday=d.AllowOnHoliday;p.RequireReason=d.RequireReason;p.RequireAttachment=d.RequireAttachment;p.ApplyToAllEmployees=d.ApplyToAllEmployees;p.ApplicableDepartments=d.ApplicableDepartments.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();p.ApplicableEmployeeIds=d.ApplicableEmployeeIds.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.Ordinal).ToList();p.ExcludedEmployeeIds=d.ExcludedEmployeeIds.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.Ordinal).ToList();p.ApplicableEmploymentTypes=d.ApplicableEmploymentTypes.Distinct().ToList();p.EffectiveFrom=d.EffectiveFrom?.Date;p.EffectiveTo=d.EffectiveTo?.Date;p.AllowedCheckInFrom=d.AllowedCheckInFrom;p.AllowedCheckInUntil=d.AllowedCheckInUntil;p.AllowedCheckOutFrom=d.AllowedCheckOutFrom;p.AllowedCheckOutUntil=d.AllowedCheckOutUntil;p.OfficeStartTime=d.OfficeStartTime;p.OfficeEndTime=d.OfficeEndTime;p.TimeZoneId=string.IsNullOrWhiteSpace(d.TimeZoneId) ? "UTC" : d.TimeZoneId.Trim();p.FullDayMinimumHours=d.FullDayMinimumHours;p.HalfDayMinimumHours=d.HalfDayMinimumHours;p.FullDayAttendanceStatusCode=d.FullDayAttendanceStatusCode.Trim().ToUpperInvariant();p.HalfDayAttendanceStatusCode=d.HalfDayAttendanceStatusCode.Trim().ToUpperInvariant();p.MixedAttendanceStatusCode=d.MixedAttendanceStatusCode.Trim().ToUpperInvariant();p.WfhWfoAttendanceStatusCode=d.WfhWfoAttendanceStatusCode?.Trim().ToUpperInvariant();p.WfhHdAttendanceStatusCode=d.WfhHdAttendanceStatusCode?.Trim().ToUpperInvariant(); p.SameDayCutoffTime=d.SameDayCutoffTime; p.SameDayGraceMinutes=d.SameDayGraceMinutes; p.AllowLateApplicationWithApproval=d.AllowLateApplicationWithApproval; p.AllowedDaysOfWeek=d.AllowedDaysOfWeek?.Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? []; p.BlockedDaysOfWeek=d.BlockedDaysOfWeek?.Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? []; }
}
