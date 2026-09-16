using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Initializes the additive lifecycle read model without changing legacy status fields.</summary>
public sealed class BackfillEmployeeLifecycleState : IMigration
{
    public string Id => $"2026-09-07-01-{nameof(BackfillEmployeeLifecycleState)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var rows = await employees.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
        foreach (var employee in rows)
        {
            if (employee.Contains("Lifecycle") && employee["Lifecycle"].BsonType == BsonType.Document) continue;
            // Legacy Status is an account/activity flag, not proof that employment
            // ended. Only an explicit legacy employment state may classify someone
            // as former; otherwise preserve the employment relationship as Active.
            var explicitEmployment = employee.TryGetValue("EmploymentStatus", out var status) && status.IsString && !string.IsNullOrWhiteSpace(status.AsString)
                ? status.AsString
                : null;
            var employment = explicitEmployment is "Resigned" or "Terminated" or "Retired" or "ContractEnded" or "Deceased"
                ? explicitEmployment : "Active";
            var legacyAccountActive = employee.TryGetValue("Status", out var active) && active.IsBoolean && active.AsBoolean;
            var former = employment is "Resigned" or "Terminated";
            var lifecycle = new BsonDocument
            {
                ["EmploymentStatus"] = employment,
                ["AvailabilityStatus"] = former ? "Working" : "Working",
                ["AccountStatus"] = former || !legacyAccountActive ? "Closed" : "Active",
                ["PayrollStatus"] = former ? "Closed" : "Active",
                ["OffboardingStatus"] = "None",
                ["FormerPortalStatus"] = "NotEligible",
                ["AlumniStatus"] = former ? "Eligible" : "NotApplicable",
                ["Version"] = 0,
                ["History"] = new BsonArray()
            };
            await employees.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", employee["_id"]), Builders<BsonDocument>.Update.Set("Lifecycle", lifecycle));
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
