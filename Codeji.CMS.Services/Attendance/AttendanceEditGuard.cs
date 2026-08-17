using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Utility.Enums;

public interface IAttendanceEditGuard
{
    Task EnsureEditableWorkingDayAsync(string companyId, string userId, DateTime date);
}

public sealed class AttendanceEditGuard(
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<EmpPayRoll> payrolls,
    IMongoDbRepository<MonthlyAttendanceSummary> summaries,
    IMongoDbRepository<WeeklyOffSetting> weeklyOffs,
    IMongoDbRepository<CalendarEntity> calendar) : IAttendanceEditGuard
{
    public async Task EnsureEditableWorkingDayAsync(string companyId, string userId, DateTime date)
    {
        var employee = (await employees.GetAll(x =>
            x.CompanyId == companyId && x.UserId == userId && !x.IsDeleted, withDefaultFilter: false)).FirstOrDefault()
            ?? throw new InvalidOperationException("Employee was not found for attendance update.");
        var month = new DateTime(date.Year, date.Month, 1);
        // A correction is only valid while the employee's attendance period remains open.
        // Payroll generation and attendance locking are both finalisation boundaries.
        if ((await payrolls.GetAll(x => x.CompanyId == employee.CompanyId && x.EmployeeId == employee.EmployeeId && x.PayMonth == month && x.IsProcessed, withDefaultFilter: false)).Any())
            throw new InvalidOperationException($"Attendance for employee {employee.EmployeeId} cannot be changed because payroll for {month:MMMM yyyy} has already been generated.");
        if ((await summaries.GetAll(x => x.CompanyId == employee.CompanyId && x.UserId == userId && x.PayrollMonth == month && x.IsLocked, withDefaultFilter: false)).Any())
            throw new InvalidOperationException($"Attendance for employee {employee.EmployeeId} cannot be changed because attendance for {month:MMMM yyyy} is locked.");

        var weekly = (await weeklyOffs.GetAll(x => x.CompanyId == employee.CompanyId, withDefaultFilter: false)).FirstOrDefault();
        var offDays = weekly?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday];
        if (offDays.Contains((int)date.DayOfWeek))
            throw new InvalidOperationException($"{date:yyyy-MM-dd} is a configured weekly off for employee {employee.EmployeeId}.");

        var holidays = await calendar.GetAll(x => x.CompanyId == employee.CompanyId && x.Type == EnumsHelper.CalendarItem.Holiday &&
            // A ±1 day window tolerates a legacy row stored as an IST-midnight instant
            // converted to UTC; CalendarDateHelpers.MatchesDate makes the exact call.
            (x.Recurring || (x.Date >= date.Date.AddDays(-1) && x.Date <= date.Date.AddDays(1))), withDefaultFilter: false);
        if (holidays.Any(x => CalendarDateHelpers.MatchesDate(x, date)))
            throw new InvalidOperationException($"{date:yyyy-MM-dd} is a company holiday for employee {employee.EmployeeId}.");
    }
}
