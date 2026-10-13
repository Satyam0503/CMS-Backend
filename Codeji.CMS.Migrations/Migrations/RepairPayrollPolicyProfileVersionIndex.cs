using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class RepairPayrollPolicyProfileVersionIndex : IMigration
{
    public string Id => $"2026-09-30-Z-{nameof(RepairPayrollPolicyProfileVersionIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var collection = db.GetCollection<BsonDocument>("PayrollPolicyProfile");
        var duplicate = await collection.Aggregate()
            .Group(new BsonDocument
            {
                { "_id", new BsonDocument { { "CompanyId", "$CompanyId" }, { "Name", "$Name" }, { "EffectiveFrom", "$EffectiveFrom" } } },
                { "count", new BsonDocument("$sum", 1) }
            })
            .Match(new BsonDocument("count", new BsonDocument("$gt", 1)))
            .FirstOrDefaultAsync();
        if (duplicate is not null)
            throw new InvalidOperationException("Payroll policy profiles contain duplicate company, name, and start date values. Resolve them before replacing the incorrect company-wide version index.");

        // Version is the optimistic-concurrency counter of one profile, not a
        // sequence shared by every pay group and company default profile.
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument { { "CompanyId", 1 }, { "Name", 1 }, { "EffectiveFrom", 1 } },
            new CreateIndexOptions { Name = "ux_policy_profile_company_name_start", Unique = true }));

        using var cursor = await collection.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();
        if (indexes.Any(x => x.GetValue("name", "").AsString == "ux_policy_profile_company_version"))
            await collection.Indexes.DropOneAsync("ux_policy_profile_company_version");

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
