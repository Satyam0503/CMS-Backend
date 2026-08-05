using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Makes the legacy semantic (every existing policy is ordinary leave) explicit.
/// This is idempotent and intentionally does not attempt to infer WFH from a name
/// or code, which would be unsafe for tenant data.
/// </summary>
public sealed class BackfillLeavePolicyTypes : IMigration
{
    public string Id => $"2026-08-04-{nameof(BackfillLeavePolicyTypes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var policies = db.GetCollection<BsonDocument>("LeavePolicy");
        var missingType = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("PolicyType", false),
            Builders<BsonDocument>.Filter.Eq("PolicyType", BsonNull.Value));
        await policies.UpdateManyAsync(missingType,
            Builders<BsonDocument>.Update.Set("PolicyType", 1));

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
