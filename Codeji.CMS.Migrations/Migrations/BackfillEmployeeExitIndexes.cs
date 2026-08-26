using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Backfills exit activity flags before enforcing one active exit per tenant employee.</summary>
public sealed class BackfillEmployeeExitIndexes : IMigration
{
    public string Id => $"2026-08-25-{nameof(BackfillEmployeeExitIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var exits = db.GetCollection<BsonDocument>("EmployeeExit");
        await exits.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Exists("IsActive", false),
            Builders<BsonDocument>.Update.Set("IsActive", true));

        // Historical duplicate active records are made inactive except the newest one,
        // so the unique index is safe to create without discarding audit history.
        var rows = await exits.Find(Builders<BsonDocument>.Filter.Eq("IsActive", true)).ToListAsync();
        foreach (var group in rows.GroupBy(x => (x.GetValue("CompanyId", "").ToString(), x.GetValue("EmployeeUserId", "").ToString())))
        {
            foreach (var duplicate in group.OrderByDescending(x => x.GetValue("CreatedDate", BsonDateTime.Create(DateTime.MinValue))).Skip(1))
                await exits.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", duplicate["_id"]),
                    Builders<BsonDocument>.Update.Set("IsActive", false).Set("Status", "Superseded"));
        }

        await exits.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("EmployeeUserId"),
            new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_employee_exit_active",
                Unique = true,
                PartialFilterExpression = Builders<BsonDocument>.Filter.Eq("IsActive", true)
            }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
