using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Utility.Enums;

public sealed class SandwichEvaluationResult
{
    public IReadOnlyList<DateOnly> LeaveDates { get; set; } = [];
    public int SandwichedDaysCount { get; set; }
    public bool IsSandwichApplied { get; set; }
}

public interface ICompanyWorkingCalendarService
{
    Task<bool> IsWorkingDayAsync(string companyId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DateOnly>> GetWorkingDatesAsync(string companyId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<int> CountWorkingDaysAsync(string companyId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DateOnly>> GetLeaveDatesAsync(string companyId, DateOnly start, DateOnly end, bool weekendInclusive, bool holidayInclusive, CancellationToken cancellationToken = default);
    Task<SandwichEvaluationResult> EvaluateSandwichLeaveDatesAsync(string companyId, DateOnly start, DateOnly end, bool weekendInclusive, bool holidayInclusive, bool enableSandwichRules, bool includeWeekendsInSandwich, bool includeHolidaysInSandwich, CancellationToken cancellationToken = default);
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

    public async Task<SandwichEvaluationResult> EvaluateSandwichLeaveDatesAsync(
        string companyId,
        DateOnly start,
        DateOnly end,
        bool weekendInclusive,
        bool holidayInclusive,
        bool enableSandwichRules,
        bool includeWeekendsInSandwich,
        bool includeHolidaysInSandwich,
        CancellationToken cancellationToken = default)
    {
        if (end < start) throw new ArgumentException("End date cannot be before start date.");
        var weekly = (await weeklyOffs.GetAll(x => x.CompanyId == companyId, withDefaultFilter: false)).FirstOrDefault();
        var offDays = (weekly?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday]).ToHashSet();
        var startDate = start.ToDateTime(TimeOnly.MinValue);
        var endDate = end.ToDateTime(TimeOnly.MinValue);
        var holidays = (await calendar.GetAll(x => x.CompanyId == companyId && x.Type == EnumsHelper.CalendarItem.Holiday &&
            (x.Recurring || (x.Date >= startDate.AddDays(-1) && x.Date <= endDate.AddDays(1))), withDefaultFilter: false)).ToList();

        var workingDaysInRange = new List<DateOnly>();
        var nonWorkingDaysInRange = new List<(DateOnly Date, bool IsWeekend, bool IsHoliday)>();

        for (var current = start; current <= end; current = current.AddDays(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isWeekend = offDays.Contains((int)current.DayOfWeek);
            var isHoliday = holidays.Any(x => CalendarDateHelpers.MatchesDate(x, current));

            if (!isWeekend && !isHoliday)
            {
                workingDaysInRange.Add(current);
            }
            else
            {
                nonWorkingDaysInRange.Add((current, isWeekend, isHoliday));
            }
        }

        if (!enableSandwichRules)
        {
            var fallback = new List<DateOnly>();
            for (var current = start; current <= end; current = current.AddDays(1))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var isWeekend = offDays.Contains((int)current.DayOfWeek);
                var isHoliday = holidays.Any(x => CalendarDateHelpers.MatchesDate(x, current));
                if ((!weekendInclusive && isWeekend) || (!holidayInclusive && isHoliday)) continue;
                fallback.Add(current);
            }
            return new SandwichEvaluationResult
            {
                LeaveDates = fallback,
                SandwichedDaysCount = 0,
                IsSandwichApplied = false
            };
        }

        var finalLeaveDates = new HashSet<DateOnly>(workingDaysInRange);
        int sandwichedCount = 0;

        foreach (var (date, isWeekend, isHoliday) in nonWorkingDaysInRange)
        {
            bool shouldInclude = false;
            bool isSandwiched = false;

            if (isWeekend && weekendInclusive)
            {
                shouldInclude = true;
            }
            else if (isHoliday && holidayInclusive)
            {
                shouldInclude = true;
            }
            else
            {
                // Sandwiched if there is a working day requested before and after within [start, end]
                bool hasWorkingDayBefore = workingDaysInRange.Any(d => d < date);
                bool hasWorkingDayAfter = workingDaysInRange.Any(d => d > date);

                if (hasWorkingDayBefore && hasWorkingDayAfter)
                {
                    if (isWeekend && includeWeekendsInSandwich)
                    {
                        shouldInclude = true;
                        isSandwiched = true;
                    }
                    else if (isHoliday && includeHolidaysInSandwich)
                    {
                        shouldInclude = true;
                        isSandwiched = true;
                    }
                }
            }

            if (shouldInclude)
            {
                finalLeaveDates.Add(date);
                if (isSandwiched) sandwichedCount++;
            }
        }

        var sortedDates = finalLeaveDates.OrderBy(d => d).ToList();
        return new SandwichEvaluationResult
        {
            LeaveDates = sortedDates,
            SandwichedDaysCount = sandwichedCount,
            IsSandwichApplied = sandwichedCount > 0
        };
    }
}
