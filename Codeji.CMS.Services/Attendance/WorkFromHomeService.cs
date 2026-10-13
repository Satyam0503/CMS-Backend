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
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.Company;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Net;
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
    Task<Result<WorkFromHomeResponseDto>> Create(WorkFromHomeRequestDto dto);
    Task<Result<WorkFromHomeResponseDto>> GetMy();
    Task<Result<WorkFromHomeResponseDto>> GetTeam(WorkFromHomeRequestFilterDto filter);
    Task<Result<WorkFromHomeResponseDto>> GetAll(WorkFromHomeRequestFilterDto filter);
    Task<Result> Decide(string requestId, string status, WorkFromHomeDecisionDto dto);
    Task<Result> Cancel(string requestId, WorkFromHomeCancelDto dto);
    Task<Result> CheckIn(string requestId);
    Task<Result> CheckOut(string requestId);
    Task<Result<WorkFromHomeTimingDto>> GetTiming(string requestId);
    Task<Result> CorrectInsufficientHours(string exceptionId, WorkFromHomeHoursCorrectionDto dto);
    Task<Result> ResolveAttendanceConflict(string exceptionId, WorkFromHomeConflictResolutionDto dto);
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
    IMongoDbRepository<WorkFromHomeRequestLog> logs,
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
    IMongoDbRepository<Notifications> notifications,
    IMongoDbRepository<UserNotifications> userNotifications,
    INotificationService notificationService,
    IPriorityTaskQueue priorityQueue,
    IMiddlewareService mail,
    IMongoClient mongoClient,
    ILeaveManagementService leaveManagement) : IWorkFromHomeService
{
    private string CompanyId => CurrentContext.CompanyId(context);
    private string UserId => CurrentContext.UserId(context);

    public async Task<Result<WorkFromHomePolicyDto>> GetPolicy()
    {
        var policy = await policies.FirstOrDefault(x => x.CompanyId == CompanyId);
        // Keep the initial UX stable: an HR/Admin can refresh, see the intended
        // one-day policy, and persist it. Creation still requires the SavePolicy action.
        return Ok(policy is null ? new WorkFromHomePolicyDto { IsEnabled = true, MaxDaysPerWeek = 1, MinimumAdvanceNoticeHours = 1 } : Map(policy));
    }

    public async Task<Result<EmployeeWfhContextDto>> GetEmployeeContext()
    {
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
        var used = WeeklyUsage(await requests.GetAll(x => x.CompanyId == CompanyId && x.UserId == UserId && (x.Status == "Pending" || x.Status == "Approved" || x.Status == "Returned") && x.FromDate < week.AddDays(7) && x.ToDate >= week), week);
        var weeklyLimit = WeeklyLimitFor(policy, employee);
        var remaining = Math.Max(0m, weeklyLimit - used);
        var reason = !eligible || weeklyLimit == 0 ? "WFH_POLICY_NOT_ASSIGNED" : remaining == 0 ? "WFH_WEEKLY_LIMIT_USED" : null;
        return Ok(new EmployeeWfhContextDto
        {
            IsFeatureEnabled = true, IsEligible = eligible && weeklyLimit > 0, WeeklyLimit = weeklyLimit, UsedThisWeek = used,
            RemainingThisWeek = remaining, CanSchedule = reason is null, DisabledReasonCode = reason,
            NextAvailableDate = remaining == 0 ? week.AddDays(7) : null, ManagerApprovalRequired = policy.ManagerApprovalRequired,
            MinimumAdvanceNoticeHours = policy.MinimumAdvanceNoticeHours, BusinessDate = today,
            IsSameDayRequestCutoffPassed = officeStart.HasValue && companyNow.TimeOfDay >= officeStart.Value,
            EffectiveOfficeStartTime = officeStart, OfficeStartTime = policy.OfficeStartTime,
            OfficeEndTime = policy.OfficeEndTime, CheckInAvailableFrom = policy.AllowedCheckInFrom,
            CheckInAvailableUntil = policy.AllowedCheckInUntil, CheckOutAvailableFrom = policy.AllowedCheckOutFrom,
            CheckOutAvailableUntil = policy.AllowedCheckOutUntil
        });
    }

    public async Task<Result> SavePolicy(WorkFromHomePolicyDto dto)
    {
        var configuredCodes = new[] { dto.FullDayAttendanceStatusCode, dto.HalfDayAttendanceStatusCode, dto.MixedAttendanceStatusCode, dto.WfhWfoAttendanceStatusCode, dto.WfhHdAttendanceStatusCode };
        var configuredStatuses = await Task.WhenAll(configuredCodes.Select(async code => string.IsNullOrWhiteSpace(code)
            ? null
            : (await statuses.GetAll(x => x.CompanyId == CompanyId && x.Code == code && x.IsActive)).FirstOrDefault()));
        if (configuredCodes.Any(string.IsNullOrWhiteSpace) || configuredStatuses.Any(status => status is null || !status.RequiresTime))
            return Fail("WFH_INVALID_ATTENDANCE_STATUS", "WFH status codes must be active company statuses that require check-in and check-out.");
        if (dto.MinimumAdvanceNoticeHours < 0 || dto.FullDayMinimumHours < 0 || dto.HalfDayMinimumHours < 0 || dto.MaxDaysPerWeek < 1 || (dto.EffectiveFrom.HasValue && dto.EffectiveTo.HasValue && dto.EffectiveFrom > dto.EffectiveTo))
            return Fail("WFH_INVALID_POLICY", "WFH policy values cannot be negative.");
        var policy = await policies.FirstOrDefault(x => x.CompanyId == CompanyId) ?? new WorkFromHomePolicy { CompanyId = CompanyId };
        Apply(policy, dto);
        return string.IsNullOrEmpty(policy.PolicyId)
            ? await policies.AddOne(policy)
            : await policies.Update(Builders<WorkFromHomePolicy>.Filter.Where(x => x.CompanyId == CompanyId && x.PolicyId == policy.PolicyId), policy);
    }

    public async Task<Result<IEnumerable<WorkFromHomeEmployeeAllocationDto>>> GetEmployeeAllocations()
    {
        var policy = await policies.FirstOrDefault(x => x.CompanyId == CompanyId);
        if (policy is null)
            return Ok<IEnumerable<WorkFromHomeEmployeeAllocationDto>>([]);

        var today = CompanyNow(policy).Date;
        var activeEmployees = await employees.GetAll(x => x.CompanyId == CompanyId && x.Status && !x.IsDeleted && (x.Lifecycle == null || x.Lifecycle.EmploymentStatus == null || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended));
        return Ok<IEnumerable<WorkFromHomeEmployeeAllocationDto>>(activeEmployees
            .Select(employee =>
            {
                var weeklyLimit = WeeklyLimitFor(policy, employee);
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
        var employeeId = dto.EmployeeId?.Trim();
        if (string.IsNullOrWhiteSpace(employeeId) || dto.WeeklyLimit is < 0 or > 31) return Fail("WFH_INVALID_EMPLOYEE_ALLOCATION", "Provide an active employee ID and a weekly WFH limit from 0 to 31.");
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
        policy.UpdatedDate = DateTime.UtcNow;
        return await policies.Update(Builders<WorkFromHomePolicy>.Filter.Where(x => x.CompanyId == CompanyId && x.PolicyId == policy.PolicyId), policy);
    }

    public async Task<Result<WorkFromHomeResponseDto>> Create(WorkFromHomeRequestDto dto)
    {
        var employee = await ActiveEmployee(UserId);
        var policy = await ActivePolicy();
        if (employee is null) return Fail<WorkFromHomeResponseDto>("WFH_EMPLOYEE_NOT_ELIGIBLE", "Employee is not active in this company.");
        if (policy is null) return Fail<WorkFromHomeResponseDto>("WFH_POLICY_DISABLED", "Work from home is not enabled for this company.");
        if (!IsLifecycleAvailableForWfh(employee, CompanyNow(policy).Date))
            return Fail<WorkFromHomeResponseDto>("WFH_LIFECYCLE_NOT_WORKING", "Work from home cannot be requested while the employee is on leave, sabbatical, suspension, or another absence state.");
        if (!IsEligible(policy, employee) || !IsEffectiveFor(policy, CompanyNow(policy).Date)) return Fail<WorkFromHomeResponseDto>("WFH_POLICY_NOT_ASSIGNED", "This WFH policy is not assigned to your current employee profile.");
        var validation = await ValidateRequest(employee, policy, dto);
        if (validation is not null) return Fail<WorkFromHomeResponseDto>(validation.Value.Code, validation.Value.Message);

        var approver = await ResolveApprover(employee, policy);
        if (policy.ManagerApprovalRequired && string.IsNullOrWhiteSpace(approver)) return Fail<WorkFromHomeResponseDto>("WFH_APPROVER_NOT_CONFIGURED", "No eligible approver is configured.");
        var request = new WorkFromHomeRequest
        {
            CompanyId = CompanyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, FromDate = BusinessDateUtc(dto.FromDate), ToDate = BusinessDateUtc(dto.ToDate),
            DurationType = dto.DurationType, ReasonCode = dto.ReasonCode.Trim(), ReasonText = Clean(dto.ReasonText), Status = policy.ManagerApprovalRequired ? "Pending" : "Approved", ApproverUserId = approver
        };
        var saved = await requests.AddOne(request);
        if (!saved.Success) return new Result<WorkFromHomeResponseDto> { Success = false, Message = saved.Message };
        if (!policy.ManagerApprovalRequired)
        {
            var reconcile = await ReconcileApproved(request, employee, policy, request.Version);
            if (!reconcile.Success)
            {
                await requests.Delete(Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.RequestId == request.RequestId && x.Status == "Approved" && x.Version == request.Version));
                return Fail<WorkFromHomeResponseDto>("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns one or more requested date(s). WFH was not applied.");
            }
        }
        await Log(request, null, request.Status, policy.ManagerApprovalRequired ? "Created" : "AutoApproved", null, null);
        QueueHrNotification(request, employee, policy.ManagerApprovalRequired ? "WFH request submitted" : "Employee working from home");
        return Ok(Map(request));
    }

    public async Task<Result<WorkFromHomeResponseDto>> GetMy()
    {
        var items = await requests.GetAll(x => x.CompanyId == CompanyId && x.UserId == UserId);
        return new Result<WorkFromHomeResponseDto> { Success = true, MethodResults = items.OrderByDescending(x => x.CreatedDate).Select(x => Map(x)).ToList(), TotalRecords = items.Count() };
    }

    public Task<Result<WorkFromHomeResponseDto>> GetTeam(WorkFromHomeRequestFilterDto filter) => GetPagedRequests(filter, Builders<WorkFromHomeRequest>.Filter.Eq(x => x.ApproverUserId, UserId));

    public Task<Result<WorkFromHomeResponseDto>> GetAll(WorkFromHomeRequestFilterDto filter) => GetPagedRequests(filter, Builders<WorkFromHomeRequest>.Filter.Empty);

    private async Task<Result<WorkFromHomeResponseDto>> GetPagedRequests(WorkFromHomeRequestFilterDto filter, FilterDefinition<WorkFromHomeRequest> scope)
    {
        var pageNo = Math.Max(1, filter.PageNo);
        var pageSize = filter.PageSize is 10 or 25 or 50 or 100 ? filter.PageSize : 25;
        var fromDate = filter.FromDate?.Date;
        var toDate = filter.ToDate?.Date;
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            return Fail<WorkFromHomeResponseDto>("WFH_INVALID_DATE_RANGE", "From date cannot be after to date.");
        var allowedStatuses = new[] { "Pending", "Approved", "Rejected", "Returned", "Cancelled" };
        var status = string.IsNullOrWhiteSpace(filter.Status) ? null : filter.Status.Trim();
        if (status is not null && !allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            return Fail<WorkFromHomeResponseDto>("WFH_INVALID_STATUS", "Unsupported WFH status filter.");
        var search = filter.EmployeeSearch?.Trim();
        var department = filter.Department?.Trim();
        var employeeRows = await employees.GetAll(x => x.CompanyId == CompanyId && x.Status && !x.IsDeleted && (x.Lifecycle == null || x.Lifecycle.EmploymentStatus == null || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended));
        var matchingEmployees = employeeRows.Where(employee =>
            (string.IsNullOrWhiteSpace(department) || string.Equals(employee.Department, department, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(search) ||
             employee.EmployeeId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             employee.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             (employee.LastName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToDictionary(employee => employee.UserId, StringComparer.Ordinal);
        if (matchingEmployees.Count == 0)
            return new Result<WorkFromHomeResponseDto> { Success = true, MethodResults = [], TotalRecords = 0 };
        var requestFilter = Builders<WorkFromHomeRequest>.Filter.And(
            Builders<WorkFromHomeRequest>.Filter.Eq(request => request.CompanyId, CompanyId),
            Builders<WorkFromHomeRequest>.Filter.In(request => request.UserId, matchingEmployees.Keys),
            scope);
        if (status != null) requestFilter &= Builders<WorkFromHomeRequest>.Filter.Eq(request => request.Status, status);
        if (fromDate.HasValue) requestFilter &= Builders<WorkFromHomeRequest>.Filter.Gte(request => request.ToDate, fromDate.Value);
        if (toDate.HasValue) requestFilter &= Builders<WorkFromHomeRequest>.Filter.Lte(request => request.FromDate, toDate.Value);
        var collection = requests.GetCollection();
        var total = await collection.CountDocumentsAsync(requestFilter);
        var items = await collection.Find(requestFilter).SortByDescending(request => request.CreatedDate)
            .Skip((pageNo - 1) * pageSize).Limit(pageSize).ToListAsync();
        return new Result<WorkFromHomeResponseDto>
        {
            Success = true,
            TotalRecords = (int)Math.Min(total, int.MaxValue),
            MethodResults = items.Select(request => Map(request, matchingEmployees[request.UserId])).ToList()
        };
    }

    public async Task<Result> Decide(string requestId, string status, WorkFromHomeDecisionDto dto)
    {
        if (status is not ("Approved" or "Rejected" or "Returned")) return Fail("WFH_INVALID_STATUS", "Unsupported WFH decision.");
        var request = await requests.FirstOrDefault(x => x.CompanyId == CompanyId && x.RequestId == requestId);
        if (request is null || request.Status != "Pending" || request.Version != dto.Version) return Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.");
        if (request.UserId == UserId) return Fail("WFH_SELF_APPROVAL_NOT_ALLOWED", "A manager cannot approve their own WFH request.");
        var isCompanyReviewer = await roles.VerifyUserAccess(AppModule.WorkFromHome, [Codeji.CMS.Utility.Constraints.Permission.ViewAll], UserId, CompanyId);
        if (!string.Equals(request.ApproverUserId, UserId, StringComparison.Ordinal) && !isCompanyReviewer) return Fail("WFH_APPROVER_NOT_AUTHORIZED", "You are not the assigned approver.");

        var previous = request.Status;
        request.Status = status; request.Version++; request.ReviewedByUserId = UserId; request.ReviewedAt = DateTime.UtcNow;
        request.ReviewRemarksCode = dto.RemarksCode; request.ReviewRemarksText = Clean(dto.RemarksText);
        var update = await requests.Update(Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.RequestId == requestId && x.Status == previous && x.Version == dto.Version), request);
        if (!update.Success) return Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.");

        if (status == "Approved")
        {
            var policy = await ActivePolicy();
            var employee = await ActiveEmployee(request.UserId);
            if (await HasActiveLeaveOverlap(request.UserId, request.FromDate, request.ToDate, request.DurationType))
            {
                request.Status = previous;
                request.Version++;
                request.ReviewedByUserId = null;
                request.ReviewedAt = null;
                request.ReviewRemarksCode = null;
                request.ReviewRemarksText = null;
                await requests.Update(Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.RequestId == requestId && x.Status == status && x.Version == request.Version - 1), request);
                return Fail("WFH_LEAVE_OVERLAP", "The Work From Home request overlaps an active leave request.");
            }
            var reconcile = policy is null
                ? Fail("WFH_POLICY_DISABLED", "WFH policy is disabled.")
                : employee is null
                    ? Fail("WFH_EMPLOYEE_NOT_ELIGIBLE", "Employee is no longer active.")
                    : await ReconcileApproved(request, employee, policy, request.Version);
            if (!reconcile.Success)
            {
                // The request is claimed before attendance is touched. If reconciliation
                // cannot proceed, restore Pending only when this exact approval is still
                // current; this prevents attendance from being generated for a failed
                // optimistic request update.
                request.Status = previous;
                request.Version++;
                request.ReviewedByUserId = null;
                request.ReviewedAt = null;
                request.ReviewRemarksCode = null;
                request.ReviewRemarksText = null;
                await requests.Update(Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.RequestId == requestId && x.Status == status && x.Version == request.Version - 1), request);
                return reconcile;
            }
        }

        await Log(request, previous, status, status, dto.RemarksCode, dto.RemarksText);
        if (status == "Approved") QueueEmployeeNotification(request, "Your WFH request was approved.");
        return update;
    }

    public async Task<Result> Cancel(string requestId, WorkFromHomeCancelDto dto)
    {
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
        if (request.FromDate.Date == companyNow.Date && (!officeStart.HasValue || companyNow.TimeOfDay >= officeStart.Value))
        {
            var label = officeStart.HasValue ? DateTime.Today.Add(officeStart.Value).ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture) : "the office start time";
            return Fail("WFH_CANNOT_CANCEL_AFTER_START", $"Today's Work From Home request can only be withdrawn before your office start time of {label}.");
        }

        if (request.Status == "Approved")
        {
            for (var day = request.FromDate.Date; day <= request.ToDate.Date; day = day.AddDays(1))
            {
                try { await attendanceEditGuard.EnsureEditableWorkingDayAsync(CompanyId, employee.UserId, day); }
                catch (InvalidOperationException ex) { return Fail("WFH_ATTENDANCE_NOT_EDITABLE", ex.Message); }
                if (request.DurationType is "FirstHalf" or "SecondHalf")
                {
                    var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : "SECOND_HALF";
                    var segment = await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1) && x.Segment == segmentName && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId);
                    if (segment?.CheckInTime is not null || segment?.CheckOutTime is not null) return Fail("WFH_CANNOT_CANCEL_AFTER_CLOCKING", "A clocked WFH attendance segment cannot be cancelled through the employee workflow.");
                    if (segment is not null && !(await attendanceSegments.Delete(Builders<AttendanceDaySegment>.Filter.Eq(x => x.AttendanceDaySegmentId, segment.AttendanceDaySegmentId))).Success)
                        return Fail("WFH_ATTENDANCE_RESTORE_FAILED", "The WFH attendance segment could not be removed.");
                }
                else
                {
                    var row = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.Date >= day && x.Date < day.AddDays(1));
                    if (row?.CheckInTime is not null || row?.CheckOutTime is not null) return Fail("WFH_CANNOT_CANCEL_AFTER_CLOCKING", "A clocked WFH attendance record cannot be cancelled through the employee workflow.");
                    if (row is not null && row.SourceType == "WFH_REQUEST" && row.SourceId == request.RequestId)
                        await attendance.Delete(Builders<AttendanceModel>.Filter.Eq(x => x.AttendanceId, row.AttendanceId));
                }
            }
            foreach (var month in Days(request.FromDate, request.ToDate).Select(day => new DateTime(day.Year, day.Month, 1)).Distinct())
                await attendanceInitialization.InitializeMonthAsync(CompanyId, UserId, month);
        }
        var previous = request.Status; request.Status = "Cancelled"; request.Version++; request.CancelledAt = DateTime.UtcNow; request.CancelledByUserId = UserId;
        var result = await requests.Update(Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.RequestId == requestId && x.Status == previous && x.Version == dto.Version), request);
        if (result.Success) await Log(request, previous, "Cancelled", "Cancelled", dto.RemarksCode, dto.RemarksText);
        if (result.Success) return result;

        // The request is still Approved in storage when its optimistic cancellation
        // loses the race. Restore its source-owned attendance before reporting the
        // conflict, so an approved WFH request is not left without attendance.
        request.Status = previous;
        request.Version = dto.Version;
        request.CancelledAt = null;
        request.CancelledByUserId = null;
        if (previous == "Approved" && policy is not null)
        {
            var restored = await ReconcileApproved(request, employee, policy, dto.Version);
            if (!restored.Success)
                return Fail("WFH_RECOVERY_REQUIRED", "The request changed while cancelling and its WFH attendance could not be restored automatically. Refresh and resolve it before payroll is locked.", StatusCodes.Status409Conflict);
        }
        return Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.", StatusCodes.Status409Conflict);
    }

    public async Task<Result> CheckIn(string requestId) => await RecordTime(requestId, true);
    public async Task<Result> CheckOut(string requestId) => await RecordTime(requestId, false);

    public async Task<Result<WorkFromHomeTimingDto>> GetTiming(string requestId)
    {
        var request = await requests.FirstOrDefault(x => x.CompanyId == CompanyId && x.RequestId == requestId && x.UserId == UserId && x.Status == "Approved");
        if (request is null) return Fail<WorkFromHomeTimingDto>("WFH_REQUEST_NOT_AVAILABLE", "No approved WFH request is available.");
        var policy = await ActivePolicy();
        var today = CompanyNow(policy ?? new WorkFromHomePolicy()).Date;
        if (today < request.FromDate.Date || today > request.ToDate.Date) return Ok(new WorkFromHomeTimingDto());
        var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : request.DurationType == "SecondHalf" ? "SECOND_HALF" : null;
        var row = segmentName is null
            ? await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == UserId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Date >= today && x.Date < today.AddDays(1))
            : null;
        var segment = segmentName is null ? null : await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == UserId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId && x.Segment == segmentName && x.Date >= today && x.Date < today.AddDays(1));
        return Ok(new WorkFromHomeTimingDto { IsApprovedForToday = true, CheckInTime = row?.CheckInTime ?? segment?.CheckInTime, CheckOutTime = row?.CheckOutTime ?? segment?.CheckOutTime, TotalHours = row?.TotalHours ?? segment?.TotalHours, RequiredHours = request.DurationType == "FullDay" ? policy?.FullDayMinimumHours ?? 8m : policy?.HalfDayMinimumHours ?? 4m, OfficeStartTime = policy?.OfficeStartTime, OfficeEndTime = policy?.OfficeEndTime, CheckInAvailableFrom = policy?.AllowedCheckInFrom, CheckInAvailableUntil = policy?.AllowedCheckInUntil, CheckOutAvailableFrom = policy?.AllowedCheckOutFrom, CheckOutAvailableUntil = policy?.AllowedCheckOutUntil });
    }

    public async Task<Result> CorrectInsufficientHours(string exceptionId, WorkFromHomeHoursCorrectionDto dto)
    {
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
            var originalCheckIn = segment.CheckInTime; var originalCheckOut = segment.CheckOutTime;
            var originalTotal = segment.TotalHours; var originalSegmentRemarks = segment.Remarks;
            segment.CheckInTime = dto.CheckInTime; segment.CheckOutTime = dto.CheckOutTime; segment.TotalHours = totalHours;
            segment.Remarks = $"HR/Admin corrected WFH hours: {Clean(dto.Remarks)}"; segment.UpdatedAtUtc = DateTime.UtcNow;
            exception.Status = "RESOLVED"; exception.Resolution = $"WFH hours corrected to {totalHours:0.##} hour(s) by HR/Admin.";
            exception.ReviewedBy = UserId; exception.ReviewedAt = DateTime.UtcNow; exception.UpdatedBy = UserId; exception.UpdatedDate = DateTime.UtcNow; exception.Version++;
            var segmentFilter = Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.AttendanceDaySegmentId == segment.AttendanceDaySegmentId && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId);
            var savedSegment = await attendanceSegments.Update(segmentFilter, segment);
            if (!savedSegment.Success) return Fail("WFH_ATTENDANCE_UPDATE_FAILED", "The half-day WFH attendance record could not be updated.");
            var resolvedException = await exceptions.Update(Builders<AttendancePayrollException>.Filter.Where(x => x.Id == exception.Id && x.CompanyId == CompanyId && x.Status == "PENDING_REVIEW" && x.Version == dto.ExceptionVersion), exception);
            if (!resolvedException.Success)
            {
                segment.CheckInTime = originalCheckIn; segment.CheckOutTime = originalCheckOut; segment.TotalHours = originalTotal; segment.Remarks = originalSegmentRemarks; segment.UpdatedAtUtc = DateTime.UtcNow;
                await attendanceSegments.Update(segmentFilter, segment);
                return Fail("WFH_EXCEPTION_CHANGED", "This exception was already changed. Refresh and try again.", StatusCodes.Status409Conflict);
            }
            await InvalidateSummary(employee, dateToCorrect);
            await Log(request, request.Status, request.Status, "HoursCorrected", "WFH_HOURS_CORRECTED", dto.Remarks);
            return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = $"WFH hours updated to {totalHours:0.##} hours and the exception was resolved." };
        }

        var originalCheckInTime = row.CheckInTime;
        var originalCheckOutTime = row.CheckOutTime;
        var originalTotalHours = row.TotalHours;
        var originalBreakMinutes = row.BreakMinutes;
        var originalRemarkCode = row.RemarkCode;
        var originalRemarks = row.Remarks;
        row.CheckInTime = dto.CheckInTime;
        row.CheckOutTime = dto.CheckOutTime;
        row.TotalHours = totalHours;
        row.BreakMinutes = breakMinutes;
        row.RemarkCode = "WFH_HOURS_CORRECTED";
        row.Remarks = $"HR/Admin corrected WFH hours: {Clean(dto.Remarks)}";
        exception.Status = "RESOLVED";
        exception.Resolution = $"WFH hours corrected to {totalHours:0.##} hour(s) by HR/Admin.";
        exception.ReviewedBy = UserId;
        exception.ReviewedAt = DateTime.UtcNow;
        exception.UpdatedBy = UserId;
        exception.UpdatedDate = DateTime.UtcNow;
        exception.Version++;
        try
        {
            using var session = await mongoClient.StartSessionAsync();
            await session.WithTransactionAsync(async (_, cancellationToken) =>
            {
                var attendanceWrite = await attendance.GetCollection().ReplaceOneAsync(session,
                    Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.AttendanceId == row.AttendanceId && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId), row, cancellationToken: cancellationToken);
                if (attendanceWrite.MatchedCount != 1) throw new InvalidOperationException("WFH_ATTENDANCE_UPDATE_FAILED");
                var exceptionWrite = await exceptions.GetCollection().ReplaceOneAsync(session,
                    Builders<AttendancePayrollException>.Filter.Where(x => x.Id == exception.Id && x.CompanyId == CompanyId && x.Status == "PENDING_REVIEW" && x.Version == dto.ExceptionVersion), exception, cancellationToken: cancellationToken);
                if (exceptionWrite.MatchedCount != 1) throw new InvalidOperationException("WFH_EXCEPTION_CHANGED");
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
        {
            // Standalone MongoDB deployments do not support transactions. Keep the
            // same compare-and-set guards and compensate the attendance write if
            // the exception update loses its optimistic-concurrency race.
            var attendanceFilter = Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.AttendanceId == row.AttendanceId && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId);
            var saved = await attendance.Update(attendanceFilter, row);
            if (!saved.Success) return Fail("WFH_ATTENDANCE_UPDATE_FAILED", "The WFH attendance record could not be updated.");
            var resolved = await exceptions.Update(Builders<AttendancePayrollException>.Filter.Where(x => x.Id == exception.Id && x.CompanyId == CompanyId && x.Status == "PENDING_REVIEW" && x.Version == dto.ExceptionVersion), exception);
            if (!resolved.Success)
            {
                row.CheckInTime = originalCheckInTime; row.CheckOutTime = originalCheckOutTime; row.TotalHours = originalTotalHours; row.BreakMinutes = originalBreakMinutes; row.RemarkCode = originalRemarkCode; row.Remarks = originalRemarks;
                await attendance.Update(attendanceFilter, row);
                return Fail("WFH_EXCEPTION_CHANGED", "This exception was already changed. Refresh and try again.", StatusCodes.Status409Conflict);
            }
        }
        await InvalidateSummary(employee, dateToCorrect);
        await Log(request, request.Status, request.Status, "HoursCorrected", "WFH_HOURS_CORRECTED", dto.Remarks);
        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = $"WFH hours updated to {totalHours:0.##} hours and the exception was resolved." };
    }

    public async Task<Result> ResolveAttendanceConflict(string exceptionId, WorkFromHomeConflictResolutionDto dto)
    {
        var exception = await exceptions.FirstOrDefault(x => x.Id == exceptionId && x.CompanyId == CompanyId);
        if (exception is null || exception.ExceptionType is not ("WFH_LEAVE_CONFLICT" or "WFH_ATTENDANCE_CONFLICT" or "WFH_ATTENDANCE_SEGMENT_CONFLICT")) return Fail("WFH_EXCEPTION_NOT_FOUND", "No resolvable WFH conflict was found.", StatusCodes.Status404NotFound);
        if (exception.Status != "PENDING_REVIEW" || exception.Version != dto.ExceptionVersion) return Fail("WFH_EXCEPTION_CHANGED", "This exception was already changed. Refresh and try again.", StatusCodes.Status409Conflict);
        var wfh = await requests.FirstOrDefault(x => x.CompanyId == CompanyId && x.RequestId == exception.LeaveRequestId && x.UserId == exception.UserId);
        if (wfh is null) return Fail("WFH_REQUEST_NOT_AVAILABLE", "The linked WFH request is no longer available.");

        if (dto.Resolution == "KEEP_LEAVE_CANCEL_WFH")
        {
            await attendance.DeleteAll(Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == wfh.UserId && x.SourceType == "WFH_REQUEST" && x.SourceId == wfh.RequestId));
            await attendanceSegments.DeleteAll(Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == wfh.UserId && x.SourceType == "WFH_REQUEST" && x.SourceId == wfh.RequestId));
            var prior = wfh.Status; wfh.Status = "Cancelled"; wfh.CancelledAt = DateTime.UtcNow; wfh.CancelledByUserId = UserId; wfh.Version++;
            var cancelled = await requests.Update(Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == CompanyId && x.RequestId == wfh.RequestId && x.Status == prior && x.Version == wfh.Version - 1), wfh);
            if (!cancelled.Success) return Fail("WFH_REQUEST_CHANGED", "The WFH request changed before it could be cancelled.", StatusCodes.Status409Conflict);
            await Log(wfh, prior, "Cancelled", "AttendanceConflictResolved", "KEEP_LEAVE_CANCEL_WFH", dto.Remarks);
        }
        else if (dto.Resolution == "KEEP_WFH_CANCEL_LEAVE")
        {
            if (!exception.AttendanceDate.HasValue) return Fail("WFH_EXCEPTION_DATE_MISSING", "The conflict does not have an attendance date.");
            var conflictDate = exception.AttendanceDate.Value.Date;
            var linkedLeave = (await leaveRequests.GetAll(x => x.CompanyId == CompanyId && x.EmployeeId == exception.UserId && (x.Status == EnumsHelper.LeaveRequestStatus.Pending || x.Status == EnumsHelper.LeaveRequestStatus.Accepted) && x.StartDate <= conflictDate && x.EndDate >= conflictDate)).FirstOrDefault();
            if (linkedLeave is null) return Fail("LEAVE_REQUEST_NOT_AVAILABLE", "No active leave request is available for this conflict.");
            var leaveResult = await leaveManagement.UpdateLeaveRequestStatus(linkedLeave.LeaveRequestId, new LeaveRequestUpdateDto { Status = EnumsHelper.LeaveRequestStatus.WithDrawn, Comment = $"HR/Admin resolved WFH conflict: {Clean(dto.Remarks)}", ExpectedVersion = linkedLeave.Version });
            if (!leaveResult.Success) return leaveResult;
            var employee = await ActiveEmployee(wfh.UserId); var policy = await ActivePolicy();
            if (employee is null || policy is null) return Fail("WFH_RECONCILIATION_FAILED", "The WFH request could not be re-applied after leave cancellation.");
            var reconciled = await ReconcileApproved(wfh, employee, policy, wfh.Version);
            if (!reconciled.Success) return reconciled;
            await Log(wfh, wfh.Status, wfh.Status, "AttendanceConflictResolved", "KEEP_WFH_CANCEL_LEAVE", dto.Remarks);
        }
        else return Fail("WFH_CONFLICT_RESOLUTION_INVALID", "Select whether to keep the existing leave or the WFH request.");

        exception.Status = "RESOLVED"; exception.Resolution = $"HR/Admin resolution: {dto.Resolution}. {Clean(dto.Remarks)}"; exception.ReviewedBy = UserId; exception.ReviewedAt = DateTime.UtcNow; exception.UpdatedBy = UserId; exception.UpdatedDate = DateTime.UtcNow; exception.Version++;
        var saved = await exceptions.Update(Builders<AttendancePayrollException>.Filter.Where(x => x.Id == exception.Id && x.CompanyId == CompanyId && x.Status == "PENDING_REVIEW" && x.Version == dto.ExceptionVersion), exception);
        return saved.Success ? new Result { Success = true, Message = "Attendance conflict resolved." } : Fail("WFH_EXCEPTION_CHANGED", "The exception changed before it could be resolved.", StatusCodes.Status409Conflict);
    }

    private async Task<Result> RecordTime(string requestId, bool checkIn)
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
        if ((row is null || row.SourceType != "WFH_REQUEST" || row.SourceId != requestId) && (segment is null || segment.SourceType != "WFH_REQUEST" || segment.SourceId != requestId)) return Fail("WFH_ATTENDANCE_CONFLICT", "No WFH-owned attendance record is available for timing.");
        var checkInTime = row?.CheckInTime ?? segment?.CheckInTime;
        var checkOutTime = row?.CheckOutTime ?? segment?.CheckOutTime;
        if (checkIn)
        {
            if (checkInTime.HasValue) return Fail("WFH_DUPLICATE_CHECK_IN", "Check-in was already recorded.");
            if (row is not null) { row.CheckInTime = now; row.Remarks = $"Checked in through employee WFH portal ({requestId})"; }
            else { segment!.CheckInTime = now; segment.Remarks = $"Checked in through employee WFH portal ({requestId})"; segment.UpdatedAtUtc = now; }
        }
        else
        {
            if (!checkInTime.HasValue) return Fail("WFH_CHECKOUT_WITHOUT_CHECKIN", "Check-in is required before check-out.");
            if (checkOutTime.HasValue) return Fail("WFH_DUPLICATE_CHECK_OUT", "Check-out was already recorded.");
            var requiredMinutes = request.DurationType == "FullDay" ? schedule?.RequiredWorkingMinutes ?? (int)(policy.FullDayMinimumHours * 60) : Math.Max(1, (schedule?.RequiredWorkingMinutes ?? (int)(policy.HalfDayMinimumHours * 60)) / 2);
            var workedMinutes = Math.Max(0, (int)(now - checkInTime.Value).TotalMinutes - (schedule?.BreakMinutes ?? 0));
            // A late but approved WFH check-in can legitimately require work past
            // the normal office punch-out cutoff. The required verified hours are
            // the authoritative checkout guard for WFH, rather than a cutoff that
            // would make a late start impossible to complete.
            if (workedMinutes < requiredMinutes) return Fail("WFH_CHECK_OUT_TOO_EARLY", $"Check-out is available after {requiredMinutes} verified working minutes. {Math.Max(0, requiredMinutes - workedMinutes)} minute(s) remain.");
            var verifiedHours = Math.Round(workedMinutes / 60m, 2, MidpointRounding.AwayFromZero);
            if (row is not null) { row.CheckOutTime = now; row.TotalHours = verifiedHours; row.Remarks = $"Checked out through employee WFH portal ({requestId})"; }
            else { segment!.CheckOutTime = now; segment.TotalHours = verifiedHours; segment.Remarks = $"Checked out through employee WFH portal ({requestId})"; segment.UpdatedAtUtc = now; }
        }
        var result = row is not null
            ? await attendance.Update(Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == UserId && x.AttendanceId == row.AttendanceId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId), row)
            : await attendanceSegments.Update(Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == CompanyId && x.UserId == UserId && x.AttendanceDaySegmentId == segment!.AttendanceDaySegmentId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId), segment!);
        if (result.Success) await Log(request, request.Status, request.Status, checkIn ? "CheckIn" : "CheckOut", null, null);
        return result;
    }

    public async Task<Result<IEnumerable<AttendanceRemarkOptionDto>>> GetRemarkOptions()
    {
        var items = await remarkOptions.GetAll(x => x.CompanyId == CompanyId && x.IsActive);
        return new Result<IEnumerable<AttendanceRemarkOptionDto>> { Success = true, MethodResult = items.OrderBy(x => x.DisplayOrder).Select(x => new AttendanceRemarkOptionDto { RemarkOptionId = x.RemarkOptionId, Code = x.Code, DisplayName = x.DisplayName, Category = x.Category, ApplicableStatusCodes = x.ApplicableStatusCodes, RequiresAdditionalText = x.RequiresAdditionalText, IsActive = x.IsActive, DisplayOrder = x.DisplayOrder }).ToList() };
    }

    public async Task<Result> SaveRemarkOption(AttendanceRemarkOptionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.DisplayName)) return Fail("ATTENDANCE_REMARK_INVALID", "Code and display name are required.");
        if (dto.ApplicableStatusCodes.Any(x => string.IsNullOrWhiteSpace(x) || !statuses.Exist(s => s.CompanyId == CompanyId && s.Code == x && s.IsActive).GetAwaiter().GetResult())) return Fail("ATTENDANCE_REMARK_STATUS_INVALID", "One or more applicable statuses are inactive.");
        var option = string.IsNullOrWhiteSpace(dto.RemarkOptionId) ? null : await remarkOptions.FirstOrDefault(x => x.CompanyId == CompanyId && x.RemarkOptionId == dto.RemarkOptionId);
        option ??= new AttendanceRemarkOption { CompanyId = CompanyId }; option.Code = dto.Code.Trim().ToUpperInvariant(); option.DisplayName = Clean(dto.DisplayName)!; option.Category = Clean(dto.Category)!; option.ApplicableStatusCodes = dto.ApplicableStatusCodes.Select(x => x.Trim().ToUpperInvariant()).Distinct().ToList(); option.RequiresAdditionalText = dto.RequiresAdditionalText; option.IsActive = dto.IsActive; option.DisplayOrder = dto.DisplayOrder;
        return string.IsNullOrEmpty(option.RemarkOptionId) ? await remarkOptions.AddOne(option) : await remarkOptions.Update(Builders<AttendanceRemarkOption>.Filter.Where(x => x.CompanyId == CompanyId && x.RemarkOptionId == option.RemarkOptionId), option);
    }

    private async Task<Result> ReconcileApproved(WorkFromHomeRequest request, EmpUser employee, WorkFromHomePolicy policy, int sourceVersion)
    {
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
        // Validate every date and ownership conflict before changing any attendance
        // row. An approval for several days must never leave the first day written
        // when a later date is locked, on leave, or otherwise unavailable.
        foreach (var date in requestDates)
        {
            var week = StartOfWeek(date);
            var alreadyApproved = WeeklyUsage(await requests.GetAll(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.RequestId != request.RequestId && x.Status == "Approved" && x.FromDate < week.AddDays(7) && x.ToDate >= week), week);
            if (alreadyApproved >= WeeklyLimitFor(policy, employee)) return Fail("WFH_WEEKLY_QUOTA_EXCEEDED", $"Only {WeeklyLimitFor(policy, employee)} WFH day(s) are allowed per week.");
            var guard = await ValidateWorkingDay(employee, policy, date); if (guard is not null) return Fail(guard.Value.Code, guard.Value.Message);
            if (request.DurationType is "FirstHalf" or "SecondHalf")
            {
                var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : "SECOND_HALF";
                var existingSegment = await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1) && x.Segment == segmentName);
                if (existingSegment is not null && !(existingSegment.SourceType == "WFH_REQUEST" && existingSegment.SourceId == request.RequestId)) return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
            }
            else
            {
                var existingSegments = await attendanceSegments.GetAll(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1));
                if (existingSegments.Any()) return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
                var row = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1));
                if (row is not null && !(row.SourceType == "WFH_REQUEST" && row.SourceId == request.RequestId) && !AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(row.SourceType)) return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
            }
        }
        for (var date = BusinessDateUtc(request.FromDate); date <= BusinessDateUtc(request.ToDate); date = date.AddDays(1))
        {
            var week = StartOfWeek(date);
            var alreadyApproved = WeeklyUsage(await requests.GetAll(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.RequestId != request.RequestId && x.Status == "Approved" && x.FromDate < week.AddDays(7) && x.ToDate >= week), week);
            var weeklyLimit = WeeklyLimitFor(policy, employee);
            if (alreadyApproved >= weeklyLimit)
                return Fail("WFH_WEEKLY_QUOTA_EXCEEDED", $"Only {weeklyLimit} WFH day(s) are allowed per week.");
            var guard = await ValidateWorkingDay(employee, policy, date); if (guard is not null) return Fail(guard.Value.Code, guard.Value.Message);
            if (request.DurationType is "FirstHalf" or "SecondHalf")
            {
                var segmentName = request.DurationType == "FirstHalf" ? "FIRST_HALF" : "SECOND_HALF";
                var existingSegment = await attendanceSegments.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1) && x.Segment == segmentName);
                if (existingSegment is not null && !(existingSegment.SourceType == "WFH_REQUEST" && existingSegment.SourceId == request.RequestId))
                {
                    return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
                }
                var segment = existingSegment ?? new AttendanceDaySegment { CompanyId = CompanyId, UserId = request.UserId, EmployeeId = employee.EmployeeId, Date = date, Segment = segmentName };
                segment.Status = code.Trim().ToUpperInvariant(); segment.SourceType = "WFH_REQUEST"; segment.SourceId = request.RequestId; segment.SourceVersion = sourceVersion; segment.Remarks = $"Generated from approved half-day WFH request {request.RequestId}"; segment.UpdatedAtUtc = DateTime.UtcNow;
                var segmentSave = string.IsNullOrWhiteSpace(segment.AttendanceDaySegmentId) ? await attendanceSegments.AddOne(segment) : await attendanceSegments.Update(Builders<AttendanceDaySegment>.Filter.Eq(x => x.AttendanceDaySegmentId, segment.AttendanceDaySegmentId), segment);
                if (!segmentSave.Success) return segmentSave;
                await InvalidateSummary(employee, date);
                await Log(request, request.Status, request.Status, "AttendanceSegmentGenerated", "WFH_APPROVED", null);
                continue;
            }
            var existingSegments = await attendanceSegments.GetAll(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1));
            if (existingSegments.Any())
            {
                return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
            }
            var row = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1));
            if (row is not null && !(row.SourceType == "WFH_REQUEST" && row.SourceId == request.RequestId) && !AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(row.SourceType))
            {
                return Fail("WFH_DATE_ALREADY_OWNED", "A leave or attendance record already owns this date. WFH cannot be applied.");
            }
            row ??= new AttendanceModel { CompanyId = CompanyId, UserId = request.UserId, EmployeeId = employee.EmployeeId, Date = date };
            row.Status = code.Trim().ToUpperInvariant(); row.SourceType = "WFH_REQUEST"; row.SourceId = request.RequestId; row.SourceVersion = sourceVersion; row.RemarkCode = "WFH_APPROVED"; row.Remarks = $"Generated from approved WFH request {request.RequestId}";
            var save = string.IsNullOrWhiteSpace(row.AttendanceId) ? await attendance.AddOne(row) : await attendance.Update(Builders<AttendanceModel>.Filter.Eq(x => x.AttendanceId, row.AttendanceId), row);
            if (!save.Success) return save;
            await InvalidateSummary(employee, date);
            await Log(request, request.Status, request.Status, "AttendanceGenerated", "WFH_APPROVED", null);
        }
        return new Result { Success = true, Message = "OK" };
    }

    private async Task<(string Code, string Message)?> ValidateRequest(EmpUser employee, WorkFromHomePolicy policy, WorkFromHomeRequestDto dto)
    {
        var companyNow = CompanyNow(policy);
        if (dto.FromDate.Date > dto.ToDate.Date) return ("WFH_INVALID_DATE_RANGE", "End date cannot be before start date.");
        if (dto.ToDate.Date < companyNow.Date && !policy.AllowBackdatedRequest) return ("WFH_BACKDATED_NOT_ALLOWED", "Backdated WFH requests are not allowed.");
        if (policy.MaximumBackdatedDays is int max && dto.FromDate.Date < companyNow.Date.AddDays(-max)) return ("WFH_BACKDATED_LIMIT", "The backdated request exceeds the policy limit.");
        if (policy.MinimumAdvanceNoticeHours > 0 && dto.FromDate.ToUniversalTime() < companyNow.ToUniversalTime().AddHours(policy.MinimumAdvanceNoticeHours)) return ("WFH_ADVANCE_NOTICE_REQUIRED", "The required advance notice was not met.");
        if (policy.RequireReason && string.IsNullOrWhiteSpace(dto.ReasonText)) return ("WFH_REASON_REQUIRED", "A reason is required.");
        // WFH+SL and WFH+CL used to be accepted as labels only: they did not
        // create or debit a real leave request. Keep the API truthful by allowing
        // only durations that have an unambiguous attendance ownership model.
        if (dto.DurationType is not ("FullDay" or "FirstHalf" or "SecondHalf" or "Mixed")) return ("WFH_INVALID_DURATION", "Invalid WFH duration. Submit a real half-day leave for the other half of the day.");
        if (dto.DurationType is "FirstHalf" or "SecondHalf" && !policy.AllowHalfDay) return ("WFH_HALF_DAY_NOT_ALLOWED", "Half day WFH is not allowed.");
        if (dto.DurationType == "Mixed" && !policy.AllowMixedDay) return ("WFH_MIXED_DAY_NOT_ALLOWED", "Mixed WFH/WFO is not allowed.");
        if (!IsEffectiveFor(policy, dto.FromDate.Date) || !IsEffectiveFor(policy, dto.ToDate.Date)) return ("WFH_POLICY_NOT_EFFECTIVE", "The selected date is outside the effective WFH policy period.");
        if (await requests.Exist(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && (x.Status == "Pending" || x.Status == "Approved" || x.Status == "Returned") && dto.FromDate.Date <= x.ToDate.Date && dto.ToDate.Date >= x.FromDate.Date)) return ("WFH_OVERLAPPING_REQUEST", "The request overlaps an existing WFH request.");
        if (await HasActiveLeaveOverlap(employee.UserId, dto.FromDate.Date, dto.ToDate.Date, dto.DurationType)) return ("WFH_LEAVE_OVERLAP", "The request overlaps leave for the same half or a full day.");
        foreach (var week in Days(dto.FromDate, dto.ToDate).Select(StartOfWeek).Distinct())
        {
            var usedDates = WeeklyUsage(await requests.GetAll(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && (x.Status == "Pending" || x.Status == "Approved" || x.Status == "Returned") && x.FromDate < week.AddDays(7) && x.ToDate >= week), week);
            var requestedDates = WeeklyUsage(dto.FromDate, dto.ToDate, dto.DurationType, week);
            var weeklyLimit = WeeklyLimitFor(policy, employee);
            if (usedDates + requestedDates > weeklyLimit) return ("WFH_WEEKLY_QUOTA_EXCEEDED", $"Only {weeklyLimit} WFH day(s) are allowed per week.");
        }
        foreach (var day in Days(dto.FromDate, dto.ToDate)) { var check = await ValidateWorkingDay(employee, policy, day); if (check is not null) return check; }
        var schedule = await effectiveSchedules.ResolveAsync(CompanyId, employee, dto.FromDate);
        companyNow = CompanyNow(schedule?.TimeZoneId ?? policy.TimeZoneId);
        var officeStart = schedule?.StartTime ?? policy.OfficeStartTime;
        if (Days(dto.FromDate, dto.ToDate).Any(day => IsSameDayWfhStartCutoffReached(day, companyNow, officeStart)))
        {
            var startLabel = DateTime.Today.Add(officeStart!.Value).ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
            return ("WFH_SAME_DAY_OFFICE_STARTED", $"Work From Home for today must be requested before your office start time ({startLabel}).");
        }
        return null;
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
    private static bool LeaveConflictsWithWfh(LeaveRequest leave, string wfhDurationType)
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
        if (!policy.ManagerApprovalRequired) return string.Empty;
        var manager = await ActiveEmployee(employee.ReportingManager);
        if (manager is not null && (policy.AllowManagerSelfApproval || manager.UserId != employee.UserId)) return manager.UserId;
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

    private static TimeSpan? HalfDayBoundary(TimeSpan? officeStartTime, int? requiredWorkingMinutes, decimal policyFullDayHours)
    {
        if (!officeStartTime.HasValue) return null;
        var fullDayMinutes = requiredWorkingMinutes.GetValueOrDefault();
        if (fullDayMinutes <= 0) fullDayMinutes = (int)Math.Round(policyFullDayHours * 60m, MidpointRounding.AwayFromZero);
        return fullDayMinutes > 0 ? officeStartTime.Value.Add(TimeSpan.FromMinutes(fullDayMinutes / 2d)) : null;
    }

    private async Task<WorkFromHomePolicy?> ActivePolicy() => await policies.FirstOrDefault(x => x.CompanyId == CompanyId && x.IsEnabled);
    private async Task<bool> ValidCode(string code) => !string.IsNullOrWhiteSpace(code) && await statuses.Exist(x => x.CompanyId == CompanyId && x.Code == code.Trim().ToUpperInvariant() && x.IsActive);
    private async Task InvalidateSummary(EmpUser employee, DateTime day) => await summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == CompanyId && x.EmployeeId == employee.EmployeeId && x.PayrollMonth == new DateTime(day.Year, day.Month, 1) && !x.IsLocked));
    private void QueueHrNotification(WorkFromHomeRequest request, EmpUser employee, string subject)
    {
        // The queued job has no HTTP context, so capture tenant and actor data now.
        var companyId = CompanyId;
        var submittedByUserId = request.UserId;
        priorityQueue.QueueBackgroundWorkItem(async _ =>
        {
            var roleIds = (await companyRoles.GetAll(x => x.CompanyId == companyId && !x.IsDeleted && (x.RoleType == (int)EnumsHelper.Roles.Administrator || x.RoleType == (int)EnumsHelper.Roles.HR || x.RoleType == (int)EnumsHelper.Roles.HRExecutive), withDefaultFilter: false)).Select(x => x.RolesId).ToHashSet();
            var recipients = (await employees.GetAll(x => x.CompanyId == companyId && x.Status && roleIds.Contains(x.RoleId), withDefaultFilter: false)).GroupBy(x => x.UserId, StringComparer.Ordinal).Select(x => x.First());
            var department = string.IsNullOrWhiteSpace(employee.Department)
                ? "Not assigned"
                : DisplayName((await departments.FirstOrDefault(x => x.CompanyId == companyId && x.DepartmentId == employee.Department && !x.IsDeleted))?.Titles);
            var jobTitle = string.IsNullOrWhiteSpace(employee.JobRole)
                ? "Not assigned"
                : DisplayName((await jobTitles.FirstOrDefault(x => x.CompanyId == companyId && x.JobTitleId == employee.JobRole && !x.IsDeleted))?.Titles);
            var body = WfhRequestEmail(subject, employee, request, department, jobTitle);
            var recipientList = recipients.ToList();
            await SaveAndPushNotification(companyId, submittedByUserId, recipientList.Select(x => x.UserId), subject, $"{employee.FirstName} {employee.LastName} ({employee.EmployeeId}) - {request.FromDate:dd MMM}", request.RequestId, EnumsHelper.NotificationTypes.WorkFromHomeRequest);
            foreach (var recipient in recipientList.Where(x => x.IsEmailVerified && !string.IsNullOrWhiteSpace(x.Email))) await mail.EmailSendAndSave(new EmpEmailLogs { CompanyId = companyId, UserTo = recipient.UserId, UserFrom = submittedByUserId, Email = recipient.Email, Subject = subject, Body = body, EmailLogType = EnumsHelper.MailType.LeaveMailToHR });
        }, 1);
    }
    private void QueueEmployeeNotification(WorkFromHomeRequest request, string subject)
    {
        var companyId = CompanyId;
        var actorUserId = UserId;
        priorityQueue.QueueBackgroundWorkItem(async _ =>
        {
            var employee = (await employees.GetAll(x => x.CompanyId == companyId && x.UserId == request.UserId && x.Status && x.IsEmailVerified, withDefaultFilter: false)).FirstOrDefault();
            if (employee is null || string.IsNullOrWhiteSpace(employee.Email)) return;
            await SaveAndPushNotification(companyId, actorUserId, [employee.UserId], subject, $"WFH dates: {request.FromDate:dd MMM yyyy} - {request.ToDate:dd MMM yyyy}", request.RequestId, EnumsHelper.NotificationTypes.WorkFromHomeApproved);
            await mail.EmailSendAndSave(new EmpEmailLogs { CompanyId = companyId, UserTo = employee.UserId, UserFrom = actorUserId, Email = employee.Email, Subject = subject, Body = WfhStatusEmail(subject, employee, request), EmailLogType = EnumsHelper.MailType.LeaveReplyMail });
        }, 1);
    }
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
    private static string RequestEmailLayout(string title, string introduction, IEnumerable<(string Label, string Value)> details, string status)
    {
        var rows = string.Concat(details.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => $"<tr><td style=\"width:36%;padding:10px 14px;color:#64748b;font-weight:600;border-bottom:1px solid #e7edf4;vertical-align:top\">{WebUtility.HtmlEncode(x.Label)}</td><td style=\"padding:10px 0;border-bottom:1px solid #e7edf4;color:#1e293b;vertical-align:top\">{WebUtility.HtmlEncode(x.Value)}</td></tr>"));
        return $"<div style=\"font-family:Arial,Helvetica,sans-serif;color:#1e293b\"><h1 style=\"font-size:22px;line-height:1.3;margin:0 0 12px;color:#172554\">{WebUtility.HtmlEncode(title)}</h1><p style=\"margin:0 0 20px;line-height:1.6\">{introduction}</p><div style=\"display:inline-block;padding:7px 12px;margin:0 0 18px;background:#eef2ff;border-radius:999px;color:#4338ca;font-size:13px;font-weight:700\">{WebUtility.HtmlEncode(status)}</div><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%;border-collapse:collapse;border-top:1px solid #e7edf4\">{rows}</table><p style=\"margin:20px 0 0;color:#64748b;font-size:13px;line-height:1.5\">Please review the request in Codeji CMS to take the next action.</p></div>";
    }
    private async Task SaveAndPushNotification(string companyId, string senderUserId, IEnumerable<string> targetUserIds, string title, string body, string requestId, EnumsHelper.NotificationTypes type)
    {
        var targets = targetUserIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList();
        if (targets.Count == 0) return;
        var notification = new Notifications { NotificationId = Guid.NewGuid().ToString(), CompanyId = companyId, CreatedBy = senderUserId, CreatedDateTime = DateTime.UtcNow, TargetId = requestId, Title = title, Body = body, NotificationType = type };
        if (!(await notifications.AddOne(notification)).Success) return;
        var userItems = targets.Select(userId => new UserNotifications { UserNotificationId = Guid.NewGuid().ToString(), UserId = userId, NotificationId = notification.NotificationId, IsRead = false, CreatedDateTime = DateTime.UtcNow }).ToList();
        await userNotifications.AddMany(userItems);
        await Task.WhenAll(userItems.Select(item => notificationService.SendNotificationToUser(item.UserId, new NotificationViewModel { UserNotificationId = item.UserNotificationId, Title = title, Body = body, TargetId = requestId, SentDateTime = DateTime.UtcNow, SentBy = senderUserId, NotificationTypes = type }))); 
    }
    private async Task Log(WorkFromHomeRequest r, string? previous, string current, string action, string? code, string? text) => await logs.AddOne(new WorkFromHomeRequestLog { CompanyId = CompanyId, RequestId = r.RequestId, PreviousStatus = previous, NewStatus = current, Action = action, PerformedByUserId = UserId, PerformedAt = DateTime.UtcNow, RemarksCode = code, RemarksText = Clean(text), Version = r.Version });
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
    private static int WeeklyLimitFor(WorkFromHomePolicy policy, EmpUser employee) => (policy.EmployeeAllocations ?? []).FirstOrDefault(x => string.Equals(x.EmployeeId, employee.EmployeeId, StringComparison.Ordinal))?.WeeklyLimit ?? policy.MaxDaysPerWeek;
    // Pending and returned requests reserve capacity, matching the existing
    // submission rule. Rejected and cancelled records remain historical only.
    private static bool CountsTowardsWeeklyAllowance(string status) => status is "Pending" or "Approved" or "Returned";
    private static decimal WeeklyUsage(IEnumerable<WorkFromHomeRequest> requests, DateTime week) => requests.Sum(request => WeeklyUsage(request.FromDate, request.ToDate, request.DurationType, week));
    private static decimal WeeklyUsage(DateTime fromDate, DateTime toDate, string durationType, DateTime week)
    {
        var unitsPerDay = durationType is "FirstHalf" or "SecondHalf" ? .5m : 1m;
        return Days(fromDate, toDate).Count(day => day >= week && day < week.AddDays(7)) * unitsPerDay;
    }
    internal static decimal WeeklyUsageForPeriod(DateTime fromDate, DateTime toDate, string durationType, DateTime week) => WeeklyUsage(fromDate, toDate, durationType, week);
    internal static bool IsSameDayWfhStartCutoffReached(DateTime requestedDate, DateTime companyNow, TimeSpan? officeStartTime) =>
        officeStartTime.HasValue && requestedDate.Date == companyNow.Date && companyNow.TimeOfDay >= officeStartTime.Value;
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
    private static string ReasonLabel(string? code) => (code ?? "OTHER").Trim().ToUpperInvariant() switch { "PERSONAL" => "Personal", "MEDICAL" => "Medical", "FAMILY" => "Family", "HOME_MAINTENANCE" => "Home maintenance", "TRAVEL_DISRUPTION" => "Travel disruption", "OPERATIONAL" => "Operational", "MANAGER_INSTRUCTION" => "Manager instruction", _ => "Other" };
    private static string DurationLabel(string? duration) => duration switch { "FirstHalf" => "First half", "SecondHalf" => "Second half", _ => "Full day" };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, 500)];
    private static Result Fail(string code, string message, int statusCode = StatusCodes.Status400BadRequest) => new() { Success = false, StatusCode = statusCode, Message = $"{code}: {message}" };
    private static Result<T> Fail<T>(string code, string message) => new() { Success = false, Message = $"{code}: {message}" };
    private static Result<T> Ok<T>(T value) => new() { Success = true, MethodResult = value };
    private static WorkFromHomePolicyDto Map(WorkFromHomePolicy x) => new() { IsEnabled=x.IsEnabled, MaxDaysPerMonth=x.MaxDaysPerMonth, MaxDaysPerWeek=x.MaxDaysPerWeek, MaxConsecutiveDays=x.MaxConsecutiveDays, MinimumAdvanceNoticeHours=x.MinimumAdvanceNoticeHours, AllowBackdatedRequest=x.AllowBackdatedRequest, MaximumBackdatedDays=x.MaximumBackdatedDays, AllowHalfDay=x.AllowHalfDay, AllowMixedDay=x.AllowMixedDay, ManagerApprovalRequired=x.ManagerApprovalRequired, AllowManagerSelfApproval=x.AllowManagerSelfApproval, AllowOnWeeklyOff=x.AllowOnWeeklyOff, AllowOnHoliday=x.AllowOnHoliday, RequireReason=x.RequireReason, RequireAttachment=x.RequireAttachment, ApplyToAllEmployees=x.ApplyToAllEmployees, ApplicableDepartments=x.ApplicableDepartments, ApplicableEmployeeIds=x.ApplicableEmployeeIds, ExcludedEmployeeIds=x.ExcludedEmployeeIds, ApplicableEmploymentTypes=x.ApplicableEmploymentTypes, EffectiveFrom=x.EffectiveFrom, EffectiveTo=x.EffectiveTo, AllowedCheckInFrom=x.AllowedCheckInFrom, AllowedCheckInUntil=x.AllowedCheckInUntil, AllowedCheckOutFrom=x.AllowedCheckOutFrom, AllowedCheckOutUntil=x.AllowedCheckOutUntil, OfficeStartTime=x.OfficeStartTime, OfficeEndTime=x.OfficeEndTime, TimeZoneId=x.TimeZoneId, FullDayMinimumHours=x.FullDayMinimumHours, HalfDayMinimumHours=x.HalfDayMinimumHours, FullDayAttendanceStatusCode=x.FullDayAttendanceStatusCode, HalfDayAttendanceStatusCode=x.HalfDayAttendanceStatusCode, MixedAttendanceStatusCode=x.MixedAttendanceStatusCode, WfhWfoAttendanceStatusCode=x.WfhWfoAttendanceStatusCode, WfhHdAttendanceStatusCode=x.WfhHdAttendanceStatusCode };
    private static WorkFromHomeResponseDto Map(WorkFromHomeRequest x, EmpUser? employee = null) => new() { RequestId=x.RequestId, UserId=x.UserId, EmployeeId=x.EmployeeId, FromDate=x.FromDate, ToDate=x.ToDate, DurationType=x.DurationType, ReasonCode=x.ReasonCode, ReasonText=x.ReasonText, Status=x.Status, ApproverUserId=x.ApproverUserId, Version=x.Version, ReviewRemarksText=x.ReviewRemarksText, CreatedDate=x.CreatedDate, ReviewedByUserId=x.ReviewedByUserId, ReviewedAt=x.ReviewedAt, EmployeeName = employee is null ? string.Empty : $"{employee.FirstName} {employee.LastName}".Trim(), Department = employee?.Department };
    private static void Apply(WorkFromHomePolicy p, WorkFromHomePolicyDto d) { p.IsEnabled=d.IsEnabled;p.MaxDaysPerMonth=d.MaxDaysPerMonth;p.MaxDaysPerWeek=d.MaxDaysPerWeek;p.MaxConsecutiveDays=d.MaxConsecutiveDays;p.MinimumAdvanceNoticeHours=d.MinimumAdvanceNoticeHours;p.AllowBackdatedRequest=d.AllowBackdatedRequest;p.MaximumBackdatedDays=d.MaximumBackdatedDays;p.AllowHalfDay=d.AllowHalfDay;p.AllowMixedDay=d.AllowMixedDay;p.ManagerApprovalRequired=d.ManagerApprovalRequired;p.AllowManagerSelfApproval=d.AllowManagerSelfApproval;p.AllowOnWeeklyOff=d.AllowOnWeeklyOff;p.AllowOnHoliday=d.AllowOnHoliday;p.RequireReason=d.RequireReason;p.RequireAttachment=d.RequireAttachment;p.ApplyToAllEmployees=d.ApplyToAllEmployees;p.ApplicableDepartments=d.ApplicableDepartments.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();p.ApplicableEmployeeIds=d.ApplicableEmployeeIds.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.Ordinal).ToList();p.ExcludedEmployeeIds=d.ExcludedEmployeeIds.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.Ordinal).ToList();p.ApplicableEmploymentTypes=d.ApplicableEmploymentTypes.Distinct().ToList();p.EffectiveFrom=d.EffectiveFrom?.Date;p.EffectiveTo=d.EffectiveTo?.Date;p.AllowedCheckInFrom=d.AllowedCheckInFrom;p.AllowedCheckInUntil=d.AllowedCheckInUntil;p.AllowedCheckOutFrom=d.AllowedCheckOutFrom;p.AllowedCheckOutUntil=d.AllowedCheckOutUntil;p.OfficeStartTime=d.OfficeStartTime;p.OfficeEndTime=d.OfficeEndTime;p.TimeZoneId=string.IsNullOrWhiteSpace(d.TimeZoneId) ? "UTC" : d.TimeZoneId.Trim();p.FullDayMinimumHours=d.FullDayMinimumHours;p.HalfDayMinimumHours=d.HalfDayMinimumHours;p.FullDayAttendanceStatusCode=d.FullDayAttendanceStatusCode.Trim().ToUpperInvariant();p.HalfDayAttendanceStatusCode=d.HalfDayAttendanceStatusCode.Trim().ToUpperInvariant();p.MixedAttendanceStatusCode=d.MixedAttendanceStatusCode.Trim().ToUpperInvariant();p.WfhWfoAttendanceStatusCode=d.WfhWfoAttendanceStatusCode?.Trim().ToUpperInvariant();p.WfhHdAttendanceStatusCode=d.WfhHdAttendanceStatusCode?.Trim().ToUpperInvariant(); }
}
