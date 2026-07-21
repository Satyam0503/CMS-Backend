using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Utility.Enums;

public interface IAttendanceEditGuard
{
    Task EnsureEditableWorkingDayAsync(string userId, DateTime date);
}

public sealed class AttendanceEditGuard(
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<MonthlyAttendanceSummary> summaries,
    IMongoDbRepository<WeeklyOffSetting> weeklyOffs,
    IMongoDbRepository<CalendarEntity> calendar) : IAttendanceEditGuard
{
    public async Task EnsureEditableWorkingDayAsync(string userId, DateTime date)
    {
        var employee = await employees.FirstOrDefault(x => x.UserId == userId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Employee was not found for attendance update.");
        var month = new DateTime(date.Year, date.Month, 1);
        var summary = await summaries.FirstOrDefault(x => x.CompanyId == employee.CompanyId && x.EmployeeId == employee.EmployeeId && x.PayrollMonth == month);
        if (summary?.IsLocked == true)
            throw new InvalidOperationException($"Attendance for employee {employee.EmployeeId} is locked for {month:MMMM yyyy}. Reopen the month through an authorized correction workflow before editing.");

        var weekly = await weeklyOffs.FirstOrDefault(x => x.CompanyId == employee.CompanyId);
        var offDays = weekly?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday];
        if (offDays.Contains((int)date.DayOfWeek))
            throw new InvalidOperationException($"{date:yyyy-MM-dd} is a configured weekly off for employee {employee.EmployeeId}.");

        var holidays = await calendar.GetAll(x => x.CompanyId == employee.CompanyId && x.Type == EnumsHelper.CalendarItem.Holiday && (x.Recurring || x.Date.Date == date.Date));
        if (holidays.Any(x => x.Recurring ? x.Date.Month == date.Month && x.Date.Day == date.Day : x.Date.Date == date.Date))
            throw new InvalidOperationException($"{date:yyyy-MM-dd} is a company holiday for employee {employee.EmployeeId}.");
    }
}
