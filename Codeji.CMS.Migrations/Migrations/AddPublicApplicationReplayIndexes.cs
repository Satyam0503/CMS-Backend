using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddPublicApplicationReplayIndexes : IMigration
{
    public string Id => $"2026-11-18-{nameof(AddPublicApplicationReplayIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        await CreateUniqueNonEmptyStringIndex(
            db,
            "RecruitmentJobApplication",
            "ux_public_application_replay_company_key",
            "PublicSubmissionIdempotencyKey");
        await CreateUniqueNonEmptyStringIndex(
            db,
            "ApplicantLogs",
            "ux_applicant_log_company_idempotency_key",
            "IdempotencyKey");

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static Task CreateUniqueNonEmptyStringIndex(
        IMongoDatabase db,
        string collectionName,
        string indexName,
        string keyField)
    {
        var filter = new BsonDocument(keyField, new BsonDocument
        {
            { "$type", "string" },
            { "$gt", "" }
        });
        var index = new CreateIndexModel<BsonDocument>(
            new BsonDocument { { "CompanyId", 1 }, { keyField, 1 } },
            new CreateIndexOptions<BsonDocument>
            {
                Name = indexName,
                Unique = true,
                PartialFilterExpression = filter
            });
        return db.GetCollection<BsonDocument>(collectionName).Indexes.CreateOneAsync(index);
    }
}
