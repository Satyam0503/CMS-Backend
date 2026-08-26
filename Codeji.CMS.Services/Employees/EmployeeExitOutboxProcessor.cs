using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Employees;

public interface IEmployeeExitOutboxProcessor
{
    Task ProcessPendingAsync(CancellationToken cancellationToken = default);
}

/// <summary>Claims exit events atomically and retries actual in-app notification delivery.</summary>
public sealed class EmployeeExitOutboxProcessor(
    IMongoDbRepository<EmployeeExitOutbox> outbox,
    INotificationService notifications) : IEmployeeExitOutboxProcessor
{
    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
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
                await notifications.SendNotificationToUser(item.RecipientUserId, new NotificationViewModel
                {
                    UserNotificationId = item.EmployeeExitOutboxId, Title = item.Title, Body = item.Body,
                    TargetId = item.EmployeeExitId, SentBy = "system", SentDateTime = now
                });
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
}
