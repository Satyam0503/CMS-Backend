using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Attendance;

public enum AttendanceTimingMode { Auto, Custom, None }
public enum ExistingAttendancePolicy { CreateMissingOnly, SkipExisting, UpdateManualOnly }

public sealed class AttendanceMutationRequest
{
    public string CompanyId { get; init; } = string.Empty;
    public string ActorUserId { get; init; } = string.Empty;
    public string TargetUserId { get; init; } = string.Empty;
    public DateOnly AttendanceDate { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public AttendanceTimingMode TimingMode { get; init; }
    public TimeOnly? CheckInTime { get; init; }
    public TimeOnly? CheckOutTime { get; init; }
    public string RemarkCode { get; init; } = "ADMIN_MANUAL";
    public string? OperatorRemark { get; init; }
    /// <summary>
    /// Identifies the system that owns the row. Manual operations intentionally use the
    /// default value; scheduled processes must supply their own source.
    /// </summary>
    public string? SourceType { get; init; }
    /// <summary>HR/Admin explicitly selected the status in the attendance editor.</summary>
    public bool PreserveRequestedStatus { get; init; }
    public ExistingAttendancePolicy ExistingRecordPolicy { get; init; } = ExistingAttendancePolicy.UpdateManualOnly;
    public long? ExpectedAttendanceVersion { get; init; }
    public bool AllowSourceOverride { get; init; }
}

public sealed class PreparedAttendanceMutation
{
    public required EmpUser Employee { get; init; }
    public required AttendanceModel Attendance { get; init; }
    public AttendanceModel? Existing { get; init; }
    public bool ShouldWrite { get; init; }
    public bool RequiresLeaveReversal { get; init; }
    public string? LeaveRequestIdToReverse { get; init; }
    public bool RequiresWfhConversion { get; init; }
    public string? WfhRequestIdToConvert { get; init; }
}

public interface IAttendanceMutationValidator
{
    Task<Result<PreparedAttendanceMutation>> PrepareAsync(AttendanceMutationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Single authoritative preparation path for normal/manual and future bulk/import mutations.</summary>
public sealed class AttendanceMutationValidator(
    IAttendanceRepository attendance,
    IAttendanceEditGuard editGuard,
    IEffectiveOfficeScheduleService schedules,
    IAttendanceStatusCalculationService statusCalculation,
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<AttendanceStatusSetting> statuses,
    IMongoDbRepository<AttendanceDaySegment> attendanceSegments) : IAttendanceMutationValidator
{
    // These statuses carry business records beyond a single attendance row:
    // leave balance/history or an approved WFH request. They must never be
    // manufactured through the generic HR/Admin attendance editor.
    private static readonly HashSet<string> WorkflowOwnedStatusCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SL", "CL", "EL", "UL", "COMP-OFF", "WFH", "WFH+WFO", "WFH-HD",
        "WFH+SL", "WFH+CL", "CL-HALF", "SL-HALF"
    };

    public async Task<Result<PreparedAttendanceMutation>> PrepareAsync(AttendanceMutationRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<PreparedAttendanceMutation> { Success = false };
        if (string.IsNullOrWhiteSpace(request.CompanyId) || string.IsNullOrWhiteSpace(request.ActorUserId) || string.IsNullOrWhiteSpace(request.TargetUserId) || string.IsNullOrWhiteSpace(request.StatusCode))
            return Fail(result, "ATTENDANCE_MUTATION_INVALID");

        var date = DateTime.SpecifyKind(request.AttendanceDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        if (DateOnly.FromDateTime(date) > DateOnly.FromDateTime(IndiaTime.Today))
            return Fail(result, "ATTENDANCE_FUTURE_DATE_NOT_ALLOWED");
        // A completed exit disables new access, but it must not make valid historical
        // attendance (including the final payroll month) impossible to repair.
        var employee = (await employees.GetAll(x => x.CompanyId == request.CompanyId && x.UserId == request.TargetUserId && !x.IsDeleted, withDefaultFilter: false)).FirstOrDefault();
        if (employee is null) return Fail(result, "ATTENDANCE_TARGET_UNAVAILABLE");
        if (EmploymentPeriod.TryGetJoiningDate(employee, out var joined) && date.Date < joined.Date)
            return Fail(result, $"Attendance cannot be marked before the employee's joining date of {joined:dd-MMM-yyyy}.");
        var lastWorkingDate = EmploymentPeriod.GetLastWorkingDate(employee);
        if (lastWorkingDate.HasValue && date.Date > lastWorkingDate.Value.Date) return Fail(result, "ATTENDANCE_AFTER_EXIT");

        // Self-heal any stale exit fields on active, non-notice employees in the database
        if (EmploymentExitRules.IsCurrentEmployee(employee) &&
            EmploymentExitRules.GetEffectiveEmploymentStatus(employee) != EmployeeLifecycleStatuses.NoticePeriod &&
            (employee.LastWorkingDate.HasValue || !string.IsNullOrWhiteSpace(employee.ExitDate)))
        {
            employee.LastWorkingDate = null;
            employee.ExitDate = null;
            await employees.Update(MongoDB.Driver.Builders<EmpUser>.Filter.Eq(x => x.UserId, employee.UserId), employee);
        }
        if (!EmploymentPeriod.WasEmployedOn(employee, date)) return Fail(result, "ATTENDANCE_TARGET_UNAVAILABLE");
        if (!EmploymentPeriod.IsAttendanceExpectedOn(employee, date)) return Fail(result, "ATTENDANCE_NOT_EXPECTED_FOR_LIFECYCLE");

        try
        {
            var isExceptionResolution = string.Equals(request.RemarkCode, "MONTHLY_EXCEPTION_RESOLUTION", StringComparison.OrdinalIgnoreCase);
            await editGuard.EnsureEditableWorkingDayAsync(request.CompanyId, request.TargetUserId, date, isExceptionResolution);
        }
        catch (InvalidOperationException ex) { return Fail(result, ex.Message); }

        if (request.AllowSourceOverride && (string.IsNullOrWhiteSpace(request.OperatorRemark) || request.OperatorRemark.Trim().Length < 10))
            return Fail(result, "ATTENDANCE_OVERRIDE_REASON_REQUIRED");

        var code = request.StatusCode.Trim().ToUpperInvariant();
        var status = (await statuses.GetAll(x => x.CompanyId == request.CompanyId && x.Code == code && x.IsActive, withDefaultFilter: false)).FirstOrDefault();
        if (status is null) return Fail(result, "ATTENDANCE_STATUS_INVALID");
        if (!request.AllowSourceOverride && (string.IsNullOrWhiteSpace(request.SourceType) || string.Equals(request.SourceType, "ADMIN_MANUAL", StringComparison.OrdinalIgnoreCase)) &&
            WorkflowOwnedStatusCodes.Contains(code))
            return Fail(result, "ATTENDANCE_WORKFLOW_STATUS_REQUIRES_LEAVE_OR_WFH_REQUEST");

        DateTime? checkIn = null, checkOut = null;
        CompanyOfficeSchedule? schedule = null;
        if (status.RequiresTime || code == "HD" || (request.TimingMode == AttendanceTimingMode.Custom && (request.CheckInTime != null || request.CheckOutTime != null)))
        {
            schedule = await schedules.ResolveAsync(request.CompanyId, employee, date, cancellationToken);
            schedule ??= new CompanyOfficeSchedule
            {
                CompanyId = request.CompanyId,
                ScheduleId = "DEFAULT_SYSTEM_SCHEDULE",
                Name = "General Shift (09:00 - 18:00)",
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                GraceMinutes = 15,
                CheckInAllowedFrom = new TimeSpan(8, 0, 0),
                CheckInAllowedUntil = new TimeSpan(10, 0, 0),
                CheckOutAllowedFrom = new TimeSpan(17, 0, 0),
                CheckOutAllowedUntil = new TimeSpan(20, 0, 0),
                BreakMinutes = 60,
                RequiredWorkingMinutes = 480,
                TimeZoneId = "Asia/Kolkata",
                IsDefault = true,
                IsActive = true
            };
            if (request.TimingMode == AttendanceTimingMode.Auto)
            {
                // System-generated Present rows carry the effective schedule's planned
                // punch window. This is tenant/employee schedule-derived (for example
                // 09:00–18:00), never a frontend or global hard-coded value.
                var shiftStart = schedule?.StartTime ?? new TimeSpan(9, 0, 0);
                var shiftEnd = schedule?.EndTime ?? new TimeSpan(18, 0, 0);
                var tz = schedule?.TimeZoneId ?? "Asia/Kolkata";

                var effIn = shiftStart;
                var effOut = shiftEnd;

                // When marking LHD in Auto mode, default check-in to the 1-hour late window (e.g. 10:00)
                if (string.Equals(code, "LHD", StringComparison.OrdinalIgnoreCase))
                {
                    effIn = shiftStart.Add(TimeSpan.FromHours(1));
                }
                // When marking ED in Auto mode, default check-out to the 1-hour early window (e.g. 17:00)
                else if (string.Equals(code, "ED", StringComparison.OrdinalIgnoreCase))
                {
                    effOut = shiftEnd.Subtract(TimeSpan.FromHours(1));
                }
                else if (string.Equals(code, "LHD+ED", StringComparison.OrdinalIgnoreCase))
                {
                    effIn = shiftStart.Add(TimeSpan.FromHours(1));
                    effOut = shiftEnd.Subtract(TimeSpan.FromHours(1));
                }
                else if (string.Equals(code, "HD", StringComparison.OrdinalIgnoreCase))
                {
                    var reqMins = schedule?.RequiredWorkingMinutes ?? 480;
                    effOut = shiftStart.Add(TimeSpan.FromMinutes(reqMins / 2));
                }

                checkIn = ToScheduleUtc(date, effIn, tz);
                checkOut = ToScheduleUtc(date, effOut, tz);
                if (effOut <= effIn)
                {
                    checkOut = checkOut.Value.AddDays(1);
                }
            }
            else if (request.TimingMode == AttendanceTimingMode.Custom)
            {
                var effCheckIn = request.CheckInTime ?? (schedule is not null ? TimeOnly.FromTimeSpan(schedule.StartTime) : new TimeOnly(9, 0));
                var effCheckOut = request.CheckOutTime ?? (schedule is not null ? TimeOnly.FromTimeSpan(schedule.EndTime) : new TimeOnly(18, 0));

                var tz = schedule?.TimeZoneId ?? "Asia/Kolkata";
                checkIn = ToScheduleUtc(date, effCheckIn.ToTimeSpan(), tz);
                checkOut = ToScheduleUtc(date, effCheckOut.ToTimeSpan(), tz);
                if (checkOut <= checkIn)
                {
                    // Check if this is an overnight shift
                    if ((schedule is not null && schedule.EndTime < schedule.StartTime) ||
                        effCheckOut < effCheckIn)
                    {
                        checkOut = checkOut.Value.AddDays(1);
                    }
                }
                if (checkOut <= checkIn)
                    return Fail(result, "Check-out time cannot be earlier than or equal to check-in time.");
            }
            else return Fail(result, "ATTENDANCE_TIMING_REQUIRED");
        }
        else
        {
            // The selected status owns its timing rules. A stale editor/API payload must
            // never prevent Present -> Absent (or another non-timed transition), and it
            // must never leave former Present clock events on the persisted row.
            checkIn = null;
            checkOut = null;
        }

        var existing = await attendance.GetByUserAndDateAsync(request.CompanyId, request.TargetUserId, date);
        // A split day is owned by its leave/WFH workflow even when there is no
        // compatibility AttendanceModel row.
        var existingSegments = (await attendanceSegments.GetAll(x =>
                x.CompanyId == request.CompanyId &&
                x.UserId == request.TargetUserId &&
                x.Date >= date &&
                x.Date < date.AddDays(1))).ToList();
        var sourceOwnedSegments = existingSegments.Where(x => x.SourceType is "LEAVE" or "WFH_REQUEST").ToList();
        var requiresLeaveReversal = false;
        string? leaveRequestIdToReverse = null;
        var requiresWfhConversion = false;
        string? wfhRequestIdToConvert = null;
        var isWfhTargetStatus = string.Equals(code, "WFH", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "WFH+WFO", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "WFH-HD", StringComparison.OrdinalIgnoreCase);

        if (sourceOwnedSegments.Count >= 2)
        {
            var firstSource = sourceOwnedSegments[0];
            if (!request.AllowSourceOverride)
            {
                return Fail(result, string.Equals(firstSource.SourceType, "LEAVE", StringComparison.OrdinalIgnoreCase)
                    ? "ATTENDANCE_SOURCE_OWNED_LEAVE"
                    : "ATTENDANCE_SOURCE_OWNED_WFH");
            }
            if (string.Equals(firstSource.SourceType, "LEAVE", StringComparison.OrdinalIgnoreCase))
            {
                requiresLeaveReversal = true;
                leaveRequestIdToReverse = firstSource.SourceId;
            }
            else if (string.Equals(firstSource.SourceType, "WFH_REQUEST", StringComparison.OrdinalIgnoreCase) && !isWfhTargetStatus)
            {
                requiresWfhConversion = true;
                wfhRequestIdToConvert = firstSource.SourceId;
            }
        }
        else if (sourceOwnedSegments.Count == 1)
        {
            var owned = sourceOwnedSegments[0];
            // System automatic present (such as shift start trigger or default present initialization)
            // must NEVER fabricate punches or overwrite employees who are on approved Work From Home or Leave.
            if (AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(request.SourceType))
            {
                result.Success = true;
                result.MethodResult = new PreparedAttendanceMutation { Employee = employee, Attendance = existing, Existing = existing, ShouldWrite = false };
                return result;
            }

            if (request.AllowSourceOverride)
            {
                if (string.Equals(owned.SourceType, "LEAVE", StringComparison.OrdinalIgnoreCase))
                {
                    requiresLeaveReversal = true;
                    leaveRequestIdToReverse = owned.SourceId;
                }
                else if (string.Equals(owned.SourceType, "WFH_REQUEST", StringComparison.OrdinalIgnoreCase) && !isWfhTargetStatus)
                {
                    requiresWfhConversion = true;
                    wfhRequestIdToConvert = owned.SourceId;
                }
            }
            else
            {
                var complement = owned.Segment == "FIRST_HALF" ? "SECOND_HALF" : "FIRST_HALF";
                var existingComp = existingSegments.FirstOrDefault(x => x.Segment == complement);
                var compEntity = existingComp ?? new AttendanceDaySegment
                {
                    CompanyId = request.CompanyId,
                    UserId = employee.UserId,
                    EmployeeId = employee.EmployeeId,
                    Date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
                    Segment = complement
                };
                compEntity.Status = code;
                compEntity.SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "ADMIN_MANUAL" : request.SourceType.Trim();
                compEntity.CheckInTime = checkIn;
                compEntity.CheckOutTime = checkOut;
                compEntity.TotalHours = checkIn.HasValue && checkOut.HasValue ? Math.Max((decimal)(checkOut.Value - checkIn.Value).TotalHours, 0) : null;
                compEntity.Remarks = request.OperatorRemark ?? $"Worked segment complement to {owned.Segment} {owned.SourceType}";
                compEntity.UpdatedAtUtc = DateTime.UtcNow;

                if (string.IsNullOrEmpty(compEntity.AttendanceDaySegmentId))
                    await attendanceSegments.AddOne(compEntity);
                else
                    await attendanceSegments.Update(Builders<AttendanceDaySegment>.Filter.Eq(x => x.AttendanceDaySegmentId, compEntity.AttendanceDaySegmentId), compEntity);
            }
        }
        if (existing is not null)
        {
            if (!request.AllowSourceOverride)
            {
                if (string.Equals(existing.SourceType, "LEAVE", StringComparison.OrdinalIgnoreCase)) return Fail(result, "ATTENDANCE_SOURCE_OWNED_LEAVE");
                if (string.Equals(existing.SourceType, "WFH_REQUEST", StringComparison.OrdinalIgnoreCase)) return Fail(result, "ATTENDANCE_SOURCE_OWNED_WFH");
            }
            else
            {
                if (string.Equals(existing.SourceType, "LEAVE", StringComparison.OrdinalIgnoreCase))
                {
                    requiresLeaveReversal = true;
                    leaveRequestIdToReverse = existing.SourceId;
                }
                else if (string.Equals(existing.SourceType, "WFH_REQUEST", StringComparison.OrdinalIgnoreCase) && !isWfhTargetStatus)
                {
                    requiresWfhConversion = true;
                    wfhRequestIdToConvert = existing.SourceId;
                }
            }
            if (request.ExpectedAttendanceVersion.HasValue && existing.Version != request.ExpectedAttendanceVersion.Value) return Fail(result, "ATTENDANCE_VERSION_CONFLICT");
            if (request.ExistingRecordPolicy is ExistingAttendancePolicy.CreateMissingOnly or ExistingAttendancePolicy.SkipExisting)
            {
                result.Success = true;
                result.MethodResult = new PreparedAttendanceMutation { Employee = employee, Attendance = existing, Existing = existing, ShouldWrite = false };
                return result;
            }
        }

        if (sourceOwnedSegments.Any() && AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(request.SourceType))
        {
            result.Success = true;
            result.MethodResult = new PreparedAttendanceMutation { Employee = employee, Attendance = existing, Existing = existing, ShouldWrite = false };
            return result;
        }

        var row = existing ?? new AttendanceModel { CompanyId = request.CompanyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, Date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc) };
        row.Status = code;
        row.SourceType = request.AllowSourceOverride
            ? "ADMIN_OVERRIDE"
            : (string.IsNullOrWhiteSpace(request.SourceType) ? "ADMIN_MANUAL" : request.SourceType.Trim());
        row.SourceId = null; row.SourceVersion = 0;
        row.CheckInTime = checkIn; row.CheckOutTime = checkOut; row.BreakMinutes = schedule?.BreakMinutes ?? 0;
        row.TotalHours = checkIn.HasValue && checkOut.HasValue ? Math.Max((decimal)(checkOut.Value - checkIn.Value).TotalHours - (row.BreakMinutes / 60m), 0) : 0;
        row.LateCount = (code is "LHD" or "LC" or "LHD+ED") ? 1 : 0;
        row.EarlyExitCount = (code is "ED" or "LHD+ED") ? 1 : 0;
        row.SystemRemark = null;
        // Explicit status selection must use the same effective (company/department/employee)
        // schedule as automatic classification. ED is about leaving early; LHD is about arriving late.
        var grace = schedule is null ? TimeSpan.Zero : TimeSpan.FromMinutes(Math.Max(schedule.GraceMinutes, 0));
        var lateArrivalBoundary = schedule?.LateArrivalAfter ?? (schedule is null ? TimeSpan.Zero : schedule.StartTime.Add(grace));
        var earlyDepartureBoundary = schedule?.EarlyDepartureBefore ?? (schedule is null ? TimeSpan.Zero : schedule.EndTime.Subtract(grace));
        TimeSpan? halfDayCheckInBoundary = schedule?.HalfDayCheckInAfter
            ?? (schedule is not null && schedule.CheckInAllowedUntil.HasValue && schedule.CheckInAllowedUntil.Value > lateArrivalBoundary
                ? schedule.CheckInAllowedUntil.Value
                : (schedule is null ? null : schedule.StartTime.Add(TimeSpan.FromHours(1))));
        TimeSpan? halfDayCheckOutBoundary = schedule?.HalfDayCheckOutBefore
            ?? (schedule is not null && schedule.CheckOutAllowedFrom.HasValue && schedule.CheckOutAllowedFrom.Value < earlyDepartureBoundary
                ? schedule.CheckOutAllowedFrom.Value
                : (schedule is null ? null : schedule.EndTime.Subtract(TimeSpan.FromHours(1))));

        // When company/admin explicitly marked status (PreserveRequestedStatus == true),
        // respect their operational authority without blocking them or escalating to HD.
        if (!request.PreserveRequestedStatus)
        {
            if (schedule is not null && code == "ED" && (!request.CheckOutTime.HasValue || request.CheckOutTime.Value.ToTimeSpan() >= earlyDepartureBoundary))
                return Fail(result, "Check-out time must be earlier than the configured Early Departure boundary to mark Early Departure.");
            if (schedule is not null && code == "ED" && halfDayCheckOutBoundary.HasValue && request.CheckOutTime!.Value.ToTimeSpan() < halfDayCheckOutBoundary.Value)
            {
                var halfDay = await statuses.FirstOrDefault(x => x.CompanyId == request.CompanyId && x.Code == "HD" && x.IsActive);
                if (halfDay is null) return Fail(result, "Check-out is before the configured Half Day boundary and no active Half Day attendance status is configured.");
                code = halfDay.Code;
                row.Status = code;
                row.SystemRemark = "Check-out exceeded the configured Half Day boundary; normalized from Early Departure to Half Day.";
            }
            if (schedule is not null && code == "LHD" && (!request.CheckInTime.HasValue || request.CheckInTime.Value.ToTimeSpan() <= lateArrivalBoundary))
                return Fail(result, "Check-in time must be later than the configured Late Arrival boundary to mark Late Arrival.");
            if (schedule is not null && code == "LHD" && halfDayCheckInBoundary.HasValue && request.CheckInTime!.Value.ToTimeSpan() > halfDayCheckInBoundary.Value)
            {
                var halfDay = await statuses.FirstOrDefault(x => x.CompanyId == request.CompanyId && x.Code == "HD" && x.IsActive);
                if (halfDay is null) return Fail(result, "Check-in is after the configured Half Day boundary and no active Half Day attendance status is configured.");
                code = halfDay.Code;
                row.Status = code;
                row.SystemRemark = "Check-in exceeded the configured Half Day boundary; normalized from Late Arrival to Half Day.";
            }
            if (schedule is not null && code == "LHD+ED")
            {
                if (!request.CheckInTime.HasValue || request.CheckInTime.Value.ToTimeSpan() <= lateArrivalBoundary ||
                    !request.CheckOutTime.HasValue || request.CheckOutTime.Value.ToTimeSpan() >= earlyDepartureBoundary)
                    return Fail(result, "Late Arrival-Half Day + Early Departure requires a late check-in and an early check-out for the effective office schedule.");

                var tooLateForCombinedStatus = halfDayCheckInBoundary.HasValue && request.CheckInTime.Value.ToTimeSpan() > halfDayCheckInBoundary.Value;
                var tooEarlyForCombinedStatus = halfDayCheckOutBoundary.HasValue && request.CheckOutTime.Value.ToTimeSpan() < halfDayCheckOutBoundary.Value;
                if (tooLateForCombinedStatus || tooEarlyForCombinedStatus)
                {
                    var halfDay = await statuses.FirstOrDefault(x => x.CompanyId == request.CompanyId && x.Code == "HD" && x.IsActive);
                    if (halfDay is null) return Fail(result, "The LHD+ED time is outside the configured attendance boundary and no active Half Day attendance status is configured.");

                    code = halfDay.Code;
                    row.Status = code;
                    row.SystemRemark = "LHD+ED was normalized to Half Day because a configured schedule boundary was exceeded.";
                }
            }
        }
        if (code == "P" && request.TimingMode == AttendanceTimingMode.Custom && schedule is not null)
        {
            var derivedCode = statusCalculation.CalculateOfficeStatus(schedule, request.CheckInTime!.Value, request.CheckOutTime!.Value, row.TotalHours ?? 0m);
            if (derivedCode != code)
            {
                var derivedStatus = await statuses.FirstOrDefault(x => x.CompanyId == request.CompanyId && x.Code == derivedCode && x.IsActive);
                if (derivedStatus is not null)
                {
                    status = derivedStatus;
                    code = derivedCode;
                    row.Status = code;
                    row.LateCount = (code is "LHD" or "LC" or "LHD+ED") ? 1 : 0;
                    row.EarlyExitCount = (code is "ED" or "LHD+ED") ? 1 : 0;
                    row.SystemRemark = $"Automatically classified as {code} from the effective office schedule.";
                }
            }
        }
        row.ScheduleId = schedule?.ScheduleId; row.ScheduleVersion = schedule?.Version;
        row.RemarkCode = request.RemarkCode; row.OperatorRemark = request.OperatorRemark?.Trim();
        row.SystemRemark ??= schedule is null ? "Manual attendance entry" : $"Applied schedule {schedule.Name} v{schedule.Version}";
        row.Remarks = row.OperatorRemark ?? row.SystemRemark; row.ModifiedByUserId = request.ActorUserId; row.ModifiedAtUtc = DateTime.UtcNow;
        if (existing is null)
        {
            row.AttendanceId = ObjectId.GenerateNewId().ToString();
            row.MarkedByUserId = request.ActorUserId; row.MarkedAtUtc = row.ModifiedAtUtc; row.Version = 1;
        }
        else row.Version++;
        result.Success = true;
        result.MethodResult = new PreparedAttendanceMutation
        {
            Employee = employee,
            Attendance = row,
            Existing = existing,
            ShouldWrite = true,
            RequiresLeaveReversal = requiresLeaveReversal,
            LeaveRequestIdToReverse = leaveRequestIdToReverse,
            RequiresWfhConversion = requiresWfhConversion,
            WfhRequestIdToConvert = wfhRequestIdToConvert
        };
        return result;
    }

    private static Result<PreparedAttendanceMutation> Fail(Result<PreparedAttendanceMutation> result, string message)
    {
        result.Message = message switch
        {
            "ATTENDANCE_MUTATION_INVALID" => "Attendance details are incomplete or invalid.",
            "ATTENDANCE_FUTURE_DATE_NOT_ALLOWED" => "Attendance cannot be marked for a future date.",
            "ATTENDANCE_TARGET_UNAVAILABLE" => "The selected employee is not available in this company.",
            "ATTENDANCE_BEFORE_JOINING" => "Attendance cannot be marked before the employee's joining date.",
            "ATTENDANCE_AFTER_EXIT" => "Attendance cannot be marked after the employee's exit date.",
            "ATTENDANCE_NOT_EXPECTED_FOR_LIFECYCLE" => "Normal attendance is not expected for this employee on the selected date because of their current leave, sabbatical, suspension, or temporary absence status.",
            "ATTENDANCE_STATUS_INVALID" => "The selected attendance status is not available for this company.",
            "ATTENDANCE_TIMING_REQUIRED" => "Check-in and check-out times are required for the selected attendance status.",
            "ATTENDANCE_STATUS_DOES_NOT_ACCEPT_TIMING" => "Check-in and check-out times are not applicable for the selected attendance status.",
            "ATTENDANCE_VERSION_CONFLICT" => "Attendance was updated by another user. Refresh and try again.",
            "ATTENDANCE_SOURCE_OWNED_LEAVE" => "This attendance is managed by an approved leave request.",
            "ATTENDANCE_SOURCE_OWNED_WFH" => "This date includes attendance managed by an approved work-from-home request. Recalculate the monthly exceptions, then use Edit on the WFH hours exception to correct only the WFH half; the generic correction cannot overwrite source-owned time.",
            "ATTENDANCE_OVERRIDE_REASON_REQUIRED" => "An override requires an explanatory justification of at least 10 characters.",
            _ => message
        };
        return result;
    }

    public static DateTime ToScheduleUtc(DateTime attendanceDate, TimeSpan localTime, string? timeZoneId)
    {
        var localDateTime = DateTime.SpecifyKind(attendanceDate.Date.Add(localTime), DateTimeKind.Unspecified);
        try
        {
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Kolkata" : timeZoneId));
        }
        catch (TimeZoneNotFoundException) when (!string.Equals(timeZoneId, "India Standard Time", StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
        }
        catch (InvalidTimeZoneException) when (!string.Equals(timeZoneId, "India Standard Time", StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
        }
    }
}
