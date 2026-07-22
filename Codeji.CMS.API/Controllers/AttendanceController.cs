using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Codeji.CMS.Services.Attendance;
using Newtonsoft.Json;
using Codeji.CMS.Utility.middlewares;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Constraints;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/admin/attendance")]
[Authorize]
public class AdminAttendanceController : ControllerBase
{
    private readonly IAdminAttendanceService _service;
    private readonly IHttpContextAccessor _context;

    public AdminAttendanceController(IAdminAttendanceService service, IHttpContextAccessor context)
    {
        _service = service;
        _context = context;
    }

    [HttpPost]
    [ModulePermission(AppModule.Attendance, Permission.Create)]
    public async Task<IActionResult> Add([FromBody] AdminAttendanceCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _service.AddManualAttendance(CurrentContext.CompanyId(_context), dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("{userId}")]
    [ModulePermission(AppModule.Attendance, Permission.View)]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var result = await _service.GetByUser(CurrentContext.CompanyId(_context), userId);
        return Ok(result);
    }

    [HttpGet("{userId}/date")]
    [ModulePermission(AppModule.Attendance, Permission.View)]
    public async Task<IActionResult> GetByUserAndDate(string userId, [FromQuery] DateTime date)
    {
        var result = await _service.GetByUserAndDate(CurrentContext.CompanyId(_context), userId, date);
        if (result == null)
            return NotFound("Attendance not found.");

        return Ok(result);
    }

    [HttpPut("{userId}")]
    [ModulePermission(AppModule.Attendance, Permission.Edit)]
    public async Task<IActionResult> Update(
        string userId,
        [FromQuery] DateTime date,
        [FromBody] AttendanceUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        bool success;
        try { success = await _service.UpdateAttendance(CurrentContext.CompanyId(_context), userId, date, dto); }
        catch (InvalidOperationException ex) { return Conflict(new { success = false, message = ex.Message }); }

        if (!success)
            return NotFound("Attendance not found.");

        return Ok("Updated successfully.");
    }

    [HttpPost("GetAllAttendanceItems")]
    [ModulePermission(AppModule.Attendance, Permission.View)]
    public async Task<IActionResult> GetAll([FromBody] AttendanceCalendarRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request payload is null.");

        if (dto.FromDate == default || dto.ToDate == default || dto.FromDate.Date > dto.ToDate.Date)
            return BadRequest("Invalid date range.");

        var allData = await _service.GetAttendanceByDateRange(
            CurrentContext.CompanyId(_context),
            dto.FromDate,
            dto.ToDate,
            dto.UserIds);

        return Ok(new { items = allData });
    }
}
