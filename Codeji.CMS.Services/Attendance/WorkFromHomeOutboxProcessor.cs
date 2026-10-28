using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Attendance;

public interface IWorkFromHomeOutboxProcessor
{
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default);
}

/// <summary>Retries tenant-scoped WFH audit and notification work committed with request transitions.</summary>
public sealed class WorkFromHomeOutboxProcessor(
    IMongoDbRepository<WorkFromHomeOutboxEvent> outbox,
    IMongoDbRepository<WorkFromHomeRequestLog> logs,
    IMongoDbRepository<Notifications> notificationRecords,
    IMongoDbRepository<UserNotifications> userNotifications,
    INotificationService notifications,
    IMiddlewareService mail,
    ILogger<WorkFromHomeOutboxProcessor> logger,
    IMongoDbRepository<WorkFromHomeRequest>? requests = null) : IWorkFromHomeOutboxProcessor
{
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var collection = outbox.GetCollection();
        var filter = Builders<WorkFromHomeOutboxEvent>.Filter.And(
            Builders<WorkFromHomeOutboxEvent>.Filter.Eq(x => x.IsDeleted, false),
            Builders<WorkFromHomeOutboxEvent>.Filter.Or(
                Builders<WorkFromHomeOutboxEvent>.Filter.And(
                    Builders<WorkFromHomeOutboxEvent>.Filter.In(x => x.Status, new[] { "Pending", "Failed" }),
                    Builders<WorkFromHomeOutboxEvent>.Filter.Lte(x => x.AvailableAtUtc, now)),
                Builders<WorkFromHomeOutboxEvent>.Filter.And(
                    Builders<WorkFromHomeOutboxEvent>.Filter.Eq(x => x.Status, "Processing"),
                    Builders<WorkFromHomeOutboxEvent>.Filter.Lt(x => x.ProcessingStartedAtUtc, now.AddMinutes(-10)))));
        var item = await collection.FindOneAndUpdateAsync(filter,
            Builders<WorkFromHomeOutboxEvent>.Update.Set(x => x.Status, "Processing").Set(x => x.ProcessingStartedAtUtc, now).Inc(x => x.AttemptCount, 1),
            new FindOneAndUpdateOptions<WorkFromHomeOutboxEvent> { ReturnDocument = ReturnDocument.After, Sort = Builders<WorkFromHomeOutboxEvent>.Sort.Ascending(x => x.AvailableAtUtc) }, cancellationToken);
        if (item is null) return false;

        try
        {
            if (string.IsNullOrWhiteSpace(item.CompanyId) || string.IsNullOrWhiteSpace(item.EventId))
                throw new InvalidOperationException("WFH outbox event is missing tenant identity.");

            if (string.IsNullOrWhiteSpace(item.RequestId))
            {
                var dedupeParts = item.DedupeKey?.Split(':');
                if (dedupeParts?.Length >= 2 && !string.IsNullOrWhiteSpace(dedupeParts[1]))
                {
                    item.RequestId = dedupeParts[1];
                }
                else if (requests is not null && item.CreatedDate.HasValue)
                {
                    var windowStart = item.CreatedDate.Value.AddMinutes(-10);
                    var windowEnd = item.CreatedDate.Value.AddMinutes(10);
                    var matchingReq = await requests.FirstOrDefault(
                        x => x.CompanyId == item.CompanyId &&
                             (x.CreatedBy == item.ActorUserId || x.UserId == item.ActorUserId) &&
                             x.CreatedDate >= windowStart && x.CreatedDate <= windowEnd);
                    if (matchingReq is not null && !string.IsNullOrWhiteSpace(matchingReq.RequestId))
                    {
                        item.RequestId = matchingReq.RequestId;
                    }
                }

                if (!string.IsNullOrWhiteSpace(item.RequestId))
                {
                    await UpdateAsync(item, Builders<WorkFromHomeOutboxEvent>.Update.Set(x => x.RequestId, item.RequestId), cancellationToken);
                }
                else
                {
                    logger.LogWarning("WFH outbox event {EventId} for company {CompanyId} has unrecoverable missing request identity. Marking as DeadLetter.", item.EventId, item.CompanyId);
                    await UpdateAsync(item, Builders<WorkFromHomeOutboxEvent>.Update
                        .Set(x => x.Status, "DeadLetter")
                        .Set(x => x.CompletedAtUtc, DateTime.UtcNow)
                        .Set(x => x.LastError, "Missing tenant or request identity"), cancellationToken);
                    return true;
                }
            }

            if (item.EventType == "AUDIT") await PersistAuditAsync(item, cancellationToken);
            else if (item.EventType == "NOTIFICATION") await PersistNotificationAsync(item, now, cancellationToken);
            else throw new InvalidOperationException("WFH outbox event type is not supported.");

            if (item.SendEmail && item.EmailSentAtUtc is null)
            {
                if (string.IsNullOrWhiteSpace(item.RecipientEmail) || string.IsNullOrWhiteSpace(item.EmailSubject))
                    throw new InvalidOperationException("WFH email outbox event is missing recipient or subject.");
                var emailResult = await mail.EmailSendAndSaveWithResult(new EmpEmailLogs
                {
                    CompanyId = item.CompanyId,
                    UserTo = item.RecipientUserId ?? string.Empty,
                    UserFrom = item.ActorUserId,
                    Email = item.RecipientEmail,
                    Subject = item.EmailSubject,
                    Body = item.EmailBody ?? string.Empty,
                    EmailLogType = (EnumsHelper.MailType)(item.EmailType ?? 0)
                });
                if (!emailResult.IsSent) throw new InvalidOperationException(emailResult.Error.Length > 1000 ? emailResult.Error[..1000] : emailResult.Error);
                await UpdateAsync(item, Builders<WorkFromHomeOutboxEvent>.Update.Set(x => x.EmailSentAtUtc, DateTime.UtcNow), cancellationToken);
            }

            await UpdateAsync(item, Builders<WorkFromHomeOutboxEvent>.Update
                .Set(x => x.Status, "Sent")
                .Set(x => x.CompletedAtUtc, DateTime.UtcNow)
                .Set(x => x.LastError, null), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var delay = TimeSpan.FromMinutes(Math.Min(60, Math.Pow(2, Math.Min(item.AttemptCount, 6))));
            var status = item.AttemptCount >= 10 ? "DeadLetter" : "Failed";
            var error = ex.Message[..Math.Min(1000, ex.Message.Length)];
            logger.LogError(ex, "WFH outbox event {EventId} for company {CompanyId} failed on attempt {AttemptCount}.", item.EventId, item.CompanyId, item.AttemptCount);
            await UpdateAsync(item, Builders<WorkFromHomeOutboxEvent>.Update
                .Set(x => x.Status, status)
                .Set(x => x.AvailableAtUtc, DateTime.UtcNow.Add(delay))
                .Set(x => x.LastError, error), cancellationToken);
        }
        return true;
    }

    private async Task PersistAuditAsync(WorkFromHomeOutboxEvent item, CancellationToken cancellationToken)
    {
        if (item.AuditWrittenAtUtc.HasValue) return;
        var existing = await logs.FirstOrDefault(x => x.CompanyId == item.CompanyId && x.LogId == item.EventId);
        if (existing is null)
        {
            var saved = await logs.AddOne(new WorkFromHomeRequestLog
            {
                LogId = item.EventId,
                CompanyId = item.CompanyId,
                CreatedBy = item.ActorUserId,
                CreatedDate = item.CreatedDate,
                RequestId = item.RequestId,
                PreviousStatus = item.PreviousStatus,
                NewStatus = item.NewStatus ?? string.Empty,
                Action = item.Action ?? string.Empty,
                PerformedByUserId = item.ActorUserId,
                PerformedAt = item.CreatedDate ?? DateTime.UtcNow,
                RemarksCode = item.RemarksCode,
                RemarksText = item.RemarksText,
                Version = item.RequestVersion
            });
            if (!saved.Success)
            {
                // A prior attempt may have committed the log and lost its acknowledgement.
                // Confirm by the stable tenant/event key before retrying the operation.
                existing = await logs.FirstOrDefault(x => x.CompanyId == item.CompanyId && x.LogId == item.EventId);
                if (existing is null) throw new InvalidOperationException(saved.Message ?? "WFH audit log could not be persisted.");
            }
        }
        await UpdateAsync(item, Builders<WorkFromHomeOutboxEvent>.Update.Set(x => x.AuditWrittenAtUtc, DateTime.UtcNow), cancellationToken);
    }

    private async Task PersistNotificationAsync(WorkFromHomeOutboxEvent item, DateTime now, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(item.RecipientUserId) || string.IsNullOrWhiteSpace(item.Title))
            throw new InvalidOperationException("WFH notification outbox event is missing recipient or title.");
        if (!item.NotificationPersistedAtUtc.HasValue)
        {
            var notification = new Notifications
            {
                NotificationId = $"wfh-{item.EventId}",
                CompanyId = item.CompanyId,
                CreatedBy = item.ActorUserId,
                CreatedDateTime = now,
                TargetId = item.RequestId,
                Title = item.Title,
                Body = item.Body ?? string.Empty,
                NotificationType = (EnumsHelper.NotificationTypes)(item.NotificationType ?? (int)EnumsHelper.NotificationTypes.WorkFromHomeRequest)
            };
            var existingNotification = await notificationRecords.FirstOrDefault(x => x.CompanyId == item.CompanyId && x.NotificationId == notification.NotificationId);
            if (existingNotification is null)
            {
                var savedNotification = await notificationRecords.AddOne(notification);
                if (!savedNotification.Success)
                {
                    existingNotification = await notificationRecords.FirstOrDefault(x => x.CompanyId == item.CompanyId && x.NotificationId == notification.NotificationId);
                    if (existingNotification is null) throw new InvalidOperationException(savedNotification.Message ?? "WFH notification record could not be persisted.");
                }
            }
            var recipient = new UserNotifications
            {
                UserNotificationId = $"wfh-{item.EventId}",
                CompanyId = item.CompanyId,
                UserId = item.RecipientUserId,
                NotificationId = notification.NotificationId,
                IsRead = false,
                CreatedDateTime = now
            };
            var existingRecipient = await userNotifications.FirstOrDefault(x => x.UserId == item.RecipientUserId && x.NotificationId == notification.NotificationId && x.UserNotificationId == recipient.UserNotificationId);
            if (existingRecipient is null)
            {
                var savedRecipient = await userNotifications.AddOne(recipient);
                if (!savedRecipient.Success)
                {
                    existingRecipient = await userNotifications.FirstOrDefault(x => x.UserId == item.RecipientUserId && x.NotificationId == notification.NotificationId && x.UserNotificationId == recipient.UserNotificationId);
                    if (existingRecipient is null) throw new InvalidOperationException(savedRecipient.Message ?? "WFH recipient notification could not be persisted.");
                }
            }
            await UpdateAsync(item, Builders<WorkFromHomeOutboxEvent>.Update.Set(x => x.NotificationPersistedAtUtc, DateTime.UtcNow), cancellationToken);
        }
        if (!item.LivePushSentAtUtc.HasValue)
        {
            await notifications.SendNotificationToUser(item.RecipientUserId, new NotificationViewModel
            {
                UserNotificationId = $"wfh-{item.EventId}",
                Title = item.Title,
                Body = item.Body ?? string.Empty,
                TargetId = item.RequestId,
                SentBy = item.ActorUserId,
                SentDateTime = now,
                NotificationTypes = (EnumsHelper.NotificationTypes)(item.NotificationType ?? (int)EnumsHelper.NotificationTypes.WorkFromHomeRequest)
            });
            await UpdateAsync(item, Builders<WorkFromHomeOutboxEvent>.Update.Set(x => x.LivePushSentAtUtc, DateTime.UtcNow), cancellationToken);
        }
    }

    private async Task UpdateAsync(WorkFromHomeOutboxEvent item, UpdateDefinition<WorkFromHomeOutboxEvent> update, CancellationToken cancellationToken)
    {
        var result = await outbox.GetCollection().UpdateOneAsync(
            Builders<WorkFromHomeOutboxEvent>.Filter.Where(x => x.CompanyId == item.CompanyId && x.EventId == item.EventId),
            update,
            cancellationToken: cancellationToken);
        if (result.MatchedCount != 1)
            throw new InvalidOperationException("WFH outbox event disappeared or changed tenant while being processed.");
    }
}
