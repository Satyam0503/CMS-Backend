using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Codeji.CMS.Services.Attendance;
using Newtonsoft.Json;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/admin/attendance")]
[Authorize(Policy = "AdminOnly")]
public class AdminAttendanceController : ControllerBase
{
    private readonly IAdminAttendanceService _service;

    public AdminAttendanceController(IAdminAttendanceService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AdminAttendanceCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _service.AddManualAttendance(dto);
            return Ok(result);
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
    public async Task<IActionResult> GetByUser(string userId)
    {
        var result = await _service.GetByUser(userId);
        return Ok(result);
    }

    [HttpGet("{userId}/date")]
    public async Task<IActionResult> GetByUserAndDate(string userId, [FromQuery] DateTime date)
    {
        var result = await _service.GetByUserAndDate(userId, date);
        if (result == null)
            return NotFound("Attendance not found.");

        return Ok(result);
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> Update(
        string userId,
        [FromQuery] DateTime date,
        [FromBody] AttendanceUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var success = await _service.UpdateAttendance(userId, date, dto);

        if (!success)
            return NotFound("Attendance not found.");

        return Ok("Updated successfully.");
    }

    [HttpPost("GetAllAttendanceItems")]
    public async Task<IActionResult> GetAll([FromBody] AttendanceCalendarRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Request payload is null.");

        if (dto.FromDate == default || dto.ToDate == default)
            return BadRequest("Invalid date range.");

        var allData = await _service.GetAttendanceByDateRange(
            dto.FromDate,
            dto.ToDate,
            dto.UserIds);

        return Ok(new { items = allData });
    }
}
