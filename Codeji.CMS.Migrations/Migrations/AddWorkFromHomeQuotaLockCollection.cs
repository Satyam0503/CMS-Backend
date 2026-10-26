using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Pre-creates the shared WFH quota lock collection for transactional claims.</summary>
public sealed class AddWorkFromHomeQuotaLockCollection : IMigration
{
    public string Id => $"2026-10-26-{nameof(AddWorkFromHomeQuotaLockCollection)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        try
        {
            await db.CreateCollectionAsync("WorkFromHomeQuotaLock");
        }
        catch (MongoCommandException ex) when (ex.Code == 48)
        {
            // Existing collection is already ready for transactional upserts.
        }

        // MongoDB creates the unique _id index automatically. The service addresses
        // lock documents exclusively by their deterministic _id.
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
