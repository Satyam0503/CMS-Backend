using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Prevents duplicate WFH request creation when clients retry an ambiguous response.</summary>
public sealed class AddWorkFromHomeRequestIdempotency : IMigration
{
    public string Id => $"2026-10-26-{nameof(AddWorkFromHomeRequestIdempotency)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var requests = db.GetCollection<BsonDocument>("WorkFromHomeRequest");
        await requests.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("UserId").Ascending("IdempotencyKey"),
            new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_wfh_request_company_user_idempotency",
                Unique = true,
                PartialFilterExpression = new BsonDocument("IdempotencyKey", new BsonDocument("$type", "string"))
            }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
