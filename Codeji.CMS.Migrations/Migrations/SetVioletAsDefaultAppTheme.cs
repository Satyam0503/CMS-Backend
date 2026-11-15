using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Violet replaces Glass as the product default; companies still on the old default (or unset) move to it.</summary>
public sealed class SetVioletAsDefaultAppTheme : IMigration
{
    public string Id => $"2026-11-15-01-{nameof(SetVioletAsDefaultAppTheme)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var onOldDefault = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("AppTheme", false),
            Builders<BsonDocument>.Filter.Eq("AppTheme", BsonNull.Value),
            Builders<BsonDocument>.Filter.In("AppTheme", new BsonValue[] { string.Empty, "glass", "Glass", "GLASS" }));
        await db.GetCollection<BsonDocument>("Company").UpdateManyAsync(onOldDefault,
            Builders<BsonDocument>.Update.Set("AppTheme", "violet"));

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
