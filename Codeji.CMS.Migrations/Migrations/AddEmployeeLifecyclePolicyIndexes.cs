using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Prevents nondeterministic tenant policy resolution while preserving soft-deleted history.</summary>
public sealed class AddEmployeeLifecyclePolicyIndexes : IMigration
{
    public string Id => $"2026-09-08-05-{nameof(AddEmployeeLifecyclePolicyIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var policies = db.GetCollection<BsonDocument>("EmployeeLifecyclePolicy");
        var duplicates = await policies.Aggregate()
            .Match(new BsonDocument { { "IsDeleted", false }, { "IsActive", true } })
            .Group(new BsonDocument { { "_id", new BsonDocument { { "CompanyId", "$CompanyId" }, { "EventType", "$EventType" } } }, { "count", new BsonDocument("$sum", 1) } })
            .Match(new BsonDocument("count", new BsonDocument("$gt", 1)))
            .ToListAsync();
        if (duplicates.Count > 0)
            throw new InvalidOperationException($"EMPLOYEE_LIFECYCLE_POLICY_DUPLICATES: {duplicates.Count} active company/event group(s) must be resolved before the unique index can be created.");

        await policies.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("EventType"),
            new CreateIndexOptions<BsonDocument> { Name = "ux_employee_lifecycle_policy_company_event", Unique = true, PartialFilterExpression = new BsonDocument { { "IsDeleted", false }, { "IsActive", true } } }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
