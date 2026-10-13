using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Initializes the additive contract read model without changing employment or exit authority.</summary>
public sealed class BackfillEmployeeContractLifecycle : IMigration
{
    public string Id => $"2026-09-16-{nameof(BackfillEmployeeContractLifecycle)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var rows = await employees.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
        foreach (var row in rows)
        {
            if (row.Contains("Contract") && !row["Contract"].IsBsonNull) continue;
            var update = Builders<BsonDocument>.Update.Set("Contract", new BsonDocument
            {
                ["Status"] = "Indefinite",
                ["Version"] = 0,
                ["History"] = new BsonArray()
            });
            await employees.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", row["_id"]), update);
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
