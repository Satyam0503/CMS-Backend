using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Supports bounded tenant-scoped leave request cohort lookups by submission time.</summary>
public sealed class AddLeaveRequestTenantCreatedDateIndex : IMigration
{
    public string Id => $"2026-11-17-{nameof(AddLeaveRequestTenantCreatedDateIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var requests = db.GetCollection<BsonDocument>("LeaveRequest");
        await requests.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("CreatedDate"),
            new CreateIndexOptions<BsonDocument> { Name = "ix_leave_request_tenant_created_date" }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
