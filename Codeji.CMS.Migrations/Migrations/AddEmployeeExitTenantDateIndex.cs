using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Supports tenant-scoped workforce movement queries by effective departure date.</summary>
public sealed class AddEmployeeExitTenantDateIndex : IMigration
{
    public string Id => $"2026-11-17-{nameof(AddEmployeeExitTenantDateIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var exits = db.GetCollection<BsonDocument>("EmployeeExit");
        await exits.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("LastWorkingDate"),
            new CreateIndexOptions<BsonDocument> { Name = "ix_employee_exit_tenant_last_working_date" }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
