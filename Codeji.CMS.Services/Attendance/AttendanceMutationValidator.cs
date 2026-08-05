using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
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
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<AttendanceStatusSetting> statuses) : IAttendanceMutationValidator
{
    public async Task<Result<PreparedAttendanceMutation>> PrepareAsync(AttendanceMutationRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<PreparedAttendanceMutation> { Success = false };
        if (string.IsNullOrWhiteSpace(request.CompanyId) || string.IsNullOrWhiteSpace(request.ActorUserId) || string.IsNullOrWhiteSpace(request.TargetUserId) || string.IsNullOrWhiteSpace(request.StatusCode))
            return Fail(result, "ATTENDANCE_MUTATION_INVALID");

        var date = DateTime.SpecifyKind(request.AttendanceDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        if (date.Date > DateTime.UtcNow.Date)
            return Fail(result, "ATTENDANCE_FUTURE_DATE_NOT_ALLOWED");
        var employee = await employees.FirstOrDefault(x => x.CompanyId == request.CompanyId && x.UserId == request.TargetUserId && x.Status && !x.IsDeleted);
        if (employee is null) return Fail(result, "ATTENDANCE_TARGET_UNAVAILABLE");
        if (DateTime.TryParse(employee.DateOfJoining, out var joined) && date.Date < joined.Date) return Fail(result, "ATTENDANCE_BEFORE_JOINING");
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
                if (schedule is null) return Fail(result, "ATTENDANCE_SCHEDULE_NOT_FOUND");
                checkIn = ToScheduleUtc(date, schedule.StartTime, schedule.TimeZoneId);
                checkOut = ToScheduleUtc(date, schedule.EndTime, schedule.TimeZoneId);
                if (checkOut <= checkIn) checkOut = checkOut.Value.AddDays(1);
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
                if (checkOut <= checkIn) checkOut = checkOut.Value.AddDays(1);
            }
            else return Fail(result, "ATTENDANCE_TIMING_REQUIRED");
        }
        // Callers may use Auto when they do not know whether a status needs times.
        // Explicit custom times remain invalid for statuses such as leave/absence.
        else if (request.TimingMode == AttendanceTimingMode.Custom) return Fail(result, "ATTENDANCE_STATUS_DOES_NOT_ACCEPT_TIMING");

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
        if (code == "P" && !request.PreserveRequestedStatus && request.TimingMode == AttendanceTimingMode.Custom && schedule is not null)
        {
            var checkInTime = request.CheckInTime!.Value.ToTimeSpan();
            var checkOutTime = request.CheckOutTime!.Value.ToTimeSpan();
            // Early arrival and late checkout remain outside the configured attendance
            // window. Late arrival and early departure are valid attendance events and
            // are classified below instead of producing a 400/validation failure.
            if (schedule.CheckInAllowedFrom.HasValue && checkInTime < schedule.CheckInAllowedFrom.Value ||
                schedule.CheckOutAllowedUntil.HasValue && checkOutTime > schedule.CheckOutAllowedUntil.Value)
                return Fail(result, "ATTENDANCE_OUTSIDE_OFFICE_WINDOW");
            var earlyDeparture = checkOutTime < schedule.EndTime || row.TotalHours < (schedule.RequiredWorkingMinutes <= 0 ? 480 : schedule.RequiredWorkingMinutes) / 60m;
            var lateArrival = checkInTime > schedule.StartTime;
            var derivedCode = earlyDeparture ? "ED" : lateArrival ? "LHD" : "P";
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
        row.SystemRemark = schedule is null ? "Manual attendance entry" : $"Applied schedule {schedule.Name} v{schedule.Version}";
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

    private static Result<PreparedAttendanceMutation> Fail(Result<PreparedAttendanceMutation> result, string message) { result.Message = message; return result; }

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
