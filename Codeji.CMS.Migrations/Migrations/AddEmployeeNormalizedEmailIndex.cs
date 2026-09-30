using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddEmployeeNormalizedEmailIndex : IMigration
{
    public string Id => $"2026-09-23-{nameof(AddEmployeeNormalizedEmailIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var rows = await employees.Find(new BsonDocument()).ToListAsync();
        // Match the active-record scope of the unique index; deleted employee records must not block migration.
        var duplicateGroups = rows
            .Where(x => x.TryGetValue("Email", out var email) && email.IsString && !string.IsNullOrWhiteSpace(email.AsString) && x.TryGetValue("IsDeleted", out var deleted) && deleted.IsBoolean && !deleted.AsBoolean)
            .GroupBy(x => x["Email"].AsString.Trim().ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key)
            .Take(20)
            .ToList();
        if (duplicateGroups.Count > 0)
        {
            var duplicateDetails = duplicateGroups.Select(group =>
            {
                var records = string.Join("; ", group.Select(row =>
                {
                    var employeeId = row.TryGetValue("EmployeeId", out var id) && id.IsString ? id.AsString : row["_id"].ToString();
                    var companyId = row.TryGetValue("CompanyId", out var company) && company.IsString ? company.AsString : "unknown-company";
                    return $"employee={employeeId}, company={companyId}";
                }));
                return $"{group.Key} [{records}]";
            });
            throw new InvalidOperationException($"Cannot create normalized employee email uniqueness index. Resolve duplicate employee records first (max 20 groups shown): {string.Join(" | ", duplicateDetails)}");
        }

        foreach (var row in rows)
        {
            if (!row.TryGetValue("Email", out var email) || !email.IsString || string.IsNullOrWhiteSpace(email.AsString)) continue;
            var normalized = email.AsString.Trim().ToLowerInvariant();
            await employees.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", row["_id"]),
                Builders<BsonDocument>.Update.Set("EmailNormalized", normalized));
        }

        var filter = new BsonDocument
        {
            { "EmailNormalized", new BsonDocument { { "$exists", true }, { "$type", "string" }, { "$gt", "" } } },
            { "IsDeleted", false }
        };
        await employees.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument("EmailNormalized", 1),
            new CreateIndexOptions<BsonDocument> { Name = "ux_empuser_email_normalized", Unique = true, PartialFilterExpression = filter }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
