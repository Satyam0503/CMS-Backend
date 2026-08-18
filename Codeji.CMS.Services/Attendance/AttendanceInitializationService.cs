using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Attendance;

public interface IAttendanceInitializationService
{
    Task<Result<AttendanceInitializationResultDto>> InitializeMonthAsync(string companyId, string actorUserId, DateTime month, CancellationToken cancellationToken = default, DateTime? throughDate = null);
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
        foreach (var employee in await employees.GetAll(x => x.CompanyId == companyId && x.Status && !x.IsDeleted))
        {
            output.EmployeesProcessed++;
            foreach (var day in workingDates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var date = day.ToDateTime(TimeOnly.MinValue);
                if (DateTime.TryParse(employee.DateOfJoining, out var joined) && date.Date < joined.Date || DateTime.TryParse(employee.ExitDate, out var exited) && date.Date > exited.Date)
                { output.EmploymentSkipped++; continue; }
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
}
