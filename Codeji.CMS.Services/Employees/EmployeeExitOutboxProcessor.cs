using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;

namespace Codeji.CMS.Services.Employees;

public interface IEmployeeExitOutboxProcessor
{
    Task ProcessPendingAsync(CancellationToken cancellationToken = default);
}

/// <summary>Claims exit events atomically and retries actual in-app notification delivery.</summary>
public sealed class EmployeeExitOutboxProcessor(
    IMongoDbRepository<EmployeeExitOutbox> outbox,
    IMongoDbRepository<EmployeeExit> exits,
    IMongoDbRepository<Notifications> notificationRecords,
    IMongoDbRepository<UserNotifications> userNotifications,
    INotificationService notifications,
    IMiddlewareService mail,
    ILogger<EmployeeExitOutboxProcessor> logger,
    IMongoDbRepository<EmpUser>? employees = null,
    IRoleService? roleService = null) : IEmployeeExitOutboxProcessor
{
    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        await QueueDueRemindersAsync(cancellationToken);
        for (var processed = 0; processed < 25 && !cancellationToken.IsCancellationRequested; processed++)
        {
            var now = DateTime.UtcNow;
            var collection = outbox.GetCollection();
            var filter = Builders<EmployeeExitOutbox>.Filter.And(
                Builders<EmployeeExitOutbox>.Filter.In(x => x.Status, new[] { "Pending", "Failed" }),
                Builders<EmployeeExitOutbox>.Filter.Or(
                    Builders<EmployeeExitOutbox>.Filter.Eq(x => x.NextAttemptAtUtc, null),
                    Builders<EmployeeExitOutbox>.Filter.Lte(x => x.NextAttemptAtUtc, now)),
                Builders<EmployeeExitOutbox>.Filter.Eq(x => x.IsDeleted, false));
            var item = await collection.FindOneAndUpdateAsync(filter,
                Builders<EmployeeExitOutbox>.Update.Set(x => x.Status, "Processing").Set(x => x.ProcessingStartedAtUtc, now).Inc(x => x.Attempts, 1),
                new FindOneAndUpdateOptions<EmployeeExitOutbox> { ReturnDocument = ReturnDocument.After, Sort = Builders<EmployeeExitOutbox>.Sort.Ascending(x => x.NextAttemptAtUtc) }, cancellationToken);
            if (item is null) return;
            try
            {
                if (string.IsNullOrWhiteSpace(item.RecipientUserId) || string.IsNullOrWhiteSpace(item.Title))
                    throw new InvalidOperationException("Employee exit outbox event is missing its notification payload.");
                if (item.NotificationSentAtUtc is null)
                {
                    var notification = new Notifications { CompanyId = item.CompanyId, Title = item.Title, Body = item.Body ?? string.Empty, TargetId = item.TargetId ?? item.EmployeeExitId, NotificationType = EnumsHelper.NotificationTypes.Notice, CreatedDateTime = now, CreatedBy = item.CreatedBy };
                    var savedNotification = await notificationRecords.AddOne(notification);
                    if (!savedNotification.Success) throw new InvalidOperationException(savedNotification.Message ?? "Unable to persist employee-exit notification.");
                    var recipientNotification = new UserNotifications { UserId = item.RecipientUserId, NotificationId = notification.NotificationId, IsRead = false, CreatedDateTime = now };
                    var savedRecipient = await userNotifications.AddOne(recipientNotification);
                    if (!savedRecipient.Success) throw new InvalidOperationException(savedRecipient.Message ?? "Unable to persist recipient notification.");
                    // A persisted unread notification is the source of truth. Live delivery is an enhancement.
                    try
                    {
                        await notifications.SendNotificationToUser(item.RecipientUserId, new NotificationViewModel
                        {
                            UserNotificationId = recipientNotification.UserNotificationId, Title = item.Title, Body = item.Body,
                            TargetId = item.TargetId ?? item.EmployeeExitId, SentBy = "system", SentDateTime = now
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Employee exit notification {OutboxId} was saved for user {UserId}, but live delivery was unavailable.",
                            item.EmployeeExitOutboxId, item.RecipientUserId);
                    }
                    await collection.UpdateOneAsync(x => x.EmployeeExitOutboxId == item.EmployeeExitOutboxId,
                        Builders<EmployeeExitOutbox>.Update.Set(x => x.NotificationSentAtUtc, DateTime.UtcNow), cancellationToken: cancellationToken);
                }

                if (item.SendEmail && item.EmailSentAtUtc is null)
                {
                    if (string.IsNullOrWhiteSpace(item.RecipientEmail)) throw new InvalidOperationException("Employee exit email delivery is missing a recipient email.");
                    await mail.EmailSendAndSave(new EmpEmailLogs
                    {
                        CompanyId = item.CompanyId,
                        UserTo = item.RecipientUserId,
                        UserFrom = item.CreatedBy,
                        Email = item.RecipientEmail,
                        Subject = item.Title,
                        Body = $"<p>{WebUtility.HtmlEncode(item.Body ?? string.Empty)}</p><p>Please review your offboarding details in Codeji CMS.</p>",
                        EmailLogType = EnumsHelper.MailType.ResignationMail
                    });
                    await collection.UpdateOneAsync(x => x.EmployeeExitOutboxId == item.EmployeeExitOutboxId,
                        Builders<EmployeeExitOutbox>.Update.Set(x => x.EmailSentAtUtc, DateTime.UtcNow), cancellationToken: cancellationToken);
                }
                await collection.UpdateOneAsync(x => x.EmployeeExitOutboxId == item.EmployeeExitOutboxId,
                    Builders<EmployeeExitOutbox>.Update.Set(x => x.Status, "Sent").Set(x => x.SentAtUtc, DateTime.UtcNow).Set(x => x.LastError, null), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                var delay = TimeSpan.FromMinutes(Math.Min(60, Math.Pow(2, Math.Min(item.Attempts, 6))));
                var state = item.Attempts >= 10 ? "DeadLetter" : "Failed";
                var error = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                await collection.UpdateOneAsync(x => x.EmployeeExitOutboxId == item.EmployeeExitOutboxId,
                    Builders<EmployeeExitOutbox>.Update.Set(x => x.Status, state).Set(x => x.LastError, error).Set(x => x.NextAttemptAtUtc, DateTime.UtcNow.Add(delay)), cancellationToken: cancellationToken);
            }
        }
    }

    private async Task QueueDueRemindersAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dueCases = await exits.GetCollection().Find(x => x.IsActive && !x.IsDeleted && x.Reminders.Any(r => r.Status == "Pending" && r.DueAtUtc <= now)).Limit(25).ToListAsync(cancellationToken);
        await QueueHrDigestAsync(dueCases, cancellationToken);
        foreach (var exit in dueCases)
        {
            foreach (var reminder in exit.Reminders.Where(r => r.Status == "Pending" && r.DueAtUtc <= now))
            {
                // Claim the embedded reminder before writing the outbox event.  This makes concurrent
                // workers harmless: only the worker that changes Pending to Queued may enqueue it.
                var claim = await exits.GetCollection().UpdateOneAsync(
                    Builders<EmployeeExit>.Filter.And(
                        Builders<EmployeeExit>.Filter.Eq(x => x.CompanyId, exit.CompanyId),
                        Builders<EmployeeExit>.Filter.Eq(x => x.EmployeeExitId, exit.EmployeeExitId),
                        Builders<EmployeeExit>.Filter.ElemMatch(x => x.Reminders, r => r.ReminderId == reminder.ReminderId && r.Status == "Pending" && r.DueAtUtc <= now)),
                    new BsonDocument("$set", new BsonDocument
                    {
                        { "Reminders.$[reminder].Status", "Queued" },
                        { "Reminders.$[reminder].LastError", BsonNull.Value }
                    }),
                    new UpdateOptions { ArrayFilters = [new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument { { "reminder.ReminderId", reminder.ReminderId }, { "reminder.Status", "Pending" } })] },
                    cancellationToken);
                if (claim.ModifiedCount != 1) continue;

                var recipients = await ResolveRecipientsAsync(exit, reminder.Trigger, cancellationToken);
                if (recipients.Count == 0) continue;
                var queuedSuccess = true;
                foreach (var recipient in recipients)
                {
                    var dedupeKey = $"exit-reminder:{exit.CompanyId}:{exit.EmployeeExitId}:{reminder.ReminderId}:{recipient.UserId}";
                    if (await outbox.Exist(x => x.CompanyId == exit.CompanyId && x.DedupeKey == dedupeKey && !x.IsDeleted)) continue;
                    var queued = await outbox.AddOne(new EmployeeExitOutbox { CompanyId = exit.CompanyId, CreatedBy = "system", EmployeeExitId = exit.EmployeeExitId, EventType = "ExitReminder", DedupeKey = dedupeKey, RecipientUserId = recipient.UserId, RecipientEmail = recipient.Email, SendEmail = exit.PolicySnapshot?.NoticePeriodReminderEmail == true && !string.IsNullOrWhiteSpace(recipient.Email), Title = recipient.Title, Body = recipient.Body, TargetId = $"employee-exit-{exit.EmployeeUserId}" });
                    queuedSuccess &= queued.Success;
                }
                var finalStatus = queuedSuccess ? "Sent" : "Failed";
                await exits.GetCollection().UpdateOneAsync(
                    Builders<EmployeeExit>.Filter.And(Builders<EmployeeExit>.Filter.Eq(x => x.CompanyId, exit.CompanyId), Builders<EmployeeExit>.Filter.Eq(x => x.EmployeeExitId, exit.EmployeeExitId)),
                    new BsonDocument("$set", new BsonDocument
                    {
                        { "Reminders.$[reminder].Status", finalStatus },
                        { "Reminders.$[reminder].SentAtUtc", queuedSuccess ? new BsonDateTime(now) : BsonNull.Value },
                        { "Reminders.$[reminder].LastError", queuedSuccess ? BsonNull.Value : new BsonString("Outbox enqueue failed.") }
                    }),
                    new UpdateOptions { ArrayFilters = [new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("reminder.ReminderId", reminder.ReminderId))] },
                    cancellationToken);
            }
        }
    }

    private async Task QueueHrDigestAsync(List<EmployeeExit> dueCases, CancellationToken cancellationToken)
    {
        if (employees is null || roleService is null) return;
        foreach (var companyGroup in dueCases.Where(x => x.PolicySnapshot?.NotifyNoticePeriodHr != false).GroupBy(x => x.CompanyId))
        {
            var cases = companyGroup.Take(25).ToList();
            if (cases.Count < 5) continue;
            var activeUsers = await employees.GetAll(x => x.CompanyId == companyGroup.Key && x.Status && !x.IsDeleted);
            var hrUsers = new List<EmpUser>();
            foreach (var user in activeUsers.Where(x => !cases.Any(c => c.EmployeeUserId == x.UserId)))
                if (await roleService.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.Offboarding, [Codeji.CMS.Utility.Constraints.Permission.CaseManage], user.UserId, companyGroup.Key)) hrUsers.Add(user);
            var names = cases.Select(x => x.EmployeeUserId).Distinct().Take(10).ToList();
            var body = $"{cases.Count} notice-period reminders are due. Review the active offboarding queue." + (names.Count == 0 ? string.Empty : $" Case employee IDs: {string.Join(", ", names)}{(cases.Count > names.Count ? ", …" : ".")}");
            foreach (var user in hrUsers)
            {
                var key = $"exit-reminder-digest:{companyGroup.Key}:{user.UserId}:{DateTime.UtcNow:yyyyMMddHHmm}";
                if (await outbox.Exist(x => x.CompanyId == companyGroup.Key && x.DedupeKey == key && !x.IsDeleted)) continue;
                await outbox.AddOne(new EmployeeExitOutbox { CompanyId = companyGroup.Key, CreatedBy = "system", EmployeeExitId = cases[0].EmployeeExitId, EventType = "ExitReminderDigest", DedupeKey = key, RecipientUserId = user.UserId, RecipientEmail = user.IsEmailVerified ? user.Email : null, SendEmail = cases[0].PolicySnapshot?.NoticePeriodReminderEmail == true && user.IsEmailVerified, Title = $"{cases.Count} notice-period reminders due", Body = body, TargetId = "employee-exit-queue", NextAttemptAtUtc = DateTime.UtcNow });
            }
        }
    }

    private async Task<List<(string UserId, string? Email, string Title, string Body)>> ResolveRecipientsAsync(EmployeeExit exit, string trigger, CancellationToken cancellationToken)
    {
        if (employees is null) return string.IsNullOrWhiteSpace(exit.ReportingManagerUserId) ? [] : [(exit.ReportingManagerUserId, null, "Offboarding reminder", trigger)];
        var all = (await employees.GetAll(x => x.CompanyId == exit.CompanyId && x.Status && !x.IsDeleted)).ToList();
        var employee = all.FirstOrDefault(x => x.UserId == exit.EmployeeUserId);
        var manager = all.FirstOrDefault(x => x.UserId == exit.ReportingManagerUserId);
        var recipients = new List<(string, string?, string, string)>();
        void Add(EmpUser? user, string audience, string body)
        {
            if (user is null || user.UserId == exit.EmployeeUserId && audience != "Employee") return;
            recipients.Add((user.UserId, user.IsEmailVerified ? user.Email : null, "Notice period reminder", body));
        }
        var policy = exit.PolicySnapshot;
        if (policy?.NotifyNoticePeriodEmployee != false) Add(employee, "Employee", $"Your notice period reminder: {trigger}. Last working date: {exit.LastWorkingDate:dd MMM yyyy}.");
        if (policy?.NotifyNoticePeriodReportingManager != false) Add(manager, "Manager", $"{employee?.FirstName ?? "Employee"}'s notice period reminder: {trigger}. Last working date: {exit.LastWorkingDate:dd MMM yyyy}.");
        if (roleService is not null)
            foreach (var user in all.Where(x => x.UserId != exit.EmployeeUserId && x.UserId != exit.ReportingManagerUserId))
            {
                var allowed = await roleService.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.Offboarding, [Codeji.CMS.Utility.Constraints.Permission.CaseManage], user.UserId, exit.CompanyId);
                var admin = policy?.NotifyNoticePeriodAdmins == true && await roleService.IsRoleTypeMatch(user.RoleId, EnumsHelper.Roles.Administrator, exit.CompanyId);
                if ((allowed && policy?.NotifyNoticePeriodHr != false) || admin)
                    Add(user, admin ? "Admin" : "HR", admin ? $"Offboarding reminder for {employee?.FirstName ?? "an employee"}: {trigger}. Last working date: {exit.LastWorkingDate:dd MMM yyyy}." : $"Notice period reminder for {employee?.FirstName ?? "an employee"}: {trigger}. Last working date: {exit.LastWorkingDate:dd MMM yyyy}.");
            }
        return recipients.GroupBy(x => x.Item1).Select(x => x.First()).ToList();
    }
}
