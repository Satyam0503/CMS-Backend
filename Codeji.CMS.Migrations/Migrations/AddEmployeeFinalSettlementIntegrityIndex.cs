using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Prevents multiple tenant-owned final settlements for one exit case.</summary>
public sealed class AddEmployeeFinalSettlementIntegrityIndex : IMigration
{
    public string Id => $"2026-09-29-{nameof(AddEmployeeFinalSettlementIntegrityIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var settlements = db.GetCollection<BsonDocument>(nameof(EmployeeFinalSettlement));
        await settlements.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("EmployeeExitId"),
            new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_final_settlement_company_exit",
                Unique = true,
                PartialFilterExpression = new BsonDocument("IsDeleted", false)
            }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}