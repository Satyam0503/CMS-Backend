using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Holiday;
using Codeji.CMS.Services.Holiday.Interface;
using Codeji.CMS.Utility.Constraints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidayController : ControllerBase
{
    private readonly IHolidayService holidayService;

    public HolidayController(IHolidayService holidayService)
    {
        this.holidayService = holidayService;
    }

    [Route("GetAllHoliday")]
    [HttpPost]
    [ModulePermission(AppModule.Holidays, Permission.View)]
    public async Task<Result<HolidayResponseDto>> GetAllHoliday([FromBody] HolidayFilter? filter)
    {
        return await holidayService.GetAllHoliday(filter);
    }

    [HttpPost]
    [Route("CreateHoliday")]
    [ModulePermission(AppModule.Holidays, Permission.Create)]
    public async Task<Result> CreateUpdateHoliday([FromForm] HolidayRequestDto model)
    {
        return await holidayService.CreateEditHoliday(model);
    }

    [HttpDelete]
    [Route("DeleteHoliday/{holidayId}")]
    [ModulePermission(AppModule.Holidays, Permission.Delete)]
    public async Task<Result> DeleteHoliday(string holidayId)
    {
        return await holidayService.DeleteHoliday(holidayId);
    }
}
