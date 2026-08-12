using Codeji.CMS.Repository.Entities.Calendar;

namespace Codeji.CMS.Services.Attendance;

public static class CalendarDateHelpers
{
    public static bool MatchesDate(CalendarEntity? holiday, DateOnly target)
    {
        if (holiday is null)
        {
            return false;
        }

        if (holiday.Recurring)
        {
            return holiday.Date.Month == target.Month && holiday.Date.Day == target.Day;
        }

        return DateOnly.FromDateTime(holiday.Date) == target;
    }

    public static bool MatchesDate(CalendarEntity? holiday, DateTime target)
        => MatchesDate(holiday, DateOnly.FromDateTime(target.Date));
}
