using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Adds tenant-scoped indexes for the additive lifecycle read model. The indexes
/// are intentionally non-unique because lifecycle history is append-only and
/// legacy employee records may not yet have a lifecycle document.
/// </summary>
public sealed class AddEmployeeLifecycleIndexes : IMigration
{
    public string Id => $"2026-09-07-02-{nameof(AddEmployeeLifecycleIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var employees = db.GetCollection<BsonDocument>("EmpUser");
        await employees.Indexes.CreateManyAsync([
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("Lifecycle.EmploymentStatus"),
                new CreateIndexOptions { Name = "ix_empuser_company_lifecycle_employment" }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("Lifecycle.AvailabilityStatus"),
                new CreateIndexOptions { Name = "ix_empuser_company_lifecycle_availability" }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("Lifecycle.Version"),
                new CreateIndexOptions { Name = "ix_empuser_company_lifecycle_version" })
        ]);

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
