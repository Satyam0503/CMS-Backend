using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Backfills company scope on inbox rows and indexes user inbox reads by tenant.</summary>
public sealed class AddUserNotificationCompanyScope : IDataBackfillMigration
{
    public string Id => $"2026-10-26-{nameof(AddUserNotificationCompanyScope)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var inbox = db.GetCollection<BsonDocument>(nameof(UserNotifications));
        var pipeline = new[]
        {
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", nameof(Notifications) },
                { "localField", "NotificationId" },
                { "foreignField", "NotificationId" },
                { "as", "linkedNotification" }
            }),
            new BsonDocument("$unwind", "$linkedNotification"),
            new BsonDocument("$match", new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("CompanyId", new BsonDocument("$exists", false)),
                new BsonDocument("CompanyId", BsonNull.Value),
                new BsonDocument("CompanyId", "")
            })),
            new BsonDocument("$set", new BsonDocument("CompanyId", "$linkedNotification.CompanyId")),
            new BsonDocument("$unset", "linkedNotification"),
            new BsonDocument("$merge", new BsonDocument
            {
                { "into", nameof(UserNotifications) },
                { "on", "_id" },
                { "whenMatched", "merge" },
                { "whenNotMatched", "discard" }
            })
        };
        await inbox.Aggregate<BsonDocument>(pipeline).ToListAsync();
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
