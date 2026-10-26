using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.Attendance;
using MongoDB.Driver;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees;
using Microsoft.AspNetCore.Http;

public interface ILeaveAttendanceReconciliationService
{
    Task<Result> ReconcileAcceptedLeaveAsync(string companyId, string leaveRequestId, string actorUserId, CancellationToken cancellationToken = default);
    Task<Result> ReconcileAcceptedLeaveInTransactionAsync(IClientSessionHandle session, string companyId, LeaveRequest request, string actorUserId, CancellationToken cancellationToken = default);
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
    public async Task<Result> ReconcileAcceptedLeaveInTransactionAsync(IClientSessionHandle session, string companyId, LeaveRequest request, string actorUserId, CancellationToken cancellationToken = default)
    {
        if (request.CompanyId != companyId || request.Status != Codeji.CMS.Utility.Enums.EnumsHelper.LeaveRequestStatus.Accepted ||
            string.IsNullOrWhiteSpace(request.EmployeeId) || string.IsNullOrWhiteSpace(request.LeaveRequestId))
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_SOURCE_CHANGED: The leave source is no longer an accepted request in this company." };

        var employee = await employees.GetCollection().Find(session,
            x => x.CompanyId == companyId && x.UserId == request.EmployeeId && !x.IsDeleted).FirstOrDefaultAsync(cancellationToken);
        var policy = await policies.GetCollection().Find(session,
            x => x.CompanyId == companyId && x.Id == request.LeavePolicyId && x.Status).FirstOrDefaultAsync(cancellationToken);
        if (employee is null || policy is null)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_POLICY_UNAVAILABLE: The employee or active leave policy is unavailable." };

        var unpaid = policy.PolicyType == Codeji.CMS.Utility.Enums.EnumsHelper.LeavePolicyType.UnpaidLeave ||
            (string.Equals(policy.Code?.Trim(), "UL", StringComparison.OrdinalIgnoreCase) && !policy.Paid);
        var statusCode = request.IsHalfDay && unpaid
            ? "HD"
            : request.IsHalfDay
                ? policy.HalfDayAttendanceStatusCode ?? policy.FullDayAttendanceStatusCode ?? policy.AttendanceStatusCode
                : policy.FullDayAttendanceStatusCode ?? policy.AttendanceStatusCode;
        statusCode = statusCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var status = await statuses.GetCollection().Find(session,
            x => x.CompanyId == companyId && x.Code == statusCode && x.IsActive && !x.RequiresTime).FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(statusCode) || status is null)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_STATUS_UNAVAILABLE: The leave attendance status is not active or does not support leave attendance." };

        List<DateOnly> dates;
        if (request.IsHalfDay)
            dates = [DateOnly.FromDateTime(request.StartDate)];
        else
            dates = (await workingCalendar.GetLeaveDatesAsync(companyId, DateOnly.FromDateTime(request.StartDate),
                DateOnly.FromDateTime(request.EndDate), policy.WeekendInclusive, policy.HolidayInclusive, cancellationToken)).ToList();
        dates = dates.Where(day => EmploymentPeriod.WasEmployedOn(employee, day.ToDateTime(TimeOnly.MinValue))).ToList();
        if (dates.Count == 0)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_DATES_UNAVAILABLE: No leave date remains within the employee's active employment period." };

