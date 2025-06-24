using Codeji.CMS.API.App_Start;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Holiday;
using Codeji.CMS.Services.Holiday.Interface;
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

    [Route("GetAllHoiday")]
    [HttpPost]
    // [ModulePermission("Holidays","View")]
    public async Task<Result<HolidayRequestDto>> GetAllHoliday([FromBody] HolidayFilter? filter)
    {
        return await holidayService.GetAllHoliday(filter);
    }

    [Route("CreateHoliday")]
    [HttpPost]
    // [ModulePermission("Holidays", "Create")]
    public async Task<Result> CreateHoliday(HolidayResponseDto holidayResponseDto)
    {
        return await holidayService.CreateEditHoliday(holidayResponseDto);
    }

    [Route("DeleteHoliday/{holidayId}")]
    [HttpDelete]
    public async Task<Result> DeleteHoliday(string holidayId)
    {
        return await holidayService.DeleteHoliday(holidayId);
    }
}
