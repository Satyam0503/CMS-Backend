using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Attendance;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Attendance;

public interface IAttendanceOutboxProcessor { Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default); }

/// <summary>Claims a durable attendance notification and retries failed live delivery with bounded backoff.</summary>
public sealed class AttendanceOutboxProcessor(
    IMongoDbRepository<AttendanceNotificationOutbox> outbox,
    INotificationService notifications) : IAttendanceOutboxProcessor
{
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var collection = outbox.GetCollection();
        var filter = Builders<AttendanceNotificationOutbox>.Filter.And(
            Builders<AttendanceNotificationOutbox>.Filter.Lte(x => x.AvailableAtUtc, now),
            Builders<AttendanceNotificationOutbox>.Filter.In(x => x.Status, new[] { "Pending", "Failed" }));
        var item = await collection.FindOneAndUpdateAsync(filter,
            Builders<AttendanceNotificationOutbox>.Update.Set(x => x.Status, "Processing").Set(x => x.ProcessingStartedAtUtc, now).Inc(x => x.AttemptCount, 1),
            new FindOneAndUpdateOptions<AttendanceNotificationOutbox> { ReturnDocument = ReturnDocument.After, Sort = Builders<AttendanceNotificationOutbox>.Sort.Ascending(x => x.AvailableAtUtc) }, cancellationToken);
        if (item is null) return false;
        try
        {
            await notifications.SendNotificationToUser(item.RecipientUserId, new NotificationViewModel { UserNotificationId = item.OutboxId, Title = item.Title, Body = item.Body, TargetId = item.AggregateId, SentDateTime = now, SentBy = "system" });
            await collection.UpdateOneAsync(x => x.OutboxId == item.OutboxId, Builders<AttendanceNotificationOutbox>.Update.Set(x => x.Status, "Sent").Set(x => x.SentAtUtc, DateTime.UtcNow).Set(x => x.LastError, null), cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            var delay = TimeSpan.FromMinutes(Math.Min(60, Math.Pow(2, Math.Min(item.AttemptCount, 6))));
            var state = item.AttemptCount >= 10 ? "DeadLetter" : "Failed";
            var error = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            await collection.UpdateOneAsync(x => x.OutboxId == item.OutboxId, Builders<AttendanceNotificationOutbox>.Update.Set(x => x.Status, state).Set(x => x.LastError, error).Set(x => x.AvailableAtUtc, DateTime.UtcNow.Add(delay)), cancellationToken: cancellationToken);
        }
        return true;
    }
}
