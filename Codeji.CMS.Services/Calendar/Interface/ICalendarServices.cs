using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Calendar;

namespace Codeji.CMS.Services.Calendar.Interface;

public interface ICalendarServices
{
    Task<Result> AddUpdateCalendarItem(CalendarRequestDto model);
    Task<Result<CalendarResponseDto>> GetAllCalendarItems(CalendarFilters? filter);
    Task<Result> DeleteItem(string itemId);
}
