using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Makes policy and template scope identity durable before unique indexes are applied.
/// Older duplicate rows are retained as inactive history; the newest row remains active.
/// </summary>
public sealed class HardenOffboardingPolicyTemplateIndexes : IMigration
{
    public string Id => $"2026-08-31-{nameof(HardenOffboardingPolicyTemplateIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        await BackfillPolicies(db.GetCollection<BsonDocument>("EmployeeExitPolicy"));
        await BackfillTemplates(db.GetCollection<BsonDocument>("EmployeeExitClearanceTemplate"));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static async Task BackfillPolicies(IMongoCollection<BsonDocument> collection)
    {
        var rows = await collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
        foreach (var row in rows)
        {
            var departmentId = StringValue(row, "DepartmentId");
            var roleId = StringValue(row, "RoleId");
            var key = !string.IsNullOrWhiteSpace(departmentId)
                ? $"Department:{departmentId}"
                : !string.IsNullOrWhiteSpace(roleId) && roleId != "BsonNull" ? $"Role:{roleId}" : "Company";
            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", row["_id"]), Builders<BsonDocument>.Update.Set("ScopeKey", key));
        }
        rows = await collection.Find(Builders<BsonDocument>.Filter.Eq("IsActive", true)).ToListAsync();
        foreach (var group in rows.GroupBy(x => (x.GetValue("CompanyId", "").ToString(), x.GetValue("ScopeKey", "").ToString())))
            foreach (var duplicate in group.OrderByDescending(x => x.GetValue("Version", 0).ToInt32()).ThenByDescending(x => x.GetValue("UpdatedDate", BsonDateTime.Create(DateTime.MinValue))).Skip(1))
                await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", duplicate["_id"]), Builders<BsonDocument>.Update.Set("IsActive", false));

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("ScopeKey"),
            new CreateIndexOptions<BsonDocument> { Name = "ux_employee_exit_policy_active_scope", Unique = true, PartialFilterExpression = Builders<BsonDocument>.Filter.Eq("IsActive", true) }));
    }

    private static async Task BackfillTemplates(IMongoCollection<BsonDocument> collection)
    {
        var rows = await collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
        foreach (var row in rows)
        {
            var scope = StringValue(row, "Scope", "Global");
            var departmentId = StringValue(row, "DepartmentId");
            var templateKey = NormalizeTemplateKey(StringValue(row, "TemplateKey", StringValue(row, "Category")));
            var key = scope == "Department" ? $"Department:{departmentId}:{templateKey}" : $"Global:{templateKey}";
            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", row["_id"]), Builders<BsonDocument>.Update.Set("ScopeKey", key));
        }
        rows = await collection.Find(Builders<BsonDocument>.Filter.Eq("IsActive", true)).ToListAsync();
        foreach (var group in rows.GroupBy(x => (x.GetValue("CompanyId", "").ToString(), x.GetValue("ScopeKey", "").ToString())))
            foreach (var duplicate in group.OrderByDescending(x => x.GetValue("Version", 0).ToInt32()).ThenByDescending(x => x.GetValue("UpdatedDate", BsonDateTime.Create(DateTime.MinValue))).Skip(1))
                await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", duplicate["_id"]), Builders<BsonDocument>.Update.Set("IsActive", false));

        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("ScopeKey"),
            new CreateIndexOptions<BsonDocument> { Name = "ux_employee_exit_template_active_scope", Unique = true, PartialFilterExpression = Builders<BsonDocument>.Filter.Eq("IsActive", true) }));
    }

    private static string StringValue(BsonDocument row, string name, string fallback = "")
    {
        if (!row.TryGetValue(name, out var value) || value.IsBsonNull) return fallback;
        return value.ToString();
    }

    private static string NormalizeTemplateKey(string value) =>
        Regex.Replace(value.Trim().ToUpperInvariant(), "[^A-Z0-9]+", "_").Trim('_');
}
