using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Calendar;
using Codeji.CMS.Services.Calendar.Interface;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarController : ControllerBase
{
    readonly ICalendarServices _calendarServices;
    readonly IHttpContextAccessor _httpContextAccessor;
    public CalendarController(ICalendarServices calendarServices, IHttpContextAccessor httpContextAccessor)
    {
        _calendarServices = calendarServices;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost]
    [Route("GetAllCalendarItems")]
    public async Task<Result<CalendarResponseDto>> GetAllCalendarItems([FromBody] CalendarFilters? filters)
    {
        Result<CalendarResponseDto> result = new()
        {
            Success = false
        };
        if (!ModelState.IsValid) return result;
        result = await _calendarServices.GetAllCalendarItems(filters);
        return result;
    }

    [Route("AddUpdateCalendarItem")]
    [HttpPost]
    [ModulePermission(AppModule.Calendar, [Permission.Create, Permission.Edit])]
    public async Task<Result> AddUpdateCalendarItem([FromForm] CalendarRequestDto model)
    {
        Result result = new();
        result = await _calendarServices.AddUpdateCalendarItem(model);
        return result;
    }

    [Route("DeleteCalendarItem/{itemId}")]
    [HttpDelete]
    [ModulePermission(AppModule.Calendar, Permission.Delete)]
    public async Task<Result> DeleteCalendarItem(string itemId)
    {
        Result result = new();
        result = await _calendarServices.DeleteItem(itemId);
        return result;
    }

    [Route("GetHolidays")]
    [HttpPost]
    public async Task<Result<HolidayResponseDto>> GetHolidays([FromBody] HolidayFilter filter)
    {
        Result<HolidayResponseDto> result = new();
        if (filter.FromDate > filter.ToDate)
        {
            result.Success = false;
            result.Message = "FromDate cannot be greater than ToDate.";
            return result;
        }
        result = await _calendarServices.GetHolidays(filter);
        return result;
    }
}
