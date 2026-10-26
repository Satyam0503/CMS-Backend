using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Adds the tenant-scoped inbox index without changing existing inbox rows.</summary>
public sealed class AddUserNotificationCompanyScopeIndex : IMigration
{
    public string Id => $"2026-10-26-{nameof(AddUserNotificationCompanyScopeIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        await db.GetCollection<BsonDocument>(nameof(UserNotifications)).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("UserId").Descending("CreatedDateTime"),
                new CreateIndexOptions { Name = "ix_usernotifications_company_user_created" }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
