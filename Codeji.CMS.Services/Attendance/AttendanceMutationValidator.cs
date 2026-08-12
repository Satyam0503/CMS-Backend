using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Bson;

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
}

public sealed class PreparedAttendanceMutation
{
    public required EmpUser Employee { get; init; }
    public required AttendanceModel Attendance { get; init; }
    public AttendanceModel? Existing { get; init; }
    public bool ShouldWrite { get; init; }
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
    IMongoDbRepository<AttendanceStatusSetting> statuses) : IAttendanceMutationValidator
{
    public async Task<Result<PreparedAttendanceMutation>> PrepareAsync(AttendanceMutationRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<PreparedAttendanceMutation> { Success = false };
        if (string.IsNullOrWhiteSpace(request.CompanyId) || string.IsNullOrWhiteSpace(request.ActorUserId) || string.IsNullOrWhiteSpace(request.TargetUserId) || string.IsNullOrWhiteSpace(request.StatusCode))
            return Fail(result, "ATTENDANCE_MUTATION_INVALID");

        var date = DateTime.SpecifyKind(request.AttendanceDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        if (DateOnly.FromDateTime(date) > DateOnly.FromDateTime(IndiaTime.Today))
            return Fail(result, "ATTENDANCE_FUTURE_DATE_NOT_ALLOWED");
        var employee = await employees.FirstOrDefault(x => x.CompanyId == request.CompanyId && x.UserId == request.TargetUserId && x.Status && !x.IsDeleted);
        if (employee is null) return Fail(result, "ATTENDANCE_TARGET_UNAVAILABLE");
        if (DateTime.TryParse(employee.DateOfJoining, out var joined) && date.Date < joined.Date)
            return Fail(result, $"Attendance cannot be marked before the employee's joining date of {joined:dd-MMM-yyyy}.");
        if (DateTime.TryParse(employee.ExitDate, out var exited) && date.Date > exited.Date) return Fail(result, "ATTENDANCE_AFTER_EXIT");

        try { await editGuard.EnsureEditableWorkingDayAsync(request.CompanyId, request.TargetUserId, date); }
        catch (InvalidOperationException ex) { return Fail(result, ex.Message); }

        var code = request.StatusCode.Trim().ToUpperInvariant();
        var status = await statuses.FirstOrDefault(x => x.CompanyId == request.CompanyId && x.Code == code && x.IsActive);
        if (status is null) return Fail(result, "ATTENDANCE_STATUS_INVALID");

        DateTime? checkIn = null, checkOut = null;
        CompanyOfficeSchedule? schedule = null;
        if (status.RequiresTime)
        {
            schedule = await schedules.ResolveAsync(request.CompanyId, employee, date, cancellationToken);
            if (request.TimingMode == AttendanceTimingMode.Auto)
            {
                // System-generated Present rows carry the effective schedule's planned
                // punch window.  This is tenant/employee schedule-derived (for example
                // 09:00–18:00), never a frontend or global hard-coded value.
                if (schedule is not null)
                {
                    checkIn = ToScheduleUtc(date, schedule.StartTime, schedule.TimeZoneId);
                    checkOut = ToScheduleUtc(date, schedule.EndTime, schedule.TimeZoneId);
                }
            }
            else if (request.TimingMode == AttendanceTimingMode.Custom)
            {
                if (!request.CheckInTime.HasValue || !request.CheckOutTime.HasValue) return Fail(result, "ATTENDANCE_TIMING_REQUIRED");
                checkIn = schedule is null
                    ? date.Add(request.CheckInTime.Value.ToTimeSpan())
                    : ToScheduleUtc(date, request.CheckInTime.Value.ToTimeSpan(), schedule.TimeZoneId);
                checkOut = schedule is null
                    ? date.Add(request.CheckOutTime.Value.ToTimeSpan())
                    : ToScheduleUtc(date, request.CheckOutTime.Value.ToTimeSpan(), schedule.TimeZoneId);
                if (checkOut <= checkIn)
                    return Fail(result, "Check-out time cannot be earlier than or equal to check-in time.");
            }
            else return Fail(result, "ATTENDANCE_TIMING_REQUIRED");
        }
        else
        {
            // The selected status owns its timing rules.  A stale editor/API payload must
            // never prevent Present -> Absent (or another non-timed transition), and it
            // must never leave former Present clock events on the persisted row.
            checkIn = null;
            checkOut = null;
        }

        var existing = await attendance.GetByUserAndDateAsync(request.CompanyId, request.TargetUserId, date);
        if (existing is not null)
        {
            if (string.Equals(existing.SourceType, "LEAVE", StringComparison.OrdinalIgnoreCase)) return Fail(result, "ATTENDANCE_SOURCE_OWNED_LEAVE");
            if (string.Equals(existing.SourceType, "WFH_REQUEST", StringComparison.OrdinalIgnoreCase)) return Fail(result, "ATTENDANCE_SOURCE_OWNED_WFH");
            if (request.ExpectedAttendanceVersion.HasValue && existing.Version != request.ExpectedAttendanceVersion.Value) return Fail(result, "ATTENDANCE_VERSION_CONFLICT");
            if (request.ExistingRecordPolicy is ExistingAttendancePolicy.CreateMissingOnly or ExistingAttendancePolicy.SkipExisting)
            {
                result.Success = true;
                result.MethodResult = new PreparedAttendanceMutation { Employee = employee, Attendance = existing, Existing = existing, ShouldWrite = false };
                return result;
            }
        }

        var row = existing ?? new AttendanceModel { CompanyId = request.CompanyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, Date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc) };
        row.Status = code; row.SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "ADMIN_MANUAL" : request.SourceType.Trim(); row.SourceId = null; row.SourceVersion = 0;
        row.CheckInTime = checkIn; row.CheckOutTime = checkOut; row.BreakMinutes = schedule?.BreakMinutes ?? 0;
        row.TotalHours = checkIn.HasValue && checkOut.HasValue ? Math.Max((decimal)(checkOut.Value - checkIn.Value).TotalHours - (row.BreakMinutes / 60m), 0) : 0;
        row.SystemRemark = null;
        // Explicit status selection must use the same effective (company/department/employee)
        // schedule as automatic classification. ED is about leaving early; LHD is about arriving late.
        if (schedule is not null && code == "ED" && (!request.CheckOutTime.HasValue || request.CheckOutTime.Value.ToTimeSpan() >= schedule.EndTime))
            return Fail(result, "Check-out time must be earlier than the configured office closing time to mark Early Departure.");
        if (schedule is not null && code == "ED" && schedule.CheckOutAllowedFrom.HasValue && request.CheckOutTime!.Value.ToTimeSpan() < schedule.CheckOutAllowedFrom.Value)
        {
            var halfDay = await statuses.FirstOrDefault(x => x.CompanyId == request.CompanyId && x.Code == "HD" && x.IsActive);
            if (halfDay is null) return Fail(result, "Check-out is before the configured Half Day boundary and no active Half Day attendance status is configured.");
            code = halfDay.Code;
            row.Status = code;
            row.SystemRemark = "Check-out exceeded the configured Half Day boundary; normalized from Early Departure to Half Day.";
        }
        if (schedule is not null && code == "LHD" && (!request.CheckInTime.HasValue || request.CheckInTime.Value.ToTimeSpan() <= schedule.StartTime))
            return Fail(result, "Check-in time must be later than the configured office start time to mark Late Arrival.");
        if (schedule is not null && code == "LHD" && schedule.CheckInAllowedUntil.HasValue && request.CheckInTime!.Value.ToTimeSpan() > schedule.CheckInAllowedUntil.Value)
        {
            var halfDay = await statuses.FirstOrDefault(x => x.CompanyId == request.CompanyId && x.Code == "HD" && x.IsActive);
            if (halfDay is null) return Fail(result, "Check-in is after the configured Half Day boundary and no active Half Day attendance status is configured.");
            code = halfDay.Code;
            row.Status = code;
            row.SystemRemark = "Check-in exceeded the configured Half Day boundary; normalized from Late Arrival to Half Day.";
        }
        if (schedule is not null && code == "LHD+ED")
        {
            if (!request.CheckInTime.HasValue || request.CheckInTime.Value.ToTimeSpan() <= schedule.StartTime ||
                !request.CheckOutTime.HasValue || request.CheckOutTime.Value.ToTimeSpan() >= schedule.EndTime)
                return Fail(result, "Late Arrival-Half Day + Early Departure requires a late check-in and an early check-out for the effective office schedule.");

            // Escalation boundaries are schedule-owned.  No global one-hour rule is
            // applied when a company has not configured these limits.
            var tooLateForCombinedStatus = schedule.CheckInAllowedUntil.HasValue && request.CheckInTime.Value.ToTimeSpan() > schedule.CheckInAllowedUntil.Value;
            var tooEarlyForCombinedStatus = schedule.CheckOutAllowedFrom.HasValue && request.CheckOutTime.Value.ToTimeSpan() < schedule.CheckOutAllowedFrom.Value;
            if (tooLateForCombinedStatus || tooEarlyForCombinedStatus)
            {
                var halfDay = await statuses.FirstOrDefault(x => x.CompanyId == request.CompanyId && x.Code == "HD" && x.IsActive);
                if (halfDay is null) return Fail(result, "The LHD+ED time is outside the configured attendance boundary and no active Half Day attendance status is configured.");

                code = halfDay.Code;
                row.Status = code;
                row.SystemRemark = "LHD+ED was normalized to Half Day because a configured schedule boundary was exceeded.";
            }
        }
        if (code == "P" && !request.PreserveRequestedStatus && request.TimingMode == AttendanceTimingMode.Custom && schedule is not null)
        {
            var checkInTime = request.CheckInTime!.Value.ToTimeSpan();
            var checkOutTime = request.CheckOutTime!.Value.ToTimeSpan();
            // Early arrival and late checkout remain outside the configured attendance
            // window. Late arrival and early departure are valid attendance events and
            // are classified below instead of producing a 400/validation failure.
            var earliestAllowedCheckIn = schedule.CheckInAllowedFrom ?? schedule.StartTime;
            if (checkInTime < earliestAllowedCheckIn ||
                schedule.CheckOutAllowedUntil.HasValue && checkOutTime > schedule.CheckOutAllowedUntil.Value)
                return Fail(result, checkInTime < earliestAllowedCheckIn
                    ? $"Check-in time cannot be earlier than the allowed check-in time of {earliestAllowedCheckIn:hh\\:mm}."
                    : "Check-out time is later than the allowed check-out time for this schedule.");
            var derivedCode = statusCalculation.CalculateOfficeStatus(schedule, request.CheckInTime.Value, request.CheckOutTime.Value, row.TotalHours ?? 0m);
            if (derivedCode != code)
            {
                status = await statuses.FirstOrDefault(x => x.CompanyId == request.CompanyId && x.Code == derivedCode && x.IsActive);
                if (status is null) return Fail(result, $"ATTENDANCE_STATUS_CONFIGURATION_MISSING_{derivedCode}");
                code = derivedCode;
                row.Status = code;
                row.SystemRemark = $"Automatically classified as {code} from the effective office schedule.";
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
        result.Success = true; result.MethodResult = new PreparedAttendanceMutation { Employee = employee, Attendance = row, Existing = existing, ShouldWrite = true };
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
            "ATTENDANCE_STATUS_INVALID" => "The selected attendance status is not available for this company.",
            "ATTENDANCE_TIMING_REQUIRED" => "Check-in and check-out times are required for the selected attendance status.",
            "ATTENDANCE_STATUS_DOES_NOT_ACCEPT_TIMING" => "Check-in and check-out times are not applicable for the selected attendance status.",
            "ATTENDANCE_VERSION_CONFLICT" => "Attendance was updated by another user. Refresh and try again.",
            "ATTENDANCE_SOURCE_OWNED_LEAVE" => "This attendance is managed by an approved leave request.",
            "ATTENDANCE_SOURCE_OWNED_WFH" => "This attendance is managed by an approved work-from-home request.",
            _ => message
        };
        return result;
    }

    private static DateTime ToScheduleUtc(DateTime attendanceDate, TimeSpan localTime, string? timeZoneId)
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
