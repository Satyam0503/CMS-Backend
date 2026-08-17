using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Utility.Enums;

public interface ICompanyWorkingCalendarService
{
    Task<bool> IsWorkingDayAsync(string companyId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DateOnly>> GetWorkingDatesAsync(string companyId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<int> CountWorkingDaysAsync(string companyId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DateOnly>> GetLeaveDatesAsync(string companyId, DateOnly start, DateOnly end, bool weekendInclusive, bool holidayInclusive, CancellationToken cancellationToken = default);
}

public sealed class CompanyWorkingCalendarService(
    IMongoDbRepository<WeeklyOffSetting> weeklyOffs,
    IMongoDbRepository<CalendarEntity> calendar) : ICompanyWorkingCalendarService
{
    public async Task<bool> IsWorkingDayAsync(string companyId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var dates = await GetWorkingDatesAsync(companyId, date, date, cancellationToken);
        return dates.Count == 1;
    }

    public async Task<IReadOnlyList<DateOnly>> GetWorkingDatesAsync(string companyId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        if (end < start) throw new ArgumentException("End date cannot be before start date.");
        cancellationToken.ThrowIfCancellationRequested();
        var weekly = (await weeklyOffs.GetAll(x => x.CompanyId == companyId, withDefaultFilter: false)).FirstOrDefault();
        var offDays = (weekly?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday]).ToHashSet();
        var startDate = start.ToDateTime(TimeOnly.MinValue);
        var endDate = end.ToDateTime(TimeOnly.MinValue);
        // A ±1 day window tolerates a legacy row stored as an IST-midnight instant
        // converted to UTC; CalendarDateHelpers.MatchesDate makes the exact call.
        var holidays = (await calendar.GetAll(x => x.CompanyId == companyId &&
            x.Type == EnumsHelper.CalendarItem.Holiday && (x.Recurring || (x.Date >= startDate.AddDays(-1) && x.Date <= endDate.AddDays(1))), withDefaultFilter: false)).ToList();
        var result = new List<DateOnly>();
        for (var current = start; current <= end; current = current.AddDays(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (offDays.Contains((int)current.DayOfWeek)) continue;
            if (holidays.Any(x => CalendarDateHelpers.MatchesDate(x, current))) continue;
            result.Add(current);
        }
        return result;
    }

    public async Task<int> CountWorkingDaysAsync(string companyId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) =>
        (await GetWorkingDatesAsync(companyId, start, end, cancellationToken)).Count;

    public async Task<IReadOnlyList<DateOnly>> GetLeaveDatesAsync(string companyId, DateOnly start, DateOnly end, bool weekendInclusive, bool holidayInclusive, CancellationToken cancellationToken = default)
    {
        if (end < start) throw new ArgumentException("End date cannot be before start date.");
        var weekly = (await weeklyOffs.GetAll(x => x.CompanyId == companyId, withDefaultFilter: false)).FirstOrDefault();
        var offDays = (weekly?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday]).ToHashSet();
        var startDate = start.ToDateTime(TimeOnly.MinValue);
        var endDate = end.ToDateTime(TimeOnly.MinValue);
        // A ±1 day window tolerates a legacy row stored as an IST-midnight instant
        // converted to UTC; CalendarDateHelpers.MatchesDate makes the exact call.
        var holidays = (await calendar.GetAll(x => x.CompanyId == companyId && x.Type == EnumsHelper.CalendarItem.Holiday &&
            (x.Recurring || (x.Date >= startDate.AddDays(-1) && x.Date <= endDate.AddDays(1))), withDefaultFilter: false)).ToList();
        var result = new List<DateOnly>();
        for (var current = start; current <= end; current = current.AddDays(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isWeekend = offDays.Contains((int)current.DayOfWeek);
            var isHoliday = holidays.Any(x => CalendarDateHelpers.MatchesDate(x, current));
            if ((!weekendInclusive && isWeekend) || (!holidayInclusive && isHoliday)) continue;
            result.Add(current);
        }
        return result;
    }
}
