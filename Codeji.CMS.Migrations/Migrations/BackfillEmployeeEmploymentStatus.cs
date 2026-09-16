using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Preserves legacy ExitDate values while introducing the canonical offboarding lifecycle.</summary>
public sealed class BackfillEmployeeEmploymentStatus : IMigration
{
    public string Id => $"2026-08-25-01-{nameof(BackfillEmployeeEmploymentStatus)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var rows = await employees.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        foreach (var employee in rows)
        {
            var update = Builders<BsonDocument>.Update.Set("EmploymentStatus", "Active");
            if (employee.TryGetValue("ExitDate", out var legacyExit) && legacyExit.IsString && DateTime.TryParse(legacyExit.AsString, out var parsed))
                update = update.Set("EmploymentStatus", "Resigned").Set("LastWorkingDate", parsed.Date).Set("Status", false);
            if (employee.TryGetValue("EmploymentStatus", out var existing) && !existing.IsBsonNull && !string.IsNullOrWhiteSpace(existing.ToString())) continue;
            await employees.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", employee["_id"]), update);
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
