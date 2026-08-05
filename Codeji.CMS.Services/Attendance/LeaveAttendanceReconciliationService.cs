using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.Attendance;
using MongoDB.Driver;
using Codeji.CMS.Services.Attendance;

public interface ILeaveAttendanceReconciliationService
{
    Task<Result> ReconcileAcceptedLeaveAsync(string companyId, string leaveRequestId, string actorUserId, CancellationToken cancellationToken = default);
    Task<Result> ReverseLeaveAttendanceAsync(string companyId, string leaveRequestId, int expectedSourceVersion, string actorUserId, CancellationToken cancellationToken = default);
}

public sealed class LeaveAttendanceReconciliationService(
    IMongoDbRepository<LeaveRequest> leaves,
    IMongoDbRepository<LeavePolicy> policies,
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<AttendanceStatusSetting> statuses,
    IMongoDbRepository<AttendanceModel> attendance,
    IMongoDbRepository<AttendanceDaySegment> segments,
    IMongoDbRepository<AttendancePayrollException> exceptions,
    IMongoDbRepository<MonthlyAttendanceSummary> summaries,
    ICompanyWorkingCalendarService workingCalendar,
    IAttendanceInitializationService initialization) : ILeaveAttendanceReconciliationService
{
    public async Task<Result> ReconcileAcceptedLeaveAsync(string companyId, string leaveRequestId, string actorUserId, CancellationToken cancellationToken = default)
    {
        var result = new Result { Success = false };
        var request = await leaves.FirstOrDefault(x => x.CompanyId == companyId && x.LeaveRequestId == leaveRequestId);
        if (request == null)
        {
            result.Message = "Leave request was not found in the authenticated company.";
            return result;
        }
        if (request.Status != Codeji.CMS.Utility.Enums.EnumsHelper.LeaveRequestStatus.Accepted)
        {
            result.Message = "Only approved leave requests can be reconciled to attendance.";
            return result;
        }

        var employee = await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.EmployeeId && x.Status);
        var policy = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == request.LeavePolicyId && x.Status);
        if (employee == null)
        {
            result.Message = "The employee for this leave request is not active in the authenticated company.";
            return result;
        }
        if (policy == null)
        {
            result.Message = "The leave policy for this request is not active.";
            return result;
        }
        if (string.IsNullOrWhiteSpace(policy.AttendanceStatusCode))
        {
            result.Message = "The leave policy must have an attendance status code before the request can be approved.";
            return result;
        }

        var statusCode = policy.AttendanceStatusCode.Trim().ToUpperInvariant();
        var status = await statuses.FirstOrDefault(x => x.CompanyId == companyId && x.Code == statusCode && x.IsActive);
        if (status == null || status.RequiresTime)
        {
            result.Message = "The leave policy attendance status must be an active status that does not require check-in or check-out times.";
            return result;
        }

        var dates = await workingCalendar.GetLeaveDatesAsync(companyId, DateOnly.FromDateTime(request.StartDate),
            DateOnly.FromDateTime(request.EndDate), policy.WeekendInclusive, policy.HolidayInclusive, cancellationToken);

        if (request.IsHalfDay)
            return await ReconcileHalfDayAsync(companyId, request, employee, statusCode, actorUserId, cancellationToken);

        // A source-linked attendance row is owned by this leave request. Remove only
        // rows that no longer belong to its current date range before rebuilding the
        // mapping. This repairs stale rows created by a prior incorrect mapping without
        // touching manually-recorded attendance or another leave request's rows.
        var leaveDates = dates.Select(UtcMidnight).ToHashSet();
        var ownedRows = await attendance.GetAll(x => x.CompanyId == companyId &&
            x.UserId == employee.UserId && x.SourceType == "LEAVE" && x.SourceId == request.LeaveRequestId);
        foreach (var ownedRow in ownedRows.Where(row => !leaveDates.Contains(row.Date.Date)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await attendance.Delete(Builders<AttendanceModel>.Filter.Eq(x => x.AttendanceId, ownedRow.AttendanceId));
        }

        var hasConflict = false;
        foreach (var day in dates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = UtcMidnight(day);
            var existing = await attendance.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Date >= date && x.Date < date.AddDays(1));
            if (existing != null && !(existing.SourceType == "LEAVE" && existing.SourceId == request.LeaveRequestId) && !AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(existing.SourceType))
            {
                hasConflict = true;
                var month = new DateTime(date.Year, date.Month, 1);
                var conflict = await exceptions.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && x.ExceptionType == "LEAVE_ATTENDANCE_CONFLICT" && x.AttendanceDate == date && x.LeaveRequestId == request.LeaveRequestId);
                if (conflict == null)
                    await exceptions.AddOne(new AttendancePayrollException { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, PayrollMonth = month, AttendanceDate = date, AffectedDates = [date], ExceptionType = "LEAVE_ATTENDANCE_CONFLICT", Severity = "BLOCKING", Status = "PENDING_REVIEW", LeaveRequestId = request.LeaveRequestId, ExistingAttendanceId = existing.AttendanceId, ExistingStatus = existing.Status, RequestedLeaveStatus = statusCode, SourceVersion = request.Version, Reason = "Approved leave conflicts with existing attendance.", CreatedBy = actorUserId });
                continue;
            }

            var entity = existing ?? new AttendanceModel { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, Date = date };
            entity.Status = statusCode;
            entity.SourceType = "LEAVE";
            entity.SourceId = request.LeaveRequestId;
            entity.SourceVersion = request.Version;
            entity.CheckInTime = null;
            entity.CheckOutTime = null;
            entity.TotalHours = null;
            entity.Remarks = $"Generated from approved leave {request.LeaveRequestId}";
            if (existing == null) await attendance.AddOne(entity);
            else await attendance.Update(Builders<AttendanceModel>.Filter.Eq(x => x.AttendanceId, existing.AttendanceId), entity);
        }

        var affectedMonths = dates.Select(x => new DateTime(x.Year, x.Month, 1)).Distinct().ToList();
        foreach (var month in affectedMonths)
            await summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && !x.IsLocked));
        var now = DateTime.UtcNow;
        await leaves.GetCollection().UpdateOneAsync(
            x => x.CompanyId == companyId && x.LeaveRequestId == leaveRequestId && x.Version == request.Version,
            Builders<LeaveRequest>.Update
                .Set(x => x.ReconciliationStatus, hasConflict ? "Conflict" : "Completed")
                .Set(x => x.ReconciliationErrorCode, hasConflict ? "LEAVE_ATTENDANCE_CONFLICT" : null)
                .Set(x => x.ReconciliationErrorMessage, hasConflict ? "One or more leave dates conflict with source-owned attendance and require review." : null)
                .Set(x => x.LastReconciliationAttemptAtUtc, now)
                .Set(x => x.NextReconciliationAttemptAtUtc, null)
                .Set(x => x.ReconciledAtUtc, hasConflict ? null : now)
                .Inc(x => x.ReconciliationAttempts, 1));
        result.Success = true;
        result.Message = hasConflict ? "LEAVE_ATTENDANCE_CONFLICT: Attendance synchronization requires review." : "Attendance synchronization completed.";
        return result;
    }

    public async Task<Result> ReverseLeaveAttendanceAsync(string companyId, string leaveRequestId, int expectedSourceVersion, string actorUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var request = await leaves.FirstOrDefault(x => x.CompanyId == companyId && x.LeaveRequestId == leaveRequestId);
        var filter = Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == companyId && x.SourceType == "LEAVE" && x.SourceId == leaveRequestId && x.SourceVersion <= expectedSourceVersion);
        var result = await attendance.DeleteAll(filter);
        var segmentResult = await segments.DeleteAll(Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == companyId && x.SourceType == "LEAVE" && x.SourceId == leaveRequestId && x.SourceVersion <= expectedSourceVersion));
        if (!segmentResult.Success) return segmentResult;
        if (!result.Success || request == null) return result;

        var affectedMonths = Enumerable.Range(0, ((request.EndDate.Date - request.StartDate.Date).Days) + 1)
            .Select(offset => request.StartDate.Date.AddDays(offset))
            .Select(date => new DateTime(date.Year, date.Month, 1))
            .Distinct();
        foreach (var month in affectedMonths)
        {
            await summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == companyId && x.UserId == request.EmployeeId && x.PayrollMonth == month && !x.IsLocked));
            await initialization.InitializeMonthAsync(companyId, actorUserId, month, cancellationToken);
        }
        await leaves.GetCollection().UpdateOneAsync(
            x => x.CompanyId == companyId && x.LeaveRequestId == leaveRequestId && x.Version == request.Version,
            Builders<LeaveRequest>.Update
                .Set(x => x.ReconciliationStatus, "Reversed")
                .Set(x => x.ReversedAtUtc, DateTime.UtcNow)
                .Set(x => x.LastReconciliationAttemptAtUtc, DateTime.UtcNow)
                .Set(x => x.NextReconciliationAttemptAtUtc, null)
                .Set(x => x.ReconciliationErrorCode, null)
                .Set(x => x.ReconciliationErrorMessage, null)
                .Inc(x => x.ReconciliationAttempts, 1));
        return result;
    }

    private static DateTime UtcMidnight(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private async Task<Result> ReconcileHalfDayAsync(string companyId, LeaveRequest request, EmpUser employee, string statusCode, string actorUserId, CancellationToken cancellationToken)
    {
        var date = UtcMidnight(DateOnly.FromDateTime(request.StartDate));
        var segmentName = request.HalfDayPeriod == "SECOND_HALF" ? "SECOND_HALF" : "FIRST_HALF";
        var existing = await segments.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Date >= date && x.Date < date.AddDays(1) && x.Segment == segmentName);
        if (existing is not null && !(existing.SourceType == "LEAVE" && existing.SourceId == request.LeaveRequestId))
        {
            var month = new DateTime(date.Year, date.Month, 1);
            if (!await exceptions.Exist(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && x.ExceptionType == "LEAVE_ATTENDANCE_SEGMENT_CONFLICT" && x.AttendanceDate == date && x.LeaveRequestId == request.LeaveRequestId))
                await exceptions.AddOne(new AttendancePayrollException { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, PayrollMonth = month, AttendanceDate = date, AffectedDates = [date], ExceptionType = "LEAVE_ATTENDANCE_SEGMENT_CONFLICT", Severity = "BLOCKING", Status = "PENDING_REVIEW", LeaveRequestId = request.LeaveRequestId, ExistingAttendanceId = existing.AttendanceDaySegmentId, ExistingStatus = existing.Status, RequestedLeaveStatus = statusCode, SourceVersion = request.Version, Reason = $"Approved leave conflicts with the {segmentName} attendance segment.", CreatedBy = actorUserId });
            return new Result { Success = true, Message = "LEAVE_ATTENDANCE_SEGMENT_CONFLICT: Attendance synchronization requires review." };
        }
        var entity = existing ?? new AttendanceDaySegment { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, Date = date, Segment = segmentName };
        entity.Status = statusCode; entity.SourceType = "LEAVE"; entity.SourceId = request.LeaveRequestId; entity.SourceVersion = request.Version; entity.CheckInTime = null; entity.CheckOutTime = null; entity.TotalHours = null; entity.Remarks = $"Generated from approved half-day leave {request.LeaveRequestId}"; entity.UpdatedAtUtc = DateTime.UtcNow;
        var saved = string.IsNullOrEmpty(entity.AttendanceDaySegmentId) ? await segments.AddOne(entity) : await segments.Update(Builders<AttendanceDaySegment>.Filter.Eq(x => x.AttendanceDaySegmentId, entity.AttendanceDaySegmentId), entity);
        if (!saved.Success) return saved;
        await summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == new DateTime(date.Year, date.Month, 1) && !x.IsLocked));
        await leaves.GetCollection().UpdateOneAsync(x => x.CompanyId == companyId && x.LeaveRequestId == request.LeaveRequestId && x.Version == request.Version,
            Builders<LeaveRequest>.Update.Set(x => x.ReconciliationStatus, "Completed").Set(x => x.ReconciliationErrorCode, null).Set(x => x.ReconciliationErrorMessage, null).Set(x => x.ReconciledAtUtc, DateTime.UtcNow).Set(x => x.LastReconciliationAttemptAtUtc, DateTime.UtcNow).Inc(x => x.ReconciliationAttempts, 1));
        return new Result { Success = true, Message = "Half-day leave attendance synchronization completed." };
    }
}
