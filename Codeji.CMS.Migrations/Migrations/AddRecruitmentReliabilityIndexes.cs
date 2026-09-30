using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddRecruitmentReliabilityIndexes : IMigration
{
    public string Id => $"2026-09-21-{typeof(AddRecruitmentReliabilityIndexes).Name}";
    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        await CreateUniqueString(db, "RecruitmentAutomationOutboxEvent", "ux_recruitment_automation_company_idempotency", "CompanyId", "IdempotencyKey");
        await CreateUniqueString(db, "RecruitmentRelationshipLink", "ux_recruitment_relationship_company_idempotency", "CompanyId", "LastCommandIdempotencyKey");
        await CreateUniqueString(db, "RecruitmentEmailOutbox", "ux_recruitment_email_company_dedupe", "CompanyId", "DedupeKey");
        await CreateUnique(db, "RecruitmentOfferVersion", "ux_recruitment_offer_version_company_offer_number", "CompanyId", "OfferId", "VersionNumber");
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
    private static Task CreateUnique(IMongoDatabase db, string collection, string name, params string[] fields)
        => CreateIndex(db, collection, name, fields, null);

    private static Task CreateUniqueString(IMongoDatabase db, string collection, string name, params string[] fields)
    {
        var filter = new BsonDocument(fields[^1], new BsonDocument { { "$type", "string" }, { "$gt", "" } });
        return CreateIndex(db, collection, name, fields, filter);
    }

    private static async Task CreateIndex(IMongoDatabase db, string collection, string name, string[] fields, BsonDocument? filter)
    {
        try
        {
            var options = new CreateIndexOptions<BsonDocument> { Name = name, Unique = true, PartialFilterExpression = filter };
            await db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(fields.Select(x => new BsonElement(x, 1))), options));
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            Console.WriteLine($"Skipped {name}: {collection} contains legacy duplicate keys.");
        }
    }
}


