using Codeji.CMS.API.App_Start;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Utility.middlewares;
using Codeji.CMS.Utility.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController, Authorize, Route("api/attendance/me")]
// These endpoints derive the employee from the JWT and never accept a user id.
// An active authenticated employee must therefore be able to read their own
// attendance even when a tenant has not yet received the ViewOwn permission
// migration. Company-wide attendance remains protected by ViewAll on the admin
// controller.
public sealed class MyAttendanceController(IAdminAttendanceService attendance,
    IMongoDbRepository<EmpUser> employees, IMongoDbRepository<AttendanceCorrectionRequest> corrections,
    IMongoDbRepository<Roles> roles, IMongoDbRepository<Notifications> notifications,
    IMongoDbRepository<UserNotifications> userNotifications, INotificationService notificationService,
    IHttpContextAccessor context) : ControllerBase
{
    [HttpGet("grid")]
    public async Task<IActionResult> Grid([FromQuery] int year, [FromQuery] int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12) return BadRequest("INVALID_ATTENDANCE_MONTH");
        var companyId = CurrentContext.CompanyId(context);
        var userId = CurrentContext.UserId(context);
        if (await employees.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == userId && x.Status && !x.IsDeleted) is null)
            return NotFound("ATTENDANCE_SELF_ACCESS_DENIED");
        var from = new DateTime(year, month, 1);
        var items = await attendance.GetAttendanceByDateRange(companyId, from, from.AddMonths(1).AddDays(-1), [userId]);
        return Ok(new { items });
    }

    // Canonical employee calendar endpoint. The older admin-route equivalent is retained
    // for existing clients while profile UI migrates to this token-derived contract.
    [HttpGet("calendar")]
    public Task<IActionResult> Calendar([FromQuery] int year, [FromQuery] int month) => Grid(year, month);

    [HttpPost("correction-requests")]
    public async Task<IActionResult> RequestCorrection([FromBody] AttendanceCorrectionRequestDto model)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var companyId = CurrentContext.CompanyId(context);
        var userId = CurrentContext.UserId(context);
        var record = await attendance.GetByUserAndDate(companyId, userId, model.AttendanceDate.Date);
        if (record is null) return NotFound("ATTENDANCE_RECORD_UNAVAILABLE");
        var duplicate = await corrections.Exist(x => x.CompanyId == companyId && x.UserId == userId && x.AttendanceDate == model.AttendanceDate.Date && x.Status == "Pending");
        if (duplicate) return Conflict("ATTENDANCE_CORRECTION_ALREADY_PENDING");
        var request = new AttendanceCorrectionRequest { CompanyId = companyId, UserId = userId, AttendanceDate = model.AttendanceDate.Date, Reason = model.Reason.Trim(), ReviewDueAt = AddBusinessDays(DateTime.UtcNow, 5) };
        var result = await corrections.AddOne(request);
        if (result.Success)
        {
            var roleIds = (await roles.GetAll(x => x.CompanyId == companyId && (x.RoleType == (int)EnumsHelper.Roles.Administrator || x.RoleType == (int)EnumsHelper.Roles.HR || x.RoleType == (int)EnumsHelper.Roles.HRExecutive))).Select(x => x.RolesId).ToHashSet();
            var recipients = (await employees.GetAll(x => x.CompanyId == companyId && x.Status && roleIds.Contains(x.RoleId))).Select(x => x.UserId).Distinct().ToList();
            if (recipients.Count > 0)
            {
                var note = new Notifications { NotificationId = Guid.NewGuid().ToString(), CompanyId = companyId, CreatedBy = userId, CreatedDateTime = DateTime.UtcNow, TargetId = request.Id, Title = "Attendance correction request", Body = $"An employee requested an attendance correction for {request.AttendanceDate:dd MMM yyyy}.", NotificationType = EnumsHelper.NotificationTypes.LeaveRequest };
                if ((await notifications.AddOne(note)).Success)
                {
                    var items = recipients.Select(id => new UserNotifications { UserNotificationId = Guid.NewGuid().ToString(), UserId = id, NotificationId = note.NotificationId, CreatedDateTime = DateTime.UtcNow }).ToList();
                    await userNotifications.AddMany(items);
                    await Task.WhenAll(items.Select(item => notificationService.SendNotificationToUser(item.UserId, new Codeji.CMS.DTO.ResponseModel.NotificationViewModel { UserNotificationId = item.UserNotificationId, Title = note.Title, Body = note.Body, TargetId = note.TargetId, SentBy = userId, SentDateTime = DateTime.UtcNow, NotificationTypes = note.NotificationType })));
                }
            }
        }
        return result.Success ? Created($"api/attendance/me/correction-requests/{request.Id}", new { request.Id, request.Status }) : StatusCode(500);
    }

    [HttpGet("correction-requests")]
    public async Task<IActionResult> MyCorrectionRequests() => Ok(new
    {
        items = await corrections.GetAll(x => x.CompanyId == CurrentContext.CompanyId(context) && x.UserId == CurrentContext.UserId(context))
    });

    private static DateTime AddBusinessDays(DateTime value, int count)
    {
        while (count > 0) { value = value.AddDays(1); if (value.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) count--; }
        return value;
    }
}
