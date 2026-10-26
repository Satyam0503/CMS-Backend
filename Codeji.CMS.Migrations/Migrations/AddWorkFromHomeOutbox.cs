using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Creates durable WFH audit/notification work and tenant-scoped claim indexes.</summary>
public sealed class AddWorkFromHomeOutbox : IMigration
{
    public string Id => $"2026-10-26-{nameof(AddWorkFromHomeOutbox)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        try
        {
            await db.CreateCollectionAsync("WorkFromHomeOutboxEvent");
        }
        catch (MongoCommandException ex) when (ex.Code == 48)
        {
            // Collection already exists; indexes below remain idempotent.
        }

        var collection = db.GetCollection<BsonDocument>("WorkFromHomeOutboxEvent");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("DedupeKey"),
            new CreateIndexOptions { Name = "ux_wfh_outbox_company_dedupe", Unique = true }));
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("Status").Ascending("AvailableAtUtc").Ascending("ProcessingStartedAtUtc"),
            new CreateIndexOptions { Name = "ix_wfh_outbox_claim" }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
