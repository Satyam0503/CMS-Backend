using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Utility.middlewares;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Repository.Entities.Attendance;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Net;
using Codeji.CMS.Services.Attendance;

public interface IWorkFromHomeService
{
    Task<Result<WorkFromHomePolicyDto>> GetPolicy();
    Task<Result<EmployeeWfhContextDto>> GetEmployeeContext();
    Task<Result> SavePolicy(WorkFromHomePolicyDto dto);
    Task<Result<IEnumerable<WorkFromHomeEmployeeAllocationDto>>> GetEmployeeAllocations();
    Task<Result> SaveEmployeeAllocation(WorkFromHomeEmployeeAllocationDto dto);
    Task<Result<WorkFromHomeResponseDto>> Create(WorkFromHomeRequestDto dto);
    Task<Result<WorkFromHomeResponseDto>> GetMy();
    Task<Result<WorkFromHomeResponseDto>> GetTeam();
    Task<Result<WorkFromHomeResponseDto>> GetAll();
    Task<Result> Decide(string requestId, string status, WorkFromHomeDecisionDto dto);
    Task<Result> Cancel(string requestId, WorkFromHomeCancelDto dto);
    Task<Result> CheckIn(string requestId);
    Task<Result> CheckOut(string requestId);
    Task<Result<WorkFromHomeTimingDto>> GetTiming(string requestId);
    Task<Result> CorrectInsufficientHours(string exceptionId, WorkFromHomeHoursCorrectionDto dto);
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
    IMongoDbRepository<EmpUser> employees,
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
    IMiddlewareService mail) : IWorkFromHomeService
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
        if (!await ValidCode(dto.FullDayAttendanceStatusCode) || !await ValidCode(dto.HalfDayAttendanceStatusCode) || !await ValidCode(dto.MixedAttendanceStatusCode))
            return Fail("WFH_INVALID_ATTENDANCE_STATUS", "WFH status codes must be active company attendance statuses.");
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
        var activeEmployees = await employees.GetAll(x => x.CompanyId == CompanyId && x.Status && !x.IsDeleted);
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
        if (!await employees.Exist(x => x.CompanyId == CompanyId && x.EmployeeId == employeeId && x.Status && !x.IsDeleted)) return Fail("WFH_EMPLOYEE_NOT_FOUND", "The employee is not active in this company.");
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
        await Log(request, null, request.Status, policy.ManagerApprovalRequired ? "Created" : "AutoApproved", null, null);
        if (!policy.ManagerApprovalRequired)
        {
            var reconcile = await ReconcileApproved(request, employee, policy, request.Version);
            if (!reconcile.Success) return Fail<WorkFromHomeResponseDto>("WFH_ATTENDANCE_CONFLICT", reconcile.Message);
        }
        QueueHrNotification(request, employee, policy.ManagerApprovalRequired ? "WFH request submitted" : "Employee working from home");
        return Ok(Map(request));
    }

    public async Task<Result<WorkFromHomeResponseDto>> GetMy()
    {
        var items = await requests.GetAll(x => x.CompanyId == CompanyId && x.UserId == UserId);
        return new Result<WorkFromHomeResponseDto> { Success = true, MethodResults = items.OrderByDescending(x => x.CreatedDate).Select(Map).ToList(), TotalRecords = items.Count() };
    }

    public async Task<Result<WorkFromHomeResponseDto>> GetTeam()
    {
        var items = await requests.GetAll(x => x.CompanyId == CompanyId && x.ApproverUserId == UserId);
        return new Result<WorkFromHomeResponseDto> { Success = true, MethodResults = items.OrderByDescending(x => x.CreatedDate).Select(Map).ToList(), TotalRecords = items.Count() };
    }

    public async Task<Result<WorkFromHomeResponseDto>> GetAll()
    {
        var items = await requests.GetAll(x => x.CompanyId == CompanyId);
        return new Result<WorkFromHomeResponseDto> { Success = true, MethodResults = items.OrderByDescending(x => x.CreatedDate).Select(Map).ToList(), TotalRecords = items.Count() };
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
        return result.Success ? result : Fail("WFH_REQUEST_CHANGED", "The request was changed by another action.");
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
        if (exception is null || exception.ExceptionType != "WFH_INSUFFICIENT_HOURS")
            return Fail("WFH_EXCEPTION_NOT_FOUND", "The WFH insufficient-hours exception was not found.");
        if (exception.Status != "PENDING_REVIEW" || exception.Version != dto.ExceptionVersion)
            return Fail("WFH_EXCEPTION_CHANGED", "This exception was already changed. Refresh and try again.");
        if (dto.CheckOutTime <= dto.CheckInTime)
            return Fail("WFH_INVALID_HOURS", "Check-out time must be later than check-in time.");
        if (string.IsNullOrWhiteSpace(dto.Remarks))
            return Fail("WFH_CORRECTION_REASON_REQUIRED", "A correction reason is required.");

        var employee = await ActiveEmployee(exception.UserId);
        if (employee is null) return Fail("WFH_EMPLOYEE_NOT_ELIGIBLE", "The employee is no longer active in this company.");
        if (!exception.AttendanceDate.HasValue)
            return Fail("WFH_EXCEPTION_DATE_MISSING", "The WFH exception does not have an affected attendance date.");
        var attendanceDate = exception.AttendanceDate.Value.Date;
        try { await attendanceEditGuard.EnsureEditableWorkingDayAsync(CompanyId, employee.UserId, attendanceDate); }
        catch (InvalidOperationException ex) { return Fail("WFH_ATTENDANCE_NOT_EDITABLE", ex.Message); }

        var request = await requests.FirstOrDefault(x => x.CompanyId == CompanyId && x.RequestId == exception.LeaveRequestId && x.UserId == employee.UserId && x.Status == "Approved");
        if (request is null) return Fail("WFH_REQUEST_NOT_AVAILABLE", "The approved WFH request for this exception is no longer available.");
        // Older exception rows can have no attendance id (or an id written before the
        // attendance document was persisted).  The WFH request + employee + business
        // date is the authoritative source-owned identity, so use it as a safe fallback.
        var row = string.IsNullOrWhiteSpace(exception.ExistingAttendanceId)
            ? null
            : await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.AttendanceId == exception.ExistingAttendanceId && x.UserId == employee.UserId);
        if (row is null || row.SourceType != "WFH_REQUEST" || row.SourceId != request.RequestId)
        {
            var endOfAttendanceDate = attendanceDate.AddDays(1);
            row = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId && x.Date >= attendanceDate && x.Date < endOfAttendanceDate);
        }
        if (row is null || row.SourceType != "WFH_REQUEST" || row.SourceId != request.RequestId)
            return Fail("WFH_ATTENDANCE_CONFLICT", "No WFH attendance record matches this employee, request, and affected date. Refresh the exception list and verify the approved WFH request.");
        if (dto.CheckInTime.Date != attendanceDate || dto.CheckOutTime.Date != attendanceDate)
            return Fail("WFH_CORRECTION_DATE_INVALID", "Both corrected times must be on the affected attendance date.");

        var policy = await ActivePolicy();
        if (policy is null) return Fail("WFH_POLICY_DISABLED", "WFH policy is disabled.");
        var minimum = request.DurationType == "FullDay" ? policy.FullDayMinimumHours : policy.HalfDayMinimumHours;
        var totalHours = Math.Round((decimal)(dto.CheckOutTime - dto.CheckInTime).TotalHours, 2, MidpointRounding.AwayFromZero);
        if (totalHours < minimum)
            return Fail("WFH_HOURS_STILL_INSUFFICIENT", $"Corrected WFH hours are {totalHours:0.##}; at least {minimum:0.##} hours are required.");

        row.CheckInTime = dto.CheckInTime;
        row.CheckOutTime = dto.CheckOutTime;
        row.TotalHours = totalHours;
        row.RemarkCode = "WFH_HOURS_CORRECTED";
        row.Remarks = $"HR/Admin corrected WFH hours: {Clean(dto.Remarks)}";
        var saved = await attendance.Update(Builders<AttendanceModel>.Filter.Where(x => x.AttendanceId == row.AttendanceId && x.SourceType == "WFH_REQUEST" && x.SourceId == request.RequestId), row);
        if (!saved.Success) return Fail("WFH_ATTENDANCE_UPDATE_FAILED", "The WFH attendance record could not be updated.");

        exception.Status = "RESOLVED";
        exception.Resolution = $"WFH hours corrected to {totalHours:0.##} hour(s) by HR/Admin.";
        exception.ReviewedBy = UserId;
        exception.ReviewedAt = DateTime.UtcNow;
        exception.UpdatedBy = UserId;
        exception.UpdatedDate = DateTime.UtcNow;
        exception.Version++;
        var resolved = await exceptions.Update(Builders<AttendancePayrollException>.Filter.Where(x => x.Id == exception.Id && x.Version == dto.ExceptionVersion), exception);
        if (!resolved.Success) return Fail("WFH_EXCEPTION_CHANGED", "Attendance was corrected, but the exception changed before it could be resolved. Recheck the exception.");
        await InvalidateSummary(employee, attendanceDate);
        await Log(request, request.Status, request.Status, "HoursCorrected", "WFH_HOURS_CORRECTED", dto.Remarks);
        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = $"WFH hours updated to {totalHours:0.##} hours and the exception was resolved." };
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
        var checkInUntil = schedule?.CheckInAllowedUntil ?? policy.AllowedCheckInUntil;
        var checkOutUntil = schedule?.CheckOutAllowedUntil ?? policy.AllowedCheckOutUntil;
        if (checkIn && ((checkInFrom.HasValue && companyNow.TimeOfDay < checkInFrom) || (checkInUntil.HasValue && companyNow.TimeOfDay > checkInUntil))) return Fail("WFH_CHECKIN_NOT_OPEN", "Check-in is outside the effective office schedule window.");
        if (!checkIn && checkOutUntil.HasValue && companyNow.TimeOfDay > checkOutUntil) return Fail("WFH_CHECK_OUT_WINDOW_CLOSED", "Check-out is outside the effective office schedule window.");
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
            if (workedMinutes < requiredMinutes) return Fail("WFH_CHECK_OUT_TOO_EARLY", $"Check-out is available after {requiredMinutes} verified working minutes. {Math.Max(0, requiredMinutes - workedMinutes)} minute(s) remain.");
            if (row is not null) { row.CheckOutTime = now; row.TotalHours = Math.Max(0, (decimal)(now - checkInTime.Value).TotalHours); row.Remarks = $"Checked out through employee WFH portal ({requestId})"; }
            else { segment!.CheckOutTime = now; segment.TotalHours = Math.Max(0, (decimal)(now - checkInTime.Value).TotalHours); segment.Remarks = $"Checked out through employee WFH portal ({requestId})"; segment.UpdatedAtUtc = now; }
        }
        var result = row is not null
            ? await attendance.Update(Builders<AttendanceModel>.Filter.Where(x => x.AttendanceId == row.AttendanceId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId), row)
            : await attendanceSegments.Update(Builders<AttendanceDaySegment>.Filter.Where(x => x.AttendanceDaySegmentId == segment!.AttendanceDaySegmentId && x.SourceType == "WFH_REQUEST" && x.SourceId == requestId), segment!);
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
        var code = request.DurationType == "Mixed" ? policy.MixedAttendanceStatusCode : request.DurationType == "FullDay" ? policy.FullDayAttendanceStatusCode : policy.HalfDayAttendanceStatusCode;
        if (!await ValidCode(code)) return Fail("WFH_INVALID_ATTENDANCE_STATUS", "Configured WFH status is inactive.");
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
                    await CreateSegmentException(request, employee, date, existingSegment, "WFH_ATTENDANCE_SEGMENT_CONFLICT", $"Approved WFH conflicts with the {segmentName} attendance segment.");
                    return Fail("WFH_ATTENDANCE_SEGMENT_CONFLICT", "A blocking half-day attendance conflict was recorded.");
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
                await CreateSegmentException(request, employee, date, existingSegments.First(), "WFH_ATTENDANCE_SEGMENT_CONFLICT", "Approved full-day WFH conflicts with existing half-day attendance.");
                return Fail("WFH_ATTENDANCE_SEGMENT_CONFLICT", "A blocking half-day attendance conflict was recorded.");
            }
            var row = await attendance.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.Date >= date && x.Date < date.AddDays(1));
            if (row is not null && !(row.SourceType == "WFH_REQUEST" && row.SourceId == request.RequestId) && !AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(row.SourceType))
            {
                await CreateException(request, row, row.SourceType == "LEAVE" ? "WFH_LEAVE_CONFLICT" : "WFH_ATTENDANCE_CONFLICT", "BLOCKING", "Approved WFH conflicts with existing attendance.");
                return Fail(row.SourceType == "LEAVE" ? "WFH_LEAVE_CONFLICT" : "WFH_ATTENDANCE_CONFLICT", "A blocking attendance conflict was recorded.");
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
        if (dto.FromDate.Date > dto.ToDate.Date) return ("WFH_INVALID_DATE_RANGE", "End date cannot be before start date.");
        if (dto.ToDate.Date < DateTime.UtcNow.Date && !policy.AllowBackdatedRequest) return ("WFH_BACKDATED_NOT_ALLOWED", "Backdated WFH requests are not allowed.");
        if (policy.MaximumBackdatedDays is int max && dto.FromDate.Date < DateTime.UtcNow.Date.AddDays(-max)) return ("WFH_BACKDATED_LIMIT", "The backdated request exceeds the policy limit.");
        if (policy.MinimumAdvanceNoticeHours > 0 && dto.FromDate.ToUniversalTime() < DateTime.UtcNow.AddHours(policy.MinimumAdvanceNoticeHours)) return ("WFH_ADVANCE_NOTICE_REQUIRED", "The required advance notice was not met.");
        if (policy.RequireReason && string.IsNullOrWhiteSpace(dto.ReasonText)) return ("WFH_REASON_REQUIRED", "A reason is required.");
        if (dto.DurationType is not ("FullDay" or "FirstHalf" or "SecondHalf" or "Mixed")) return ("WFH_INVALID_DURATION", "Invalid WFH duration.");
        if (dto.DurationType is "FirstHalf" or "SecondHalf" && !policy.AllowHalfDay) return ("WFH_HALF_DAY_NOT_ALLOWED", "Half day WFH is not allowed.");
        if (dto.DurationType == "Mixed" && !policy.AllowMixedDay) return ("WFH_MIXED_DAY_NOT_ALLOWED", "Mixed WFH is not allowed.");
        if (!IsEffectiveFor(policy, dto.FromDate.Date) || !IsEffectiveFor(policy, dto.ToDate.Date)) return ("WFH_POLICY_NOT_EFFECTIVE", "The selected date is outside the effective WFH policy period.");
        if (await requests.Exist(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && (x.Status == "Pending" || x.Status == "Approved" || x.Status == "Returned") && dto.FromDate.Date <= x.ToDate.Date && dto.ToDate.Date >= x.FromDate.Date)) return ("WFH_OVERLAPPING_REQUEST", "The request overlaps an existing WFH request.");
        foreach (var week in Days(dto.FromDate, dto.ToDate).Select(StartOfWeek).Distinct())
        {
            var usedDates = WeeklyUsage(await requests.GetAll(x => x.CompanyId == CompanyId && x.UserId == employee.UserId && (x.Status == "Pending" || x.Status == "Approved" || x.Status == "Returned") && x.FromDate < week.AddDays(7) && x.ToDate >= week), week);
            var requestedDates = WeeklyUsage(dto.FromDate, dto.ToDate, dto.DurationType, week);
            var weeklyLimit = WeeklyLimitFor(policy, employee);
            if (usedDates + requestedDates > weeklyLimit) return ("WFH_WEEKLY_QUOTA_EXCEEDED", $"Only {weeklyLimit} WFH day(s) are allowed per week.");
        }
        foreach (var day in Days(dto.FromDate, dto.ToDate)) { var check = await ValidateWorkingDay(employee, policy, day); if (check is not null) return check; }
        var schedule = await effectiveSchedules.ResolveAsync(CompanyId, employee, dto.FromDate);
        var companyNow = CompanyNow(schedule?.TimeZoneId ?? policy.TimeZoneId);
        var officeStart = schedule?.StartTime ?? policy.OfficeStartTime;
        if (Days(dto.FromDate, dto.ToDate).Any(day => IsSameDayWfhStartCutoffReached(day, companyNow, officeStart)))
        {
            var startLabel = DateTime.Today.Add(officeStart!.Value).ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
            return ("WFH_SAME_DAY_OFFICE_STARTED", $"Work From Home for today must be requested before your office start time ({startLabel}).");
        }
        return null;
    }

    private async Task<(string Code, string Message)?> ValidateWorkingDay(EmpUser employee, WorkFromHomePolicy policy, DateTime date)
    {
        var month = new DateTime(date.Year, date.Month, 1);
        if (await summaries.Exist(x => x.CompanyId == CompanyId && x.EmployeeId == employee.EmployeeId && x.PayrollMonth == month && x.IsLocked)) return ("WFH_DATE_LOCKED", "Attendance month is locked.");
        var off = await weeklyOffs.FirstOrDefault(x => x.CompanyId == CompanyId);
        if (!policy.AllowOnWeeklyOff && (off?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday]).Contains((int)date.DayOfWeek)) return ("WFH_WEEKLY_OFF_NOT_ALLOWED", "WFH is not allowed on a weekly off.");
        var holidays = await calendar.GetAll(x => x.CompanyId == CompanyId && x.Type == EnumsHelper.CalendarItem.Holiday && (x.Recurring || x.Date.Date == date.Date));
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

    private async Task<EmpUser?> ActiveEmployee(string userId) => string.IsNullOrWhiteSpace(userId) ? null : await employees.FirstOrDefault(x => x.CompanyId == CompanyId && x.UserId == userId && x.Status && !x.IsDeleted);
    private async Task<WorkFromHomePolicy?> ActivePolicy() => await policies.FirstOrDefault(x => x.CompanyId == CompanyId && x.IsEnabled);
    private async Task<bool> ValidCode(string code) => !string.IsNullOrWhiteSpace(code) && await statuses.Exist(x => x.CompanyId == CompanyId && x.Code == code.Trim().ToUpperInvariant() && x.IsActive);
    private async Task InvalidateSummary(EmpUser employee, DateTime day) => await summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == CompanyId && x.EmployeeId == employee.EmployeeId && x.PayrollMonth == new DateTime(day.Year, day.Month, 1) && !x.IsLocked));
    private async Task CreateException(WorkFromHomeRequest request, AttendanceModel row, string type, string severity, string reason)
    {
        var month = new DateTime(row.Date.Year, row.Date.Month, 1);
        if (await exceptions.Exist(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.PayrollMonth == month && x.ExceptionType == type && x.AttendanceDate == row.Date && x.LeaveRequestId == request.RequestId)) return;
        await exceptions.AddOne(new AttendancePayrollException { CompanyId = CompanyId, UserId = request.UserId, EmployeeId = request.EmployeeId, PayrollMonth = month, AttendanceDate = row.Date, AffectedDates = [row.Date], ExceptionType = type, Severity = severity, Status = "PENDING_REVIEW", LeaveRequestId = request.RequestId, ExistingAttendanceId = row.AttendanceId, ExistingStatus = row.Status, RequestedLeaveStatus = request.DurationType, SourceVersion = request.Version, Reason = reason });
        await Log(request, request.Status, request.Status, "AttendanceConflictCreated", type, reason);
    }
    private async Task CreateSegmentException(WorkFromHomeRequest request, EmpUser employee, DateTime date, AttendanceDaySegment segment, string type, string reason)
    {
        var month = new DateTime(date.Year, date.Month, 1);
        if (await exceptions.Exist(x => x.CompanyId == CompanyId && x.UserId == request.UserId && x.PayrollMonth == month && x.ExceptionType == type && x.AttendanceDate == date && x.LeaveRequestId == request.RequestId)) return;
        await exceptions.AddOne(new AttendancePayrollException { CompanyId = CompanyId, UserId = request.UserId, EmployeeId = employee.EmployeeId, PayrollMonth = month, AttendanceDate = date, AffectedDates = [date], ExceptionType = type, Severity = "BLOCKING", Status = "PENDING_REVIEW", LeaveRequestId = request.RequestId, ExistingAttendanceId = segment.AttendanceDaySegmentId, ExistingStatus = segment.Status, RequestedLeaveStatus = request.DurationType, SourceVersion = request.Version, Reason = reason });
        await Log(request, request.Status, request.Status, "AttendanceConflictCreated", type, reason);
    }
    private void QueueHrNotification(WorkFromHomeRequest request, EmpUser employee, string subject)
    {
        // The queued job has no HTTP context, so capture tenant and actor data now.
        var companyId = CompanyId;
        var submittedByUserId = request.UserId;
        priorityQueue.QueueBackgroundWorkItem(async _ =>
        {
            var roleIds = (await companyRoles.GetAll(x => x.CompanyId == companyId && !x.IsDeleted && (x.RoleType == (int)EnumsHelper.Roles.Administrator || x.RoleType == (int)EnumsHelper.Roles.HR || x.RoleType == (int)EnumsHelper.Roles.HRExecutive), withDefaultFilter: false)).Select(x => x.RolesId).ToHashSet();
            var recipients = (await employees.GetAll(x => x.CompanyId == companyId && x.Status && roleIds.Contains(x.RoleId), withDefaultFilter: false)).GroupBy(x => x.UserId, StringComparer.Ordinal).Select(x => x.First());
            var body = $"<p><strong>{WebUtility.HtmlEncode(subject)}</strong></p><p>Employee: {WebUtility.HtmlEncode(employee.FirstName)} {WebUtility.HtmlEncode(employee.LastName)} ({WebUtility.HtmlEncode(employee.EmployeeId)})</p><p>Department: {WebUtility.HtmlEncode(employee.Department)}<br/>Role: {WebUtility.HtmlEncode(employee.JobRole)}<br/>Dates: {request.FromDate:dd MMM yyyy} - {request.ToDate:dd MMM yyyy}<br/>Duration: {WebUtility.HtmlEncode(request.DurationType)}<br/>Reason category: {WebUtility.HtmlEncode(ReasonLabel(request.ReasonCode))}</p>";
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
            await mail.EmailSendAndSave(new EmpEmailLogs { CompanyId = companyId, UserTo = employee.UserId, UserFrom = actorUserId, Email = employee.Email, Subject = subject, Body = $"<p>{WebUtility.HtmlEncode(subject)}</p><p>WFH dates: {request.FromDate:dd MMM yyyy} - {request.ToDate:dd MMM yyyy}</p>", EmailLogType = EnumsHelper.MailType.LeaveReplyMail });
        }, 1);
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
    private static string ReasonLabel(string? code) => (code ?? "OTHER").Trim().ToUpperInvariant() switch { "PERSONAL" => "Personal", "MEDICAL" => "Medical", "FAMILY" => "Family", "HOME_MAINTENANCE" => "Home maintenance", "TRAVEL_DISRUPTION" => "Travel disruption", "OPERATIONAL" => "Operational", "MANAGER_INSTRUCTION" => "Manager instruction", _ => "Other" };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, 500)];
    private static Result Fail(string code, string message) => new() { Success = false, Message = $"{code}: {message}" };
    private static Result<T> Fail<T>(string code, string message) => new() { Success = false, Message = $"{code}: {message}" };
    private static Result<T> Ok<T>(T value) => new() { Success = true, MethodResult = value };
    private static WorkFromHomePolicyDto Map(WorkFromHomePolicy x) => new() { IsEnabled=x.IsEnabled, MaxDaysPerMonth=x.MaxDaysPerMonth, MaxDaysPerWeek=x.MaxDaysPerWeek, MaxConsecutiveDays=x.MaxConsecutiveDays, MinimumAdvanceNoticeHours=x.MinimumAdvanceNoticeHours, AllowBackdatedRequest=x.AllowBackdatedRequest, MaximumBackdatedDays=x.MaximumBackdatedDays, AllowHalfDay=x.AllowHalfDay, AllowMixedDay=x.AllowMixedDay, ManagerApprovalRequired=x.ManagerApprovalRequired, AllowManagerSelfApproval=x.AllowManagerSelfApproval, AllowOnWeeklyOff=x.AllowOnWeeklyOff, AllowOnHoliday=x.AllowOnHoliday, RequireReason=x.RequireReason, RequireAttachment=x.RequireAttachment, ApplyToAllEmployees=x.ApplyToAllEmployees, ApplicableDepartments=x.ApplicableDepartments, ApplicableEmployeeIds=x.ApplicableEmployeeIds, ExcludedEmployeeIds=x.ExcludedEmployeeIds, ApplicableEmploymentTypes=x.ApplicableEmploymentTypes, EffectiveFrom=x.EffectiveFrom, EffectiveTo=x.EffectiveTo, AllowedCheckInFrom=x.AllowedCheckInFrom, AllowedCheckInUntil=x.AllowedCheckInUntil, AllowedCheckOutFrom=x.AllowedCheckOutFrom, AllowedCheckOutUntil=x.AllowedCheckOutUntil, OfficeStartTime=x.OfficeStartTime, OfficeEndTime=x.OfficeEndTime, TimeZoneId=x.TimeZoneId, FullDayMinimumHours=x.FullDayMinimumHours, HalfDayMinimumHours=x.HalfDayMinimumHours, FullDayAttendanceStatusCode=x.FullDayAttendanceStatusCode, HalfDayAttendanceStatusCode=x.HalfDayAttendanceStatusCode, MixedAttendanceStatusCode=x.MixedAttendanceStatusCode };
    private static WorkFromHomeResponseDto Map(WorkFromHomeRequest x) => new() { RequestId=x.RequestId, UserId=x.UserId, EmployeeId=x.EmployeeId, FromDate=x.FromDate, ToDate=x.ToDate, DurationType=x.DurationType, ReasonCode=x.ReasonCode, ReasonText=x.ReasonText, Status=x.Status, ApproverUserId=x.ApproverUserId, Version=x.Version, ReviewRemarksText=x.ReviewRemarksText, CreatedDate=x.CreatedDate, ReviewedByUserId=x.ReviewedByUserId, ReviewedAt=x.ReviewedAt };
    private static void Apply(WorkFromHomePolicy p, WorkFromHomePolicyDto d) { p.IsEnabled=d.IsEnabled;p.MaxDaysPerMonth=d.MaxDaysPerMonth;p.MaxDaysPerWeek=d.MaxDaysPerWeek;p.MaxConsecutiveDays=d.MaxConsecutiveDays;p.MinimumAdvanceNoticeHours=d.MinimumAdvanceNoticeHours;p.AllowBackdatedRequest=d.AllowBackdatedRequest;p.MaximumBackdatedDays=d.MaximumBackdatedDays;p.AllowHalfDay=d.AllowHalfDay;p.AllowMixedDay=d.AllowMixedDay;p.ManagerApprovalRequired=d.ManagerApprovalRequired;p.AllowManagerSelfApproval=d.AllowManagerSelfApproval;p.AllowOnWeeklyOff=d.AllowOnWeeklyOff;p.AllowOnHoliday=d.AllowOnHoliday;p.RequireReason=d.RequireReason;p.RequireAttachment=d.RequireAttachment;p.ApplyToAllEmployees=d.ApplyToAllEmployees;p.ApplicableDepartments=d.ApplicableDepartments.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();p.ApplicableEmployeeIds=d.ApplicableEmployeeIds.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.Ordinal).ToList();p.ExcludedEmployeeIds=d.ExcludedEmployeeIds.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).Distinct(StringComparer.Ordinal).ToList();p.ApplicableEmploymentTypes=d.ApplicableEmploymentTypes.Distinct().ToList();p.EffectiveFrom=d.EffectiveFrom?.Date;p.EffectiveTo=d.EffectiveTo?.Date;p.AllowedCheckInFrom=d.AllowedCheckInFrom;p.AllowedCheckInUntil=d.AllowedCheckInUntil;p.AllowedCheckOutFrom=d.AllowedCheckOutFrom;p.AllowedCheckOutUntil=d.AllowedCheckOutUntil;p.OfficeStartTime=d.OfficeStartTime;p.OfficeEndTime=d.OfficeEndTime;p.TimeZoneId=string.IsNullOrWhiteSpace(d.TimeZoneId) ? "UTC" : d.TimeZoneId.Trim();p.FullDayMinimumHours=d.FullDayMinimumHours;p.HalfDayMinimumHours=d.HalfDayMinimumHours;p.FullDayAttendanceStatusCode=d.FullDayAttendanceStatusCode.Trim().ToUpperInvariant();p.HalfDayAttendanceStatusCode=d.HalfDayAttendanceStatusCode.Trim().ToUpperInvariant();p.MixedAttendanceStatusCode=d.MixedAttendanceStatusCode.Trim().ToUpperInvariant(); }
}
