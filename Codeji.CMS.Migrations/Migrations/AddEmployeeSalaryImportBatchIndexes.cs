using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddEmployeeSalaryImportBatchIndexes : IMigration
{
    public string Id => $"2026-09-30-{nameof(AddEmployeeSalaryImportBatchIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var collection = db.GetCollection<BsonDocument>("EmployeeSalaryImportBatch");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument(new[] { new BsonElement("CompanyId", 1), new BsonElement("BatchId", 1) }),
            new CreateIndexOptions { Name = "ux_salary_import_batch_company_batch", Unique = true }));
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument(new[] { new BsonElement("CompanyId", 1), new BsonElement("Status", 1), new BsonElement("UpdatedAtUtc", -1) }),
            new CreateIndexOptions { Name = "ix_salary_import_batch_company_status_updated" }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}