using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddCareerDiscoveryIndexes : IMigration
{
    public string Id => $"2026-11-18-{nameof(AddCareerDiscoveryIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var jobs = db.GetCollection<BsonDocument>("JobVacancy");
        await jobs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument
            {
                { "CompanyId", 1 },
                { "PublishToCareerPortal", 1 },
                { "Status", 1 },
                { "IsDeleted", 1 },
                { "PublishedAt", -1 },
                { "CreatedDate", -1 }
            },
            new CreateIndexOptions { Name = "ix_career_company_discovery" }));
        await jobs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument
            {
                { "PublishToMasterPortal", 1 },
                { "PublishToCareerPortal", 1 },
                { "Status", 1 },
                { "IsDeleted", 1 },
                { "PublishedAt", -1 },
                { "CreatedDate", -1 }
            },
            new CreateIndexOptions { Name = "ix_career_master_discovery" }));

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
