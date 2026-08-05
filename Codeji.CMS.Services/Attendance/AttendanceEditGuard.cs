using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Attendance;
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
        var employee = await employees.FirstOrDefault(x =>
            x.CompanyId == companyId && x.UserId == userId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Employee was not found for attendance update.");
        var month = new DateTime(date.Year, date.Month, 1);
        // A correction is only valid while the employee's attendance period remains open.
        // Payroll generation and attendance locking are both finalisation boundaries.
        if (await payrolls.Exist(x => x.CompanyId == employee.CompanyId && x.EmployeeId == employee.EmployeeId && x.PayMonth == month && x.IsProcessed))
            throw new InvalidOperationException($"Attendance for employee {employee.EmployeeId} cannot be changed because payroll for {month:MMMM yyyy} has already been generated.");
        if (await summaries.Exist(x => x.CompanyId == employee.CompanyId && x.UserId == userId && x.PayrollMonth == month && x.IsLocked))
            throw new InvalidOperationException($"Attendance for employee {employee.EmployeeId} cannot be changed because attendance for {month:MMMM yyyy} is locked.");

        var weekly = await weeklyOffs.FirstOrDefault(x => x.CompanyId == employee.CompanyId);
        var offDays = weekly?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday];
        if (offDays.Contains((int)date.DayOfWeek))
            throw new InvalidOperationException($"{date:yyyy-MM-dd} is a configured weekly off for employee {employee.EmployeeId}.");

        var holidays = await calendar.GetAll(x => x.CompanyId == employee.CompanyId && x.Type == EnumsHelper.CalendarItem.Holiday && (x.Recurring || x.Date.Date == date.Date));
        if (holidays.Any(x => x.Recurring ? x.Date.Month == date.Month && x.Date.Day == date.Day : x.Date.Date == date.Date))
            throw new InvalidOperationException($"{date:yyyy-MM-dd} is a company holiday for employee {employee.EmployeeId}.");
    }
}
