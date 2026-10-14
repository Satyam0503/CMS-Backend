using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Services.Attendance;

public interface IAttendanceInitializationService
{
    Task<Result<AttendanceInitializationResultDto>> InitializeMonthAsync(string companyId, string actorUserId, DateTime month, CancellationToken cancellationToken = default, DateTime? throughDate = null);
    Task<Result<AttendanceInitializationResultDto>> InitializeEmployeeAttendanceAsync(string companyId, string actorUserId, string userId, DateTime? month = null, CancellationToken cancellationToken = default, DateTime? throughDate = null);
    Task<string?> ResolvePresentStatusCodeAsync(string companyId, CancellationToken cancellationToken = default);
}

public sealed class AttendanceInitializationService(
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<AttendanceStatusSetting> statuses,
    IMongoDbRepository<MonthlyAttendanceSummary> summaries,
    ICompanyWorkingCalendarService calendar,
    IAttendanceMutationValidator validator,
    IAttendanceRepository attendance) : IAttendanceInitializationService
{
    public async Task<string?> ResolvePresentStatusCodeAsync(string companyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var present = (await statuses.GetAll(x => x.CompanyId == companyId && x.IsActive, withDefaultFilter: false))
            .Where(x => string.Equals(x.Name?.Trim(), "Present", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.SortOrder).FirstOrDefault();
        return present?.Code?.Trim().ToUpperInvariant();
    }

    public async Task<Result<AttendanceInitializationResultDto>> InitializeMonthAsync(string companyId, string actorUserId, DateTime month, CancellationToken cancellationToken = default, DateTime? throughDate = null)
    {
        var result = new Result<AttendanceInitializationResultDto> { Success = false, MethodResult = new AttendanceInitializationResultDto() };
        var presentCode = await ResolvePresentStatusCodeAsync(companyId, cancellationToken);
        if (string.IsNullOrWhiteSpace(presentCode)) { result.Message = "ATTENDANCE_PRESENT_STATUS_NOT_CONFIGURED"; return result; }
        var start = new DateTime(month.Year, month.Month, 1);
        // The caller supplies the tenant's business date when this runs from the scheduler.
        // API callers retain the historical UTC-safe default and can never initialize future days.
        var businessToday = (throughDate ?? DateTime.UtcNow).Date;
        var end = businessToday < start.AddMonths(1).AddDays(-1) ? businessToday : start.AddMonths(1).AddDays(-1);
        if (end < start) { result.Success = true; return result; }
        var output = result.MethodResult;
        var summariesToInvalidate = new HashSet<(string UserId, DateTime Month)>();
        var workingDates = await calendar.GetWorkingDatesAsync(companyId, DateOnly.FromDateTime(start), DateOnly.FromDateTime(end), cancellationToken);
        foreach (var employee in (await employees.GetAll(x => x.CompanyId == companyId && !x.IsDeleted))
            .Where(employee => EmploymentPeriod.IsEligibleForMonth(employee, start)))
        {
            output.EmployeesProcessed++;
            foreach (var day in workingDates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var date = day.ToDateTime(TimeOnly.MinValue);
                if (!EmploymentPeriod.WasEmployedOn(employee, date))
                { output.EmploymentSkipped++; continue; }
                if (!EmploymentPeriod.IsAttendanceExpectedOn(employee, date))
                { output.InvalidSkipped++; output.Failures.Add($"{employee.EmployeeId} {day:yyyy-MM-dd}: ATTENDANCE_NOT_EXPECTED_FOR_LIFECYCLE"); continue; }
                output.EligibleWorkingDays++;
                var prepared = await validator.PrepareAsync(new AttendanceMutationRequest
                {
                    CompanyId = companyId, ActorUserId = actorUserId, TargetUserId = employee.UserId,
                    AttendanceDate = day, StatusCode = presentCode, TimingMode = AttendanceTimingMode.Auto,
                    ExistingRecordPolicy = ExistingAttendancePolicy.CreateMissingOnly,
                    SourceType = AttendanceSourceTransitionPolicy.DefaultPresent,
                    RemarkCode = AttendanceSourceTransitionPolicy.DefaultPresent,
                    OperatorRemark = "System default Present created during attendance initialization."
                }, cancellationToken);
                if (!prepared.Success || prepared.MethodResult is null)
                { output.InvalidSkipped++; output.Failures.Add($"{employee.EmployeeId} {day:yyyy-MM-dd}: {prepared.Message}"); continue; }
                if (!prepared.MethodResult.ShouldWrite) { output.AlreadyExisting++; continue; }
                try
                {
                    await attendance.AddAsync(prepared.MethodResult.Attendance);
                    output.Created++;
                    summariesToInvalidate.Add((employee.UserId, new DateTime(date.Year, date.Month, 1)));
                }
                catch (MongoDB.Driver.MongoWriteException) { output.AlreadyExisting++; }
            }
        }
        foreach (var (affectedUserId, affectedMonth) in summariesToInvalidate)
            await summaries.DeleteAll(MongoDB.Driver.Builders<MonthlyAttendanceSummary>.Filter.Where(x =>
                x.CompanyId == companyId && x.UserId == affectedUserId && x.PayrollMonth == affectedMonth && !x.IsLocked));
        result.Success = true;
        result.Message = $"Created {output.Created} default Present record(s); {output.AlreadyExisting} already existed.";
        return result;
    }

    public async Task<Result<AttendanceInitializationResultDto>> InitializeEmployeeAttendanceAsync(
        string companyId,
        string actorUserId,
        string userId,
        DateTime? month = null,
        CancellationToken cancellationToken = default,
        DateTime? throughDate = null)
    {
        var result = new Result<AttendanceInitializationResultDto> { Success = false, MethodResult = new AttendanceInitializationResultDto() };
        var presentCode = await ResolvePresentStatusCodeAsync(companyId, cancellationToken);
        if (string.IsNullOrWhiteSpace(presentCode)) { result.Message = "ATTENDANCE_PRESENT_STATUS_NOT_CONFIGURED"; return result; }

        var employee = await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == userId && !x.IsDeleted);
        if (employee is null) { result.Message = "EMPLOYEE_NOT_FOUND"; return result; }

        DateTime joiningDate;
        if (!EmploymentPeriod.TryGetJoiningDate(employee, out joiningDate))
        {
            if (employee.CreatedDate.HasValue)
                joiningDate = employee.CreatedDate.Value.Date;
            else
                joiningDate = (throughDate ?? IndiaTime.Today).Date;
        }

        var lastWorkingDate = EmploymentPeriod.GetLastWorkingDate(employee);

        // Clean up any replaceable DefaultPresent rows that fall outside the employee's current employment window.
        // This ensures repeatedly changing DateOfJoining instantly purges stale auto-present rows.
        var deletedOutside = await attendance.DeleteDefaultPresentOutsideEmploymentPeriodAsync(
            companyId, userId, joiningDate, lastWorkingDate);

        var summariesToInvalidate = new HashSet<(string UserId, DateTime Month)>();
        if (deletedOutside > 0)
        {
            if (month.HasValue)
                summariesToInvalidate.Add((userId, new DateTime(month.Value.Year, month.Value.Month, 1)));
            else
            {
                summariesToInvalidate.Add((userId, new DateTime(joiningDate.Year, joiningDate.Month, 1)));
                var today = (throughDate ?? IndiaTime.Today).Date;
                summariesToInvalidate.Add((userId, new DateTime(today.Year, today.Month, 1)));
            }
        }

        var businessToday = (throughDate ?? IndiaTime.Today).Date;
        DateTime rangeStart;
        DateTime rangeEnd;

        if (month.HasValue)
        {
            var monthStart = new DateTime(month.Value.Year, month.Value.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            rangeStart = joiningDate > monthStart ? joiningDate : monthStart;
            rangeEnd = businessToday < monthEnd ? businessToday : monthEnd;
        }
        else
        {
            rangeStart = joiningDate;
            rangeEnd = businessToday;
        }

        if (rangeEnd < rangeStart)
        {
            foreach (var (affectedUserId, affectedMonth) in summariesToInvalidate)
                await summaries.DeleteAll(MongoDB.Driver.Builders<MonthlyAttendanceSummary>.Filter.Where(x =>
                    x.CompanyId == companyId && x.UserId == affectedUserId && x.PayrollMonth == affectedMonth && !x.IsLocked));
            result.Success = true;
            return result;
        }

        var output = result.MethodResult;
        var workingDates = await calendar.GetWorkingDatesAsync(companyId, DateOnly.FromDateTime(rangeStart), DateOnly.FromDateTime(rangeEnd), cancellationToken);

        output.EmployeesProcessed = 1;
        foreach (var day in workingDates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = day.ToDateTime(TimeOnly.MinValue);
            if (!EmploymentPeriod.WasEmployedOn(employee, date))
            { output.EmploymentSkipped++; continue; }
            if (!EmploymentPeriod.IsAttendanceExpectedOn(employee, date))
            { output.InvalidSkipped++; output.Failures.Add($"{employee.EmployeeId} {day:yyyy-MM-dd}: ATTENDANCE_NOT_EXPECTED_FOR_LIFECYCLE"); continue; }
            output.EligibleWorkingDays++;
            var prepared = await validator.PrepareAsync(new AttendanceMutationRequest
            {
                CompanyId = companyId,
                ActorUserId = actorUserId,
                TargetUserId = employee.UserId,
                AttendanceDate = day,
                StatusCode = presentCode,
                TimingMode = AttendanceTimingMode.Auto,
                ExistingRecordPolicy = ExistingAttendancePolicy.CreateMissingOnly,
                SourceType = AttendanceSourceTransitionPolicy.DefaultPresent,
                RemarkCode = AttendanceSourceTransitionPolicy.DefaultPresent,
                OperatorRemark = "System default Present created on the basis of date of joining."
            }, cancellationToken);
            if (!prepared.Success || prepared.MethodResult is null)
            { output.InvalidSkipped++; output.Failures.Add($"{employee.EmployeeId} {day:yyyy-MM-dd}: {prepared.Message}"); continue; }
            if (!prepared.MethodResult.ShouldWrite) { output.AlreadyExisting++; continue; }
            try
            {
                await attendance.AddAsync(prepared.MethodResult.Attendance);
                output.Created++;
                summariesToInvalidate.Add((employee.UserId, new DateTime(date.Year, date.Month, 1)));
            }
            catch (MongoDB.Driver.MongoWriteException) { output.AlreadyExisting++; }
        }

        foreach (var (affectedUserId, affectedMonth) in summariesToInvalidate)
            await summaries.DeleteAll(MongoDB.Driver.Builders<MonthlyAttendanceSummary>.Filter.Where(x =>
                x.CompanyId == companyId && x.UserId == affectedUserId && x.PayrollMonth == affectedMonth && !x.IsLocked));

        result.Success = true;
        result.Message = $"Created {output.Created} default Present record(s) for employee {employee.EmployeeId}; {output.AlreadyExisting} already existed.";
        return result;
    }
}