        var months = dates.Select(day => new DateTime(day.Year, day.Month, 1)).Distinct().ToList();
        var locked = await summaries.GetCollection().Find(session,
            Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId &&
                months.Contains(x.PayrollMonth) && x.IsLocked)).AnyAsync(cancellationToken);
        if (locked)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "WFH_CONFLICT_LOCKED_PAYROLL: The approved leave touches a locked payroll month." };

        foreach (var day in dates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = UtcMidnight(day);
            var month = new DateTime(date.Year, date.Month, 1);
            if (request.IsHalfDay)
            {
                var segmentName = request.HalfDayPeriod == "SECOND_HALF" ? "SECOND_HALF" : "FIRST_HALF";
                var segmentFilter = Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId &&
                    x.Date >= date && x.Date < date.AddDays(1) && x.Segment == segmentName);
                var existing = await segments.GetCollection().Find(session, segmentFilter).FirstOrDefaultAsync(cancellationToken);
                if (existing is not null && !(existing.SourceType == "LEAVE" && existing.SourceId == request.LeaveRequestId))
                    return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_ATTENDANCE_CONFLICT: Another source still owns the leave attendance segment." };

                var target = existing ?? new AttendanceDaySegment
                {
                    CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId,
                    Date = date, Segment = segmentName, CreatedAtUtc = DateTime.UtcNow
                };
                var originalVersion = target.Version;
                target.Status = statusCode;
                target.SourceType = "LEAVE";
                target.SourceId = request.LeaveRequestId;
                target.SourceVersion = request.Version;
                target.CheckInTime = null;
                target.CheckOutTime = null;
                target.TotalHours = null;
                target.Remarks = $"Generated from approved half-day leave {request.LeaveRequestId}";
                target.UpdatedAtUtc = DateTime.UtcNow;
                target.Version++;
                if (existing is null)
                    await segments.GetCollection().InsertOneAsync(session, target, cancellationToken: cancellationToken);
                else
                {
                    var saved = await segments.GetCollection().ReplaceOneAsync(session,
                        Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId &&
                            x.AttendanceDaySegmentId == existing.AttendanceDaySegmentId && x.SourceType == existing.SourceType &&
                            x.SourceId == existing.SourceId && x.Version == originalVersion), target, cancellationToken: cancellationToken);
                    if (saved.MatchedCount != 1)
                        return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_ATTENDANCE_CHANGED: The attendance segment changed during conflict resolution." };
                }
            }
            else
            {
                await segments.GetCollection().DeleteManyAsync(session,
                    Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId &&
                        x.Date >= date && x.Date < date.AddDays(1) && x.SourceType == "LEAVE" && x.SourceId == request.LeaveRequestId),
                    options: null, cancellationToken: cancellationToken);
                var existingSegments = await segments.GetCollection().Find(session,
                    Builders<AttendanceDaySegment>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Date >= date && x.Date < date.AddDays(1))).ToListAsync(cancellationToken);
                if (existingSegments.Count != 0)
                    return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_ATTENDANCE_CONFLICT: Another source still owns an attendance segment on a full-day leave date." };
                var rowFilter = Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Date >= date && x.Date < date.AddDays(1));
                var existing = await attendance.GetCollection().Find(session, rowFilter).FirstOrDefaultAsync(cancellationToken);
                if (existing is not null && !(existing.SourceType == "LEAVE" && existing.SourceId == request.LeaveRequestId) &&
                    !AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(existing.SourceType))
                    return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_ATTENDANCE_CONFLICT: Another source still owns this full-day attendance date." };
                var target = existing ?? new AttendanceModel { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, Date = date };
                var originalVersion = target.Version;
                target.Status = statusCode;
                target.SourceType = "LEAVE";
                target.SourceId = request.LeaveRequestId;
                target.SourceVersion = request.Version;
                target.CheckInTime = null;
                target.CheckOutTime = null;
                target.TotalHours = null;
                target.Remarks = $"Generated from approved leave {request.LeaveRequestId}";
                target.Version++;
                if (existing is null)
                    await attendance.GetCollection().InsertOneAsync(session, target, cancellationToken: cancellationToken);
                else
                {
                    var saved = await attendance.GetCollection().ReplaceOneAsync(session,
                        Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId &&
                            x.AttendanceId == existing.AttendanceId && x.Version == originalVersion &&
                            (x.SourceType == existing.SourceType && x.SourceId == existing.SourceId)), target, cancellationToken: cancellationToken);
                    if (saved.MatchedCount != 1)
                        return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_ATTENDANCE_CHANGED: The attendance row changed during conflict resolution." };
                }
            }
        }

        foreach (var month in months)
            await summaries.GetCollection().DeleteManyAsync(session,
                Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && !x.IsLocked),
                options: null, cancellationToken: cancellationToken);
        var now = DateTime.UtcNow;
        var leaveWrite = await leaves.GetCollection().UpdateOneAsync(session,
            Builders<LeaveRequest>.Filter.Where(x => x.CompanyId == companyId && x.LeaveRequestId == request.LeaveRequestId &&
                x.EmployeeId == request.EmployeeId && x.Status == Codeji.CMS.Utility.Enums.EnumsHelper.LeaveRequestStatus.Accepted && x.Version == request.Version),
            Builders<LeaveRequest>.Update.Set(x => x.ReconciliationStatus, "Completed")
                .Set(x => x.ReconciliationErrorCode, null).Set(x => x.ReconciliationErrorMessage, null)
                .Set(x => x.ReconciledAtUtc, now).Set(x => x.LastReconciliationAttemptAtUtc, now)
                .Set(x => x.NextReconciliationAttemptAtUtc, null).Inc(x => x.ReconciliationAttempts, 1), cancellationToken: cancellationToken);
        if (leaveWrite.MatchedCount != 1)
            return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "LEAVE_WFH_SOURCE_CHANGED: The accepted leave changed during attendance reconciliation." };
        return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "Approved leave attendance was reconciled." };
    }

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

        var employee = await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.EmployeeId && !x.IsDeleted);
        var policy = await policies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == request.LeavePolicyId && x.Status);
        if (employee == null)
        {
            result.Message = "The employee for this leave request is not available in the authenticated company.";
            return result;
        }
        if (policy == null)
        {
            result.Message = "The leave policy for this request is not active.";
            return result;
        }
        // Unpaid leave has a fixed attendance meaning: a full day is unpaid
        // absence, while a half day is the configured Half Day (HD) status.
        // Do not let a legacy UL policy mapping turn an approved half-day UL
        // request into a full absent attendance segment.
        var isUnpaidLeave = policy.PolicyType == Codeji.CMS.Utility.Enums.EnumsHelper.LeavePolicyType.UnpaidLeave ||
            (string.Equals(policy.Code?.Trim(), "UL", StringComparison.OrdinalIgnoreCase) && !policy.Paid);
        var mappedStatus = request.IsHalfDay && isUnpaidLeave
            ? "HD"
            : request.IsHalfDay
                ? policy.HalfDayAttendanceStatusCode ?? policy.FullDayAttendanceStatusCode ?? policy.AttendanceStatusCode
                : policy.FullDayAttendanceStatusCode ?? policy.AttendanceStatusCode;
        if (string.IsNullOrWhiteSpace(mappedStatus))
        {
            result.Message = "The leave policy must have an attendance status code before the request can be approved.";
            return result;
        }

        var statusCode = mappedStatus.Trim().ToUpperInvariant();
        var status = await statuses.FirstOrDefault(x => x.CompanyId == companyId && x.Code == statusCode && x.IsActive);
        if (status == null || status.RequiresTime)
        {
            result.Message = "The leave policy attendance status must be an active status that does not require check-in or check-out times.";
            return result;
        }

        var dates = (await workingCalendar.GetLeaveDatesAsync(companyId, DateOnly.FromDateTime(request.StartDate),
            DateOnly.FromDateTime(request.EndDate), policy.WeekendInclusive, policy.HolidayInclusive, cancellationToken)).ToList();
        dates = dates.Where(day => EmploymentPeriod.WasEmployedOn(employee, day.ToDateTime(TimeOnly.MinValue))).ToList();
        if (dates.Count == 0)
        {
            result.Message = "The leave request does not contain a date within the employee's employment period.";
            return result;
        }

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
        var affectedMonthsList = dates.Select(x => new DateTime(x.Year, x.Month, 1)).Distinct().ToList();
        var lockedMonths = (await summaries.GetAll(x => x.CompanyId == companyId && x.UserId == employee.UserId && affectedMonthsList.Contains(x.PayrollMonth) && x.IsLocked))
            .Select(x => x.PayrollMonth).ToHashSet();

        foreach (var day in dates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = UtcMidnight(day);
            var month = new DateTime(date.Year, date.Month, 1);
            if (lockedMonths.Contains(month))
            {
                hasConflict = true;
                var conflict = await exceptions.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && x.ExceptionType == "LEAVE_APPLIED_FOR_LOCKED_MONTH" && x.AttendanceDate == date && x.LeaveRequestId == request.LeaveRequestId);
                if (conflict == null)
                    await exceptions.AddOne(new AttendancePayrollException { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, PayrollMonth = month, AttendanceDate = date, AffectedDates = [date], ExceptionType = "LEAVE_APPLIED_FOR_LOCKED_MONTH", Severity = "BLOCKING", Status = "PENDING_REVIEW", LeaveRequestId = request.LeaveRequestId, RequestedLeaveStatus = statusCode, SourceVersion = request.Version, Reason = $"Approved leave for {date:yyyy-MM-dd} falls into an already locked payroll month ({month:MMMM yyyy}). Unlock the month in Attendance settings to apply retroactive adjustments.", CreatedBy = actorUserId });
                continue;
            }

            var existingSegments = await segments.GetAll(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Date >= date && x.Date < date.AddDays(1));
            if (existingSegments.Any())
            {
                hasConflict = true;
                var existingSegment = existingSegments.First();
                var conflict = await exceptions.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && x.ExceptionType == "LEAVE_ATTENDANCE_SEGMENT_CONFLICT" && x.AttendanceDate == date && x.LeaveRequestId == request.LeaveRequestId);
                if (conflict == null)
                    await exceptions.AddOne(new AttendancePayrollException { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, PayrollMonth = month, AttendanceDate = date, AffectedDates = [date], ExceptionType = "LEAVE_ATTENDANCE_SEGMENT_CONFLICT", Severity = "BLOCKING", Status = "PENDING_REVIEW", LeaveRequestId = request.LeaveRequestId, WorkFromHomeRequestId = ResolveWfhSourceId(existingSegment.SourceType, existingSegment.SourceId), ExistingAttendanceId = existingSegment.AttendanceDaySegmentId, ExistingStatus = existingSegment.Status, RequestedLeaveStatus = statusCode, SourceVersion = request.Version, Reason = "Approved full-day leave conflicts with existing half-day attendance.", CreatedBy = actorUserId });
                continue;
            }
            var existing = await attendance.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Date >= date && x.Date < date.AddDays(1));
            if (existing != null && !(existing.SourceType == "LEAVE" && existing.SourceId == request.LeaveRequestId) && !AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(existing.SourceType))
            {
                hasConflict = true;
                var conflict = await exceptions.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && x.ExceptionType == "LEAVE_ATTENDANCE_CONFLICT" && x.AttendanceDate == date && x.LeaveRequestId == request.LeaveRequestId);
                if (conflict == null)
                    await exceptions.AddOne(new AttendancePayrollException { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, PayrollMonth = month, AttendanceDate = date, AffectedDates = [date], ExceptionType = "LEAVE_ATTENDANCE_CONFLICT", Severity = "BLOCKING", Status = "PENDING_REVIEW", LeaveRequestId = request.LeaveRequestId, WorkFromHomeRequestId = ResolveWfhSourceId(existing.SourceType, existing.SourceId), ExistingAttendanceId = existing.AttendanceId, ExistingStatus = existing.Status, RequestedLeaveStatus = statusCode, SourceVersion = request.Version, Reason = "Approved leave conflicts with existing attendance.", CreatedBy = actorUserId });
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
            await initialization.InitializeEmployeeAttendanceAsync(companyId, actorUserId, request.EmployeeId, month, cancellationToken);
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

    internal static string? ResolveWfhSourceId(string? sourceType, string? sourceId) =>
        string.Equals(sourceType, "WFH_REQUEST", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(sourceId)
            ? sourceId
            : null;

    private async Task<Result> ReconcileHalfDayAsync(string companyId, LeaveRequest request, EmpUser employee, string statusCode, string actorUserId, CancellationToken cancellationToken)
    {
        var date = UtcMidnight(DateOnly.FromDateTime(request.StartDate));
        var month = new DateTime(date.Year, date.Month, 1);
        var isLocked = await summaries.Exist(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && x.IsLocked);
        if (isLocked)
        {
            if (!await exceptions.Exist(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && x.ExceptionType == "LEAVE_APPLIED_FOR_LOCKED_MONTH" && x.AttendanceDate == date && x.LeaveRequestId == request.LeaveRequestId))
                await exceptions.AddOne(new AttendancePayrollException { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, PayrollMonth = month, AttendanceDate = date, AffectedDates = [date], ExceptionType = "LEAVE_APPLIED_FOR_LOCKED_MONTH", Severity = "BLOCKING", Status = "PENDING_REVIEW", LeaveRequestId = request.LeaveRequestId, RequestedLeaveStatus = statusCode, SourceVersion = request.Version, Reason = $"Approved half-day leave for {date:yyyy-MM-dd} falls into an already locked payroll month ({month:MMMM yyyy}). Unlock the month in Attendance settings to apply retroactive adjustments.", CreatedBy = actorUserId });
            return new Result { Success = true, Message = "LEAVE_APPLIED_FOR_LOCKED_MONTH: Attendance synchronization requires review." };
        }
        var segmentName = request.HalfDayPeriod == "SECOND_HALF" ? "SECOND_HALF" : "FIRST_HALF";
        var existing = await segments.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Date >= date && x.Date < date.AddDays(1) && x.Segment == segmentName);
        if (existing is not null && !(existing.SourceType == "LEAVE" && existing.SourceId == request.LeaveRequestId))
        {
            if (!await exceptions.Exist(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && x.ExceptionType == "LEAVE_ATTENDANCE_SEGMENT_CONFLICT" && x.AttendanceDate == date && x.LeaveRequestId == request.LeaveRequestId))
                await exceptions.AddOne(new AttendancePayrollException { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, PayrollMonth = month, AttendanceDate = date, AffectedDates = [date], ExceptionType = "LEAVE_ATTENDANCE_SEGMENT_CONFLICT", Severity = "BLOCKING", Status = "PENDING_REVIEW", LeaveRequestId = request.LeaveRequestId, WorkFromHomeRequestId = ResolveWfhSourceId(existing.SourceType, existing.SourceId), ExistingAttendanceId = existing.AttendanceDaySegmentId, ExistingStatus = existing.Status, RequestedLeaveStatus = statusCode, SourceVersion = request.Version, Reason = $"Approved leave conflicts with the {segmentName} attendance segment.", CreatedBy = actorUserId });
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
