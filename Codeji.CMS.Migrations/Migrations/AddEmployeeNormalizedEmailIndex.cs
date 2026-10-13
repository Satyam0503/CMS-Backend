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

        // Multi-tenant isolation: email uniqueness is enforced per tenant (CompanyId).
        // Distinct tenants are isolated and may have employees with identical email addresses.
        // Deleted employee records must not block active records.
        var duplicateGroups = rows
            .Where(x => x.TryGetValue("Email", out var email) && email.IsString && !string.IsNullOrWhiteSpace(email.AsString) &&
                        x.TryGetValue("CompanyId", out var company) && company.IsString && !string.IsNullOrWhiteSpace(company.AsString) &&
                        x.TryGetValue("IsDeleted", out var deleted) && deleted.IsBoolean && !deleted.AsBoolean)
            .GroupBy(x => new
            {
                CompanyId = x["CompanyId"].AsString.Trim(),
                EmailNormalized = x["Email"].AsString.Trim().ToLowerInvariant()
            })
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key.CompanyId)
            .ThenBy(g => g.Key.EmailNormalized)
            .Take(20)
            .ToList();

        if (duplicateGroups.Count > 0)
        {
            var duplicateDetails = duplicateGroups.Select(group =>
            {
                var records = string.Join("; ", group.Select(row =>
                {
                    var employeeId = row.TryGetValue("EmployeeId", out var id) && id.IsString ? id.AsString : row["_id"].ToString();
                    return $"employee={employeeId}";
                }));
                return $"company={group.Key.CompanyId}, email={group.Key.EmailNormalized} [{records}]";
            });
            throw new InvalidOperationException($"Cannot create tenant-scoped normalized employee email uniqueness index. Resolve duplicate employee records first (max 20 groups shown): {string.Join(" | ", duplicateDetails)}");
        }

        foreach (var row in rows)
        {
            if (!row.TryGetValue("Email", out var email) || !email.IsString || string.IsNullOrWhiteSpace(email.AsString)) continue;
            var normalized = email.AsString.Trim().ToLowerInvariant();
            if (row.TryGetValue("EmailNormalized", out var existing) && existing.IsString && existing.AsString == normalized) continue;

            await employees.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", row["_id"]),
                Builders<BsonDocument>.Update.Set("EmailNormalized", normalized));
        }

        // Drop any legacy global index that violated tenant isolation
        await DropIndexIfExistsAsync(employees, "ux_empuser_email_normalized");
        await DropIndexIfExistsAsync(employees, "ux_empuser_company_email_normalized");

        var filter = new BsonDocument
        {
            { "CompanyId", new BsonDocument { { "$exists", true }, { "$type", "string" }, { "$gt", "" } } },
            { "EmailNormalized", new BsonDocument { { "$exists", true }, { "$type", "string" }, { "$gt", "" } } },
            { "IsDeleted", false }
        };

        await employees.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument
            {
                { "CompanyId", 1 },
                { "EmailNormalized", 1 }
            },
            new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_empuser_company_email_normalized",
                Unique = true,
                PartialFilterExpression = filter
            }));

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static async Task DropIndexIfExistsAsync(IMongoCollection<BsonDocument> collection, string indexName)
    {
        try
        {
            await collection.Indexes.DropOneAsync(indexName);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexNotFound")
        {
        }
    }
}
