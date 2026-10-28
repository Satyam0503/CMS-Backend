using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.Services.Attendance;
using Newtonsoft.Json;
using Codeji.CMS.Utility.middlewares;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Employees.Interface;

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
    public async Task<IActionResult> Add(
        [FromBody] AdminAttendanceCreateDto dto,
        [FromServices] IRoleService roleService)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (dto.AllowSourceOverride)
        {
            var hasAccess = await roleService.VerifyUserAccess(
                AppModule.Attendance,
                [Permission.Override, Permission.Edit],
                CurrentContext.UserId(_context),
                CurrentContext.CompanyId(_context)
            );
            if (!hasAccess)
                return Forbid();
        }

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

    [HttpDelete("{attendanceId}")]
    [HttpDelete("DeleteAttendance/{attendanceId}")]
    [ModulePermission(AppModule.Attendance, Permission.Override)]
    public async Task<IActionResult> DeleteAttendance(
        string attendanceId,
        [FromServices] IMongoDbRepository<AttendanceModel> attendanceRepo,
        [FromServices] IMongoDbRepository<MonthlyAttendanceSummary> summaries,
        [FromServices] IAttendanceAuditWriter auditWriter)
    {
        var companyId = CurrentContext.CompanyId(_context);
        var actorUserId = CurrentContext.UserId(_context);

        var existing = await attendanceRepo.FirstOrDefault(x => x.AttendanceId == attendanceId && x.CompanyId == companyId);
        if (existing is null)
            return NotFound(new { success = false, message = "ATTENDANCE_NOT_FOUND" });

        var month = new DateTime(existing.Date.Year, existing.Date.Month, 1);
        var isLocked = await summaries.Exist(x => x.CompanyId == companyId && x.PayrollMonth == month && x.IsLocked);
        if (isLocked)
            return Conflict(new { success = false, message = "ATTENDANCE_MONTH_LOCKED" });

        var deleteResult = await attendanceRepo.Delete(MongoDB.Driver.Builders<AttendanceModel>.Filter.Eq(x => x.AttendanceId, existing.AttendanceId));
        if (!deleteResult.Success)
            return StatusCode(500, new { success = false, message = "ATTENDANCE_DELETE_FAILED" });

        await auditWriter.WriteMutationAsync(companyId, actorUserId, "DELETED", existing);
        await summaries.DeleteAll(MongoDB.Driver.Builders<MonthlyAttendanceSummary>.Filter.Where(x =>
            x.CompanyId == companyId && x.UserId == existing.UserId && x.PayrollMonth == month && !x.IsLocked));

        return Ok(new { success = true, message = "Attendance deleted successfully." });
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

    [HttpPost("effective-schedules")]
    [ModulePermission(AppModule.Attendance, Permission.ViewAll)]
    public async Task<IActionResult> GetEffectiveSchedules(
        [FromBody] EffectiveOfficeSchedulesRequest request,
        [FromServices] IEffectiveOfficeScheduleService effectiveSchedules)
    {
        if (request is null || request.AttendanceDate == default)
            return BadRequest(new { success = false, message = "ATTENDANCE_DATE_REQUIRED" });
        if (request.UserIds is null || request.UserIds.Count == 0 || request.UserIds.Count > 1000)
            return BadRequest(new { success = false, message = "EMPLOYEE_SELECTION_INVALID" });

        // The effective-schedule service independently scopes every employee,
        // department, assignment and schedule to this authenticated company.
        var items = await effectiveSchedules.ResolveManyAsync(
            CurrentContext.CompanyId(_context), request.UserIds, request.AttendanceDate, HttpContext.RequestAborted);
        return Ok(new { items });
    }

    [HttpGet("export/monthly")]
    [ModulePermission(AppModule.Attendance, Permission.ViewAll)]
    public async Task<IActionResult> ExportMonthly(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] string? departmentId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? employmentStatus = null,
        [FromQuery] bool onlyEmployed = true)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            return BadRequest("A valid report month is required.");

        var export = await _service.ExportMonthlyAttendanceAsync(
            CurrentContext.CompanyId(_context),
            year,
            month,
            departmentId,
            search,
            employmentStatus,
            onlyEmployed);
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
        [FromServices] IMongoDbRepository<AttendanceCorrectionRequest> corrections,
        [FromServices] IMongoDbRepository<Codeji.CMS.Repository.Entities.UserNotifications> userNotifications,
        [FromServices] IMongoDbRepository<Codeji.CMS.Repository.Entities.Notifications> notifications,
        [FromServices] IAttendanceRepository attendanceRepo,
        [FromServices] IMongoDbRepository<EmpUser> employees,
        [FromServices] IEffectiveOfficeScheduleService schedules,
        [FromServices] IMongoDbRepository<MonthlyAttendanceSummary> summaries,
        [FromServices] IAttendancePenaltyService penaltyService,
        [FromServices] IAttendanceAuditWriter auditWriter)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var companyId = CurrentContext.CompanyId(_context);
        var request = await corrections.FirstOrDefault(x => x.Id == requestId && x.CompanyId == companyId);
        if (request is null) return NotFound("ATTENDANCE_CORRECTION_NOT_FOUND");
        if (request.Status != "Pending") return Conflict("ATTENDANCE_CORRECTION_ALREADY_REVIEWED");
        request.Status = model.Resolution; request.Resolution = model.Resolution; request.ResolutionNote = model.Note?.Trim(); request.ReviewedByUserId = CurrentContext.UserId(_context); request.ReviewedAtUtc = DateTime.UtcNow;
        var saved = await corrections.Update(MongoDB.Driver.Builders<AttendanceCorrectionRequest>.Filter.Eq(x => x.Id, request.Id), request);
        if (!saved.Success) return StatusCode(500);

        if (string.Equals(model.Resolution, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedDate = DateTime.SpecifyKind(request.AttendanceDate.Date, DateTimeKind.Utc);
            var employee = await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.UserId && !x.IsDeleted);
            var existingAttendance = await attendanceRepo.GetByUserAndDateAsync(companyId, request.UserId, normalizedDate);

            var schedule = employee is not null
                ? await schedules.ResolveAsync(companyId, employee, normalizedDate, HttpContext.RequestAborted)
                : null;

            var istTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            DateTime checkInUtc;
            DateTime checkOutUtc;
            if (schedule is not null)
            {
                var checkInLocal = DateTime.SpecifyKind(normalizedDate.Add(schedule.StartTime), DateTimeKind.Unspecified);
                var checkOutLocal = DateTime.SpecifyKind(normalizedDate.Add(schedule.EndTime), DateTimeKind.Unspecified);
                checkInUtc = TimeZoneInfo.ConvertTimeToUtc(checkInLocal, istTz);
                checkOutUtc = TimeZoneInfo.ConvertTimeToUtc(checkOutLocal, istTz);
                if (schedule.EndTime <= schedule.StartTime)
                    checkOutUtc = checkOutUtc.AddDays(1);
            }
            else
            {
                checkInUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(normalizedDate.AddHours(9), DateTimeKind.Unspecified), istTz);
                checkOutUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(normalizedDate.AddHours(18), DateTimeKind.Unspecified), istTz);
            }

            var totalHours = (decimal)(checkOutUtc - checkInUtc).TotalHours;

            if (existingAttendance is not null)
            {
                existingAttendance.Status = "P";
                existingAttendance.CheckInTime ??= checkInUtc;
                existingAttendance.CheckOutTime ??= checkOutUtc;
                existingAttendance.TotalHours ??= totalHours;
                existingAttendance.Remarks = $"Attendance correction approved: {model.Note?.Trim() ?? request.Reason}";
                existingAttendance.OperatorRemark = model.Note?.Trim();
                existingAttendance.SystemRemark = "Approved via attendance correction request";
                existingAttendance.ModifiedByUserId = CurrentContext.UserId(_context);
                existingAttendance.ModifiedAtUtc = DateTime.UtcNow;
                existingAttendance.Version++;
                await attendanceRepo.UpdateAsync(companyId, request.UserId, normalizedDate, existingAttendance);
                await auditWriter.WriteMutationAsync(companyId, CurrentContext.UserId(_context), "UPDATED", existingAttendance);
            }
            else
            {
                var newAttendance = new AttendanceModel
                {
                    CompanyId = companyId,
                    UserId = request.UserId,
                    EmployeeId = employee?.EmployeeId ?? string.Empty,
                    Date = normalizedDate,
                    Status = "P",
                    SourceType = "CORRECTION_REQUEST",
                    SourceId = request.Id,
                    CheckInTime = checkInUtc,
                    CheckOutTime = checkOutUtc,
                    TotalHours = totalHours,
                    Remarks = $"Attendance correction approved: {model.Note?.Trim() ?? request.Reason}",
                    OperatorRemark = model.Note?.Trim(),
                    SystemRemark = "Created via approved attendance correction request",
                    MarkedByUserId = CurrentContext.UserId(_context),
                    MarkedAtUtc = DateTime.UtcNow,
                    Version = 1
                };
                await attendanceRepo.AddAsync(newAttendance);
                await auditWriter.WriteMutationAsync(companyId, CurrentContext.UserId(_context), "CREATED", newAttendance);
            }

            var monthStart = new DateTime(request.AttendanceDate.Year, request.AttendanceDate.Month, 1);
            await summaries.DeleteAll(MongoDB.Driver.Builders<MonthlyAttendanceSummary>.Filter.Where(x =>
                x.CompanyId == companyId && x.UserId == request.UserId && x.PayrollMonth == monthStart && !x.IsLocked));
            await penaltyService.Recalculate(companyId, monthStart);
        }

        var note = new Codeji.CMS.Repository.Entities.Notifications { NotificationId = Guid.NewGuid().ToString(), CompanyId = companyId, CreatedBy = request.ReviewedByUserId, CreatedDateTime = DateTime.UtcNow, TargetId = request.Id, Title = "Attendance correction reviewed", Body = $"Your attendance correction request for {request.AttendanceDate:dd MMM yyyy} was {model.Resolution.ToLowerInvariant()}.", NotificationType = Codeji.CMS.Utility.Enums.EnumsHelper.NotificationTypes.LeaveRequest };
        if ((await notifications.AddOne(note)).Success) await userNotifications.AddOne(new Codeji.CMS.Repository.Entities.UserNotifications { UserNotificationId = Guid.NewGuid().ToString(), CompanyId = companyId, UserId = request.UserId, NotificationId = note.NotificationId, CreatedDateTime = DateTime.UtcNow });
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

    [HttpGet("import/template")]
    [ModulePermission(AppModule.Attendance, Permission.CreateForEmployee)]
    public async Task<IActionResult> DownloadImportTemplate(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] IAttendanceImportService importService)
    {
        try
        {
            var companyId = CurrentContext.CompanyId(_context);
            var (fileBytes, fileName) = await importService.GenerateTemplateAsync(companyId, year, month);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("import/validate")]
    [ModulePermission(AppModule.Attendance, Permission.CreateForEmployee)]
    public async Task<IActionResult> ValidateImport(
        [FromForm] Microsoft.AspNetCore.Http.IFormFile file,
        [FromForm] int year,
        [FromForm] int month,
        [FromForm] bool preserveApprovedLeaves,
        [FromServices] IAttendanceImportService importService)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "An Excel file (.xlsx) is required." });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { success = false, message = "Only .xlsx files are supported." });

        try
        {
            var companyId = CurrentContext.CompanyId(_context);
            using var stream = file.OpenReadStream();
            var result = await importService.ValidateAsync(companyId, year, month, stream, preserveApprovedLeaves);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("import/commit")]
    [ModulePermission(AppModule.Attendance, Permission.CreateForEmployee)]
    public async Task<IActionResult> CommitImport(
        [FromForm] Microsoft.AspNetCore.Http.IFormFile file,
        [FromForm] int year,
        [FromForm] int month,
        [FromForm] bool preserveApprovedLeaves,
        [FromServices] IAttendanceImportService importService)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "An Excel file (.xlsx) is required." });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { success = false, message = "Only .xlsx files are supported." });

        try
        {
            var companyId = CurrentContext.CompanyId(_context);
            var actorUserId = CurrentContext.UserId(_context);
            using var stream = file.OpenReadStream();
            var result = await importService.CommitAsync(companyId, actorUserId, year, month, stream, preserveApprovedLeaves);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

