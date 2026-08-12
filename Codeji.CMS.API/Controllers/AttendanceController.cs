using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Codeji.CMS.DTO.Attendance;
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
    [ModulePermission(AppModule.Attendance, Permission.CreateForEmployee)]
    public async Task<IActionResult> Add([FromBody] AdminAttendanceCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _service.AddManualAttendance(CurrentContext.CompanyId(_context), CurrentContext.UserId(_context), dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "ATTENDANCE_OPERATION_FAILED"
            });
        }
    }

    [HttpGet("{userId}")]
    [ModulePermission(AppModule.Attendance, Permission.ViewAll)]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var result = await _service.GetByUser(CurrentContext.CompanyId(_context), userId);
        return Ok(result);
    }

    [HttpGet("{userId}/date")]
    [ModulePermission(AppModule.Attendance, Permission.ViewAll)]
    public async Task<IActionResult> GetByUserAndDate(string userId, [FromQuery] DateTime date)
    {
        var result = await _service.GetByUserAndDate(CurrentContext.CompanyId(_context), userId, date);
        if (result == null)
            return NotFound("Attendance not found.");

        return Ok(result);
    }

    [HttpPut("{userId}")]
    [ModulePermission(AppModule.Attendance, Permission.Override)]
    public async Task<IActionResult> Update(
        string userId,
        [FromQuery] DateTime date,
        [FromBody] AttendanceUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        bool success;
        try { success = await _service.UpdateAttendance(CurrentContext.CompanyId(_context), CurrentContext.UserId(_context), userId, date, dto); }
        catch (InvalidOperationException ex) { return Conflict(new { success = false, message = ex.Message }); }

        if (!success)
            return NotFound("Attendance not found.");

        return Ok("Updated successfully.");
    }

    [HttpPost("GetAllAttendanceItems")]
    [ModulePermission(AppModule.Attendance, Permission.ViewAll)]
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

    [HttpGet("export/monthly")]
    [ModulePermission(AppModule.Attendance, Permission.ViewAll)]
    public async Task<IActionResult> ExportMonthly([FromQuery] int year, [FromQuery] int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            return BadRequest("A valid report month is required.");

        var export = await _service.ExportMonthlyAttendanceAsync(CurrentContext.CompanyId(_context), year, month);
        return File(
            export.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            export.FileName);
    }

    [HttpPost("initialize-month")]
    [ModulePermission(AppModule.Attendance, Permission.Override)]
    public async Task<IActionResult> InitializeMonth([FromBody] AttendanceInitializationRequestDto request,
        [FromServices] IAttendanceInitializationService initialization)
    {
        if (request.Month == default) return BadRequest(new { message = "ATTENDANCE_MONTH_REQUIRED" });
        var result = await initialization.InitializeMonthAsync(CurrentContext.CompanyId(_context), CurrentContext.UserId(_context), request.Month, HttpContext.RequestAborted);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("correction-requests")]
    [ModulePermission(AppModule.Attendance, Permission.ViewAll)]
    public async Task<IActionResult> CorrectionRequests([FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Attendance.AttendanceCorrectionRequest> corrections)
        => Ok(new { items = await corrections.GetAll(x => x.CompanyId == CurrentContext.CompanyId(_context) && x.Status == "Pending") });

    [HttpPut("correction-requests/{requestId}")]
    [ModulePermission(AppModule.Attendance, Permission.Override)]
    public async Task<IActionResult> ReviewCorrection(string requestId, [FromBody] AttendanceCorrectionReviewDto model,
        [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Attendance.AttendanceCorrectionRequest> corrections,
        [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.UserNotifications> userNotifications,
        [FromServices] Codeji.CMS.GenericRepository.Interfaces.IMongoDbRepository<Codeji.CMS.Repository.Entities.Notifications> notifications)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var companyId = CurrentContext.CompanyId(_context);
        var request = await corrections.FirstOrDefault(x => x.Id == requestId && x.CompanyId == companyId);
        if (request is null) return NotFound("ATTENDANCE_CORRECTION_NOT_FOUND");
        if (request.Status != "Pending") return Conflict("ATTENDANCE_CORRECTION_ALREADY_REVIEWED");
        request.Status = model.Resolution; request.Resolution = model.Resolution; request.ResolutionNote = model.Note?.Trim(); request.ReviewedByUserId = CurrentContext.UserId(_context); request.ReviewedAtUtc = DateTime.UtcNow;
        var saved = await corrections.Update(MongoDB.Driver.Builders<Codeji.CMS.Repository.Entities.Attendance.AttendanceCorrectionRequest>.Filter.Eq(x => x.Id, request.Id), request);
        if (!saved.Success) return StatusCode(500);
        var note = new Codeji.CMS.Repository.Entities.Notifications { NotificationId = Guid.NewGuid().ToString(), CompanyId = companyId, CreatedBy = request.ReviewedByUserId, CreatedDateTime = DateTime.UtcNow, TargetId = request.Id, Title = "Attendance correction reviewed", Body = $"Your attendance correction request for {request.AttendanceDate:dd MMM yyyy} was {model.Resolution.ToLowerInvariant()}.", NotificationType = Codeji.CMS.Utility.Enums.EnumsHelper.NotificationTypes.LeaveRequest };
        if ((await notifications.AddOne(note)).Success) await userNotifications.AddOne(new Codeji.CMS.Repository.Entities.UserNotifications { UserNotificationId = Guid.NewGuid().ToString(), UserId = request.UserId, NotificationId = note.NotificationId, CreatedDateTime = DateTime.UtcNow });
        return Ok(new { request.Id, request.Status, request.Resolution, request.ReviewedAtUtc });
    }

    [HttpGet("my-calendar")]
    public async Task<IActionResult> GetMyCalendar([FromQuery] int year, [FromQuery] int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            return BadRequest("A valid calendar year and month are required.");

        var fromDate = new DateTime(year, month, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);
        var userId = CurrentContext.UserId(_context);
        var attendance = await _service.GetAttendanceByDateRange(
            CurrentContext.CompanyId(_context),
            fromDate,
            toDate,
            new[] { userId });

        return Ok(new { items = attendance });
    }
}
