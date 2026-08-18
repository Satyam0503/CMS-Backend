using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Microsoft.Extensions.Logging;

namespace Codeji.CMS.Services.Attendance;

public interface IAttendanceReminderProcessor
{
    Task ProcessAsync(DateTime? referenceTime = null, string reminderStage = "first", CancellationToken cancellationToken = default);
    Task ProcessAutomaticPresentAsync(DateTime? referenceTime = null, CancellationToken cancellationToken = default);
    Task ProcessReviewReminderAsync(DateTime? referenceTime = null, CancellationToken cancellationToken = default);
}

public sealed class AttendanceReminderProcessor(
    IMongoDbRepository<EmpUser> employees,
    IMongoDbRepository<AttendanceModel> attendance,
    IMongoDbRepository<Notifications> notifications,
    IMongoDbRepository<UserNotifications> userNotifications,
    IMongoDbRepository<Roles> roles,
    IMiddlewareService middlewareService,
    INotificationService notificationService,
    IAttendanceMutationValidator mutationValidator,
    IAttendanceInitializationService initialization,
    IEffectiveOfficeScheduleService schedules,
    ICompanyWorkingCalendarService calendar,
    IMongoDbRepository<Company> companies,
    ILogger<AttendanceReminderProcessor> logger) : IAttendanceReminderProcessor
{
    public async Task ProcessAutomaticPresentAsync(DateTime? referenceTime = null, CancellationToken cancellationToken = default)
    {
        var reference = referenceTime ?? IndiaTime.Now;
        var today = reference.Date;
        // Each tenant is a separate unit of work.  Never read configuration or employee rows
        // for one company while creating attendance for another.
        var companyIds = (await companies.GetAll(x => x.Status && !x.IsDeleted, WithDeletedObjects: false, withDefaultFilter: false))
            .Select(x => x.CompanyId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var companyId in companyIds)
        {
            try
            {
                if (!await calendar.IsWorkingDayAsync(companyId, DateOnly.FromDateTime(today), cancellationToken))
                {
                    logger.LogInformation("Automatic present skipped for company {CompanyId}: today is not a working day", companyId);
                    continue;
                }

                var presentCode = await initialization.ResolvePresentStatusCodeAsync(companyId, cancellationToken);
                if (string.IsNullOrWhiteSpace(presentCode))
                {
                    logger.LogWarning("Automatic present skipped for company {CompanyId}: ATTENDANCE_PRESENT_STATUS_NOT_CONFIGURED", companyId);
                    continue;
                }

                var created = 0;
                var existing = 0;
                foreach (var employee in await employees.GetAll(x => x.CompanyId == companyId && x.Status && !x.IsDeleted, WithDeletedObjects: false, withDefaultFilter: false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var schedule = await schedules.ResolveAsync(companyId, employee, today, cancellationToken);
                    if (schedule is null || !AttendanceReminderEvaluator.HasOfficeStarted(reference, schedule.StartTime)) continue;

                    var prepared = await mutationValidator.PrepareAsync(new AttendanceMutationRequest
                    {
                        CompanyId = companyId,
                        ActorUserId = "system",
                        TargetUserId = employee.UserId,
                        AttendanceDate = DateOnly.FromDateTime(today),
                        StatusCode = presentCode,
                        TimingMode = AttendanceTimingMode.Auto,
                        ExistingRecordPolicy = ExistingAttendancePolicy.CreateMissingOnly,
                        SourceType = "SYSTEM_OFFICE_START_PRESENT",
                        RemarkCode = "SYSTEM_OFFICE_START_PRESENT",
                        OperatorRemark = "System Present created at the effective office start time."
                    }, cancellationToken);

                    if (!prepared.Success || prepared.MethodResult is null)
                    {
                        logger.LogWarning("Automatic present skipped for employee {EmployeeId} in company {CompanyId}: {Reason}", employee.EmployeeId, companyId, prepared.Message);
                        continue;
                    }
                    if (!prepared.MethodResult.ShouldWrite)
                    {
                        existing++;
                        continue;
                    }
                    try
                    {
                        var save = await attendance.AddOne(prepared.MethodResult.Attendance);
                        if (save.Success) created++;
                        else logger.LogWarning("Automatic present could not be saved for employee {EmployeeId} in company {CompanyId}", employee.EmployeeId, companyId);
                    }
                    catch (MongoDB.Driver.MongoWriteException)
                    {
                        // The unique company/user/date index can race another scheduler instance;
                        // both outcomes mean a row now exists and no row was overwritten.
                        existing++;
                    }
                }
                logger.LogInformation("Automatic present completed for company {CompanyId}: {Created} created, {Existing} existing", companyId, created, existing);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One tenant must not prevent another tenant's scheduled run.
                logger.LogError(ex, "Automatic present failed for company {CompanyId}", companyId);
            }
        }
    }

    public async Task ProcessReviewReminderAsync(DateTime? referenceTime = null, CancellationToken cancellationToken = default)
    {
        var reference = referenceTime ?? IndiaTime.Now;
        if (!AttendanceReminderEvaluator.IsReviewReminderDue(reference)) return;

        var today = reference.Date;
        var autoMarked = (await attendance.GetAll(x => x.Date.Date == today && x.SourceType == "SYSTEM_OFFICE_START_PRESENT", WithDeletedObjects: false, withDefaultFilter: false)).ToList();
        var activeEmployees = (await employees.GetAll(x => x.Status && !x.IsDeleted && !string.IsNullOrWhiteSpace(x.CompanyId), WithDeletedObjects: false, withDefaultFilter: false)).ToList();
        foreach (var companyGroup in activeEmployees.GroupBy(x => x.CompanyId, StringComparer.OrdinalIgnoreCase))
        {
            var companyId = companyGroup.Key;
            var targetId = $"attendance-review-{today:yyyy-MM-dd}";
            // Hosted services have no authenticated HTTP tenant context.  Use the
            // explicit company predicate without the repository's request filter;
            // otherwise this idempotency check always sees no notification and
            // creates the same review reminder every scheduler minute.
            var reviewReminderAlreadyCreated = (await notifications.GetAll(
                x => x.CompanyId == companyId && x.TargetId == targetId,
                WithDeletedObjects: false,
                withDefaultFilter: false)).Any();
            if (reviewReminderAlreadyCreated)
            {
                logger.LogInformation("Skipping duplicate attendance review reminder for company {CompanyId}", companyId);
                continue;
            }

            var reviewers = await GetReviewersAsync(companyId);
            if (reviewers.Count == 0) continue;

            var autoMarkedCount = autoMarked
                .Where(x => string.Equals(x.CompanyId, companyId, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.UserId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            var notification = new Notifications
            {
                NotificationId = Guid.NewGuid().ToString(), CompanyId = companyId, CreatedBy = "system", CreatedDateTime = DateTime.UtcNow,
                TargetId = targetId, Title = "Review today's attendance",
                Body = autoMarkedCount > 0
                    ? $"{autoMarkedCount} employee(s) were automatically marked Present at office start. Open Attendance to confirm the records and update any absence, Sick Leave, Earned Leave, or other exception."
                    : "Open Attendance to review today's records and update any absence, Sick Leave, Earned Leave, or other exception.",
                NotificationType = EnumsHelper.NotificationTypes.Notice
            };
            if (!(await notifications.AddOne(notification)).Success) continue;

            var userItems = reviewers.Select(reviewer => new UserNotifications { UserNotificationId = Guid.NewGuid().ToString(), UserId = reviewer.UserId, NotificationId = notification.NotificationId, IsRead = false, CreatedDateTime = DateTime.UtcNow }).ToList();
            await userNotifications.AddMany(userItems);
            foreach (var item in userItems)
                await notificationService.SendNotificationToUser(item.UserId, new NotificationViewModel { UserNotificationId = item.UserNotificationId, Title = notification.Title, Body = notification.Body, TargetId = targetId, SentBy = "system", SentDateTime = DateTime.UtcNow, NotificationTypes = EnumsHelper.NotificationTypes.Notice });
        }
    }

    private async Task<List<EmpUser>> GetReviewersAsync(string companyId)
    {
        var reviewerRoleIds = (await roles.GetAll(r => r.CompanyId == companyId && !r.IsDeleted &&
            (r.RoleType == (int)EnumsHelper.Roles.Administrator || r.RoleType == (int)EnumsHelper.Roles.HR || r.RoleType == (int)EnumsHelper.Roles.HRExecutive),
            WithDeletedObjects: false, withDefaultFilter: false))
            .Select(r => r.RolesId).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet();
        if (reviewerRoleIds.Count == 0) return [];
        return (await employees.GetAll(e => e.CompanyId == companyId && e.Status && !e.IsDeleted && reviewerRoleIds.Contains(e.RoleId), WithDeletedObjects: false, withDefaultFilter: false)).ToList();
    }

    public async Task ProcessAsync(DateTime? referenceTime = null, string reminderStage = "first", CancellationToken cancellationToken = default)
    {
        var reference = referenceTime ?? IndiaTime.Now;
        var today = reference.Date;
        var allEmployees = (await employees.GetAll(x => x.Status && !x.IsDeleted, WithDeletedObjects: false, withDefaultFilter: false)).ToList();
        var todayAttendance = (await attendance.GetAll(x => x.Date.Date == today.Date, WithDeletedObjects: false, withDefaultFilter: false)).ToList();
        var missing = AttendanceReminderEvaluator.FindMissingAttendance(allEmployees, todayAttendance, today);
        if (missing.Count == 0)
        {
            return;
        }

        var companyIds = missing
            .Select(x => x.CompanyId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var companyId in companyIds)
        {
            var companyMissing = missing.Where(x => string.Equals(x.CompanyId, companyId, StringComparison.OrdinalIgnoreCase)).ToList();
            var hrAdminRoleIds = (await roles.GetAll(r => r.CompanyId == companyId && !r.IsDeleted && (r.RoleType == (int)EnumsHelper.Roles.HR || r.RoleType == (int)EnumsHelper.Roles.HRExecutive || r.RoleType == (int)EnumsHelper.Roles.Administrator), WithDeletedObjects: false, withDefaultFilter: false))
                .Select(r => r.RolesId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            if (hrAdminRoleIds.Count == 0)
            {
                continue;
            }

            var recipientUsers = (await employees.GetAll(e => e.CompanyId == companyId && e.Status && !e.IsDeleted && hrAdminRoleIds.Contains(e.RoleId), WithDeletedObjects: false, withDefaultFilter: false)).ToList();
            var recipientIds = recipientUsers.Select(x => x.UserId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (recipientIds.Count == 0)
            {
                continue;
            }

            var detailList = string.Join("\n", companyMissing.Select(x => $"- {x.EmployeeId} - {x.FirstName} {x.LastName}"));
            var title = "Attendance reminder";
            var body = $"The following employees have not marked attendance by {reference:hh:mm tt} today:\n{detailList}";

            var targetId = $"attendance-reminder-{today:yyyy-MM-dd}-{reminderStage}";
            var existingReminder = await notifications.FirstOrDefault(x => x.CompanyId == companyId && x.TargetId == targetId, WithDeletedObjects: false);
            if (existingReminder != null)
            {
                logger.LogInformation("Skipping duplicate attendance reminder for company {CompanyId} and stage {ReminderStage}", companyId, reminderStage);
                continue;
            }

            var notification = new Notifications
            {
                NotificationId = Guid.NewGuid().ToString(),
                CompanyId = companyId,
                CreatedBy = "system",
                CreatedDateTime = DateTime.UtcNow,
                TargetId = targetId,
                Title = title,
                Body = body,
                NotificationType = EnumsHelper.NotificationTypes.Notice
            };

            var addNotification = await notifications.AddOne(notification);
            if (!addNotification.Success)
            {
                logger.LogWarning("Failed to create attendance reminder notification for company {CompanyId}", companyId);
                continue;
            }

            var userItems = recipientIds.Select(userId => new UserNotifications
            {
                UserNotificationId = Guid.NewGuid().ToString(),
                UserId = userId,
                NotificationId = notification.NotificationId,
                IsRead = false,
                CreatedDateTime = DateTime.UtcNow
            }).ToList();

            await userNotifications.AddMany(userItems);
            foreach (var item in userItems)
            {
                await notificationService.SendNotificationToUser(item.UserId, new NotificationViewModel
                {
                    UserNotificationId = item.UserNotificationId,
                    Title = title,
                    Body = body,
                    TargetId = notification.TargetId,
                    SentDateTime = DateTime.UtcNow,
                    SentBy = "system",
                    NotificationTypes = EnumsHelper.NotificationTypes.Notice
                });
            }

            foreach (var recipient in recipientUsers)
            {
                if (string.IsNullOrWhiteSpace(recipient.Email))
                {
                    continue;
                }

                var emailLog = new EmpEmailLogs
                {
                    EmailLogId = Guid.NewGuid().ToString(),
                    UserTo = recipient.UserId ?? recipient.EmployeeId ?? recipient.Email,
                    Email = recipient.Email,
                    Subject = "Attendance not marked yet",
                    Body = $"Hello {recipient.FirstName} {recipient.LastName},\n\nThe following employees have not marked attendance for {today:dd MMM yyyy} yet:\n{detailList}",
                    EmailLogType = EnumsHelper.MailType.AttendanceReminder,
                    UserFrom = "system",
                    CompanyId = companyId,
                    Status = false,
                    ErrorMessage = string.Empty
                };

                await middlewareService.EmailSendAndSave(emailLog);
            }
        }
    }
}
