using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.Attendance;
using MongoDB.Driver;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees;

namespace Codeji.CMS.Services.Attendance;

public interface ILifecycleAttendanceReconciliationService
{
    Task<Result> ReconcileCaseAttendanceAsync(string companyId, string caseId, string actorUserId, CancellationToken cancellationToken = default);
    Task<Result> ReverseCaseAttendanceAsync(string companyId, string caseId, string actorUserId, CancellationToken cancellationToken = default);
    Task<Result> HandleReturnToWorkAsync(string companyId, string caseId, DateTime actualReturnDate, string actorUserId, CancellationToken cancellationToken = default);
}

public sealed class LifecycleAttendanceReconciliationService(
    IMongoDbRepository<EmployeeLifecycleCase> cases,
    IMongoDbRepository<EmployeeLifecyclePolicy> lifecyclePolicies,
    IMongoDbRepository<LeavePolicy> leavePolicies,
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<AttendanceStatusSetting> statuses,
    IMongoDbRepository<AttendanceModel> attendance,
    IMongoDbRepository<AttendanceDaySegment> segments,
    IMongoDbRepository<AttendancePayrollException> exceptions,
    IMongoDbRepository<MonthlyAttendanceSummary> summaries,
    ICompanyWorkingCalendarService workingCalendar,
    IAttendanceInitializationService initialization) : ILifecycleAttendanceReconciliationService
{
    public async Task<Result> ReconcileCaseAttendanceAsync(string companyId, string caseId, string actorUserId, CancellationToken cancellationToken = default)
    {
        var result = new Result { Success = false };
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(caseId))
        {
            result.Message = "Company and Case ID are required.";
            return result;
        }

        var item = await cases.FirstOrDefault(x => x.CompanyId == companyId && x.CaseId == caseId);
        if (item == null)
        {
            result.Message = "Lifecycle case was not found in the current company.";
            return result;
        }

        if (item.Status == "Cancelled")
        {
            result.Message = "Cancelled lifecycle cases cannot be reconciled to attendance.";
            return result;
        }

        var employee = await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == item.EmployeeUserId && !x.IsDeleted);
        if (employee == null)
        {
            result.Message = "The employee for this lifecycle case is not available in the current company.";
            return result;
        }

        var statusCode = await ResolveAttendanceStatusCodeAsync(companyId, item, cancellationToken);
        if (string.IsNullOrWhiteSpace(statusCode))
        {
            result.Message = "Could not resolve an active attendance status code for this lifecycle event.";
            return result;
        }

        // Determine the date range covered by the absence.
        var fromDate = DateOnly.FromDateTime(item.EffectiveFromUtc);
        DateOnly toDate;

        if (item.Status == "Completed" && item.ActualReturnDate.HasValue)
        {
            var returnDate = DateOnly.FromDateTime(item.ActualReturnDate.Value);
            toDate = returnDate > fromDate ? returnDate.AddDays(-1) : fromDate;
        }
        else if (item.ExpectedReturnDate.HasValue)
        {
            toDate = DateOnly.FromDateTime(item.ExpectedReturnDate.Value);
        }
        else
        {
            toDate = fromDate;
        }

        if (toDate < fromDate)
            toDate = fromDate;

        var dates = (await workingCalendar.GetLeaveDatesAsync(companyId, fromDate, toDate, weekendInclusive: false, holidayInclusive: false, cancellationToken)).ToList();
        dates = dates.Where(day => EmploymentPeriod.WasEmployedOn(employee, day.ToDateTime(TimeOnly.MinValue))).ToList();

        var activeDateInstants = dates.Select(UtcMidnight).ToHashSet();

        // 1. Remove stale rows owned by this case outside the active date range (e.g. if extended date shrank or returned early)
        var ownedRows = await attendance.GetAll(x => x.CompanyId == companyId &&
            x.UserId == employee.UserId && x.SourceType == "LIFECYCLE_CASE" && x.SourceId == item.CaseId);
        foreach (var ownedRow in ownedRows.Where(row => !activeDateInstants.Contains(row.Date.Date)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await attendance.Delete(Builders<AttendanceModel>.Filter.Eq(x => x.AttendanceId, ownedRow.AttendanceId));
        }

        // 2. Upsert attendance rows for active dates
        var hasConflict = false;
        foreach (var day in dates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = UtcMidnight(day);

            var existingSegments = await segments.GetAll(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Date >= date && x.Date < date.AddDays(1));
            if (existingSegments.Any())
            {
                hasConflict = true;
                continue;
            }

            var existing = await attendance.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Date >= date && x.Date < date.AddDays(1));
            if (existing != null && !(existing.SourceType == "LIFECYCLE_CASE" && existing.SourceId == item.CaseId)
                && !(existing.SourceType == "LEAVE" && existing.SourceId == item.LeaveRequestId)
                && !AttendanceSourceTransitionPolicy.IsReplaceableDefaultPresent(existing.SourceType))
            {
                hasConflict = true;
                continue;
            }

            var entity = existing ?? new AttendanceModel { CompanyId = companyId, UserId = employee.UserId, EmployeeId = employee.EmployeeId, Date = date };
            entity.Status = statusCode;
            entity.SourceType = "LIFECYCLE_CASE";
            entity.SourceId = item.CaseId;
            entity.SourceVersion = item.Version;
            entity.CheckInTime = null;
            entity.CheckOutTime = null;
            entity.TotalHours = null;
            entity.Remarks = $"Generated from lifecycle case {item.CaseId} ({item.LifecycleStatus})";
            entity.ModifiedByUserId = actorUserId;
            entity.ModifiedAtUtc = DateTime.UtcNow;

            if (existing == null) await attendance.AddOne(entity);
            else await attendance.Update(Builders<AttendanceModel>.Filter.Eq(x => x.AttendanceId, existing.AttendanceId), entity);
        }

        // 3. Invalidate unlocked monthly attendance summaries for affected months
        var affectedMonths = dates.Select(x => new DateTime(x.Year, x.Month, 1)).Distinct().ToList();
        foreach (var month in affectedMonths)
        {
            await summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.PayrollMonth == month && !x.IsLocked));
        }

        // 4. Update reconciliation status on case
        item.LeaveReconciliationStatus = hasConflict ? "Conflict" : "Completed";
        item.UpdatedBy = actorUserId;
        item.UpdatedDate = DateTime.UtcNow;
        await cases.Update(Builders<EmployeeLifecycleCase>.Filter.Where(x => x.CompanyId == companyId && x.CaseId == item.CaseId && x.Version == item.Version), item);

        result.Success = true;
        result.Message = hasConflict
            ? "Lifecycle attendance reconciled with some conflicts."
            : "Lifecycle attendance reconciled successfully.";
        return result;
    }

    public async Task<Result> ReverseCaseAttendanceAsync(string companyId, string caseId, string actorUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = new Result { Success = false };
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(caseId))
        {
            result.Message = "Company and Case ID are required.";
            return result;
        }

        var item = await cases.FirstOrDefault(x => x.CompanyId == companyId && x.CaseId == caseId);
        var filter = Builders<AttendanceModel>.Filter.Where(x => x.CompanyId == companyId && x.SourceType == "LIFECYCLE_CASE" && x.SourceId == caseId);
        var deleteResult = await attendance.DeleteAll(filter);
        if (!deleteResult.Success) return deleteResult;

        if (item != null)
        {
            var from = item.EffectiveFromUtc.Date;
            var to = (item.ExpectedReturnDate ?? item.EffectiveFromUtc).Date;
            var affectedMonths = Enumerable.Range(0, Math.Max(1, (to - from).Days + 1))
                .Select(offset => from.AddDays(offset))
                .Select(date => new DateTime(date.Year, date.Month, 1))
                .Distinct();

            foreach (var month in affectedMonths)
            {
                await summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == companyId && x.UserId == item.EmployeeUserId && x.PayrollMonth == month && !x.IsLocked));
                await initialization.InitializeMonthAsync(companyId, actorUserId, month, cancellationToken);
            }

            item.LeaveReconciliationStatus = "Reversed";
            item.UpdatedBy = actorUserId;
            item.UpdatedDate = DateTime.UtcNow;
            await cases.Update(Builders<EmployeeLifecycleCase>.Filter.Where(x => x.CompanyId == companyId && x.CaseId == item.CaseId && x.Version == item.Version), item);
        }

        result.Success = true;
        result.Message = "Lifecycle case attendance reversed successfully.";
        return result;
    }

    public async Task<Result> HandleReturnToWorkAsync(string companyId, string caseId, DateTime actualReturnDate, string actorUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = new Result { Success = false };
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(caseId))
        {
            result.Message = "Company and Case ID are required.";
            return result;
        }

        var item = await cases.FirstOrDefault(x => x.CompanyId == companyId && x.CaseId == caseId);
        if (item == null)
        {
            result.Message = "Lifecycle case was not found in the current company.";
            return result;
        }

        // 1. Delete any rows owned by this case on or after the actual return date
        var returnDateUtc = UtcMidnight(DateOnly.FromDateTime(actualReturnDate));
        await attendance.DeleteAll(Builders<AttendanceModel>.Filter.Where(x =>
            x.CompanyId == companyId &&
            x.UserId == item.EmployeeUserId &&
            x.SourceType == "LIFECYCLE_CASE" &&
            x.SourceId == caseId &&
            x.Date >= returnDateUtc));

        // 2. Reconcile attendance for dates prior to return date
        var reconcileResult = await ReconcileCaseAttendanceAsync(companyId, caseId, actorUserId, cancellationToken);
        if (!reconcileResult.Success) return reconcileResult;

        // 3. For the month containing return date, re-initialize attendance so working days from return date onward get proper attendance
        var month = new DateTime(actualReturnDate.Year, actualReturnDate.Month, 1);
        await summaries.DeleteAll(Builders<MonthlyAttendanceSummary>.Filter.Where(x => x.CompanyId == companyId && x.UserId == item.EmployeeUserId && x.PayrollMonth == month && !x.IsLocked));
        await initialization.InitializeMonthAsync(companyId, actorUserId, month, cancellationToken);

        result.Success = true;
        result.Message = "Return to work attendance handled successfully.";
        return result;
    }

    private async Task<string?> ResolveAttendanceStatusCodeAsync(string companyId, EmployeeLifecycleCase item, CancellationToken cancellationToken)
    {
        // 1. Try leave policy directly linked to case
        if (!string.IsNullOrWhiteSpace(item.LeavePolicyId))
        {
            var policy = await leavePolicies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == item.LeavePolicyId && x.Status);
            var code = policy?.FullDayAttendanceStatusCode ?? policy?.AttendanceStatusCode;
            if (!string.IsNullOrWhiteSpace(code)) return code.Trim().ToUpperInvariant();
        }

        // 2. Try lifecycle policy for event type
        var lifecyclePolicy = await lifecyclePolicies.FirstOrDefault(x => x.CompanyId == companyId && x.EventType == item.LifecycleStatus && !x.IsDeleted && x.IsActive);
        if (lifecyclePolicy != null && !string.IsNullOrWhiteSpace(lifecyclePolicy.LeavePolicyId))
        {
            var policy = await leavePolicies.FirstOrDefault(x => x.CompanyId == companyId && x.Id == lifecyclePolicy.LeavePolicyId && x.Status);
            var code = policy?.FullDayAttendanceStatusCode ?? policy?.AttendanceStatusCode;
            if (!string.IsNullOrWhiteSpace(code)) return code.Trim().ToUpperInvariant();
        }

        // 3. Fallback map based on event type
        var fallbackCode = item.LifecycleStatus switch
        {
            EmployeeLifecycleStatuses.MedicalLeave => "MED",
            EmployeeLifecycleStatuses.SickMedicalLeave => "MED",
            EmployeeLifecycleStatuses.MaternityLeave => "ML",
            EmployeeLifecycleStatuses.PaternityLeave => "PTL",
            EmployeeLifecycleStatuses.Sabbatical => "SAB",
            EmployeeLifecycleStatuses.LongTermLeave => "UL",
            EmployeeLifecycleStatuses.AdministrativeLeave => "ADMIN",
            EmployeeLifecycleStatuses.UnauthorizedAbsence => "A",
            _ => "L"
        };

        // Check if tenant has an active AttendanceStatusSetting matching fallbackCode
        var activeStatus = await statuses.FirstOrDefault(x => x.CompanyId == companyId && x.Code == fallbackCode && x.IsActive);
        if (activeStatus != null) return activeStatus.Code;

        // If not found, try common standard codes
        var secondaryFallback = item.LifecycleStatus switch
        {
            EmployeeLifecycleStatuses.MedicalLeave => "SL",
            EmployeeLifecycleStatuses.SickMedicalLeave => "SL",
            EmployeeLifecycleStatuses.MaternityLeave => "L",
            EmployeeLifecycleStatuses.PaternityLeave => "L",
            EmployeeLifecycleStatuses.Sabbatical => "UL",
            EmployeeLifecycleStatuses.AdministrativeLeave => "A",
            _ => "A"
        };

        var secondaryStatus = await statuses.FirstOrDefault(x => x.CompanyId == companyId && x.Code == secondaryFallback && x.IsActive);
        return secondaryStatus?.Code ?? fallbackCode;
    }

    private static DateTime UtcMidnight(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
}

