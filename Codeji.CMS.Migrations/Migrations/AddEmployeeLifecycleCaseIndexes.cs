using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Supports tenant-safe lifecycle queues and idempotent leave-to-case lookup.</summary>
public sealed class AddEmployeeLifecycleCaseIndexes : IMigration
{
    public string Id => $"2026-09-09-{nameof(AddEmployeeLifecycleCaseIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var cases = db.GetCollection<BsonDocument>("EmployeeLifecycleCase");
        await cases.Indexes.CreateManyAsync([
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("LeaveRequestId"), new CreateIndexOptions { Name = "ix_lifecycle_case_company_leave_request" }),
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("EmployeeUserId").Ascending("Status"), new CreateIndexOptions { Name = "ix_lifecycle_case_company_employee_status" }),
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("Status").Ascending("ExpectedReturnDate"), new CreateIndexOptions { Name = "ix_lifecycle_case_company_status_return" })
        ]);
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
