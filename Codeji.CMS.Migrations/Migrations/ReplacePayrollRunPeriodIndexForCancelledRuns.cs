using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Allows a cancelled draft/blocked run to be replaced for the same payroll scope,
/// while retaining uniqueness for every active or completed run.
/// </summary>
public sealed class ReplacePayrollRunPeriodIndexForCancelledRuns : IMigration
{
    private const string LegacyIndexName = "ux_payrollrun_company_period_type_population";
    private const string ActiveIndexName = "ux_payrollrun_company_period_type_population_active";

    public string Id => $"2026-10-05-{nameof(ReplacePayrollRunPeriodIndexForCancelledRuns)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var runs = db.GetCollection<BsonDocument>("PayrollRun");

        // Populate the partial-index discriminator while the existing full
        // unique index is still present. This ensures every non-cancelled
        // legacy run is protected by the replacement index as soon as it is
        // created, and cancelled runs are consistently excluded.
        await runs.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Eq("Status", "CANCELLED"),
            Builders<BsonDocument>.Update.Set("IsActive", false));
        await runs.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Ne("Status", "CANCELLED"),
            Builders<BsonDocument>.Update.Set("IsActive", true));

        var duplicate = await runs.Aggregate()
            .Match(new BsonDocument("IsActive", true))
            .Group(new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "CompanyId", "$CompanyId" }, { "PeriodStart", "$PeriodStart" },
                        { "RunType", "$RunType" }, { "PopulationType", "$PopulationType" },
                        { "DepartmentId", "$DepartmentId" }, { "PayGroupId", "$PayGroupId" }
                    }
                },
                { "count", new BsonDocument("$sum", 1) }
            })
            .Match(new BsonDocument("count", new BsonDocument("$gt", 1)))
            .FirstOrDefaultAsync();
        if (duplicate is not null)
            throw new InvalidOperationException("Active payroll runs contain duplicate company/period/type/population scopes. Resolve these records before replacing the payroll-run unique index.");

        using var cursor = await runs.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();
        var current = indexes.FirstOrDefault(index => index.GetValue("name", "").AsString == ActiveIndexName);
        var expectedPartialFilter = new BsonDocument("IsActive", true);
        if (current is not null)
        {
            var currentPartialFilter = current.GetValue("partialFilterExpression", BsonNull.Value);
            if (!currentPartialFilter.Equals(expectedPartialFilter))
            {
                await runs.Indexes.DropOneAsync(ActiveIndexName);
                current = null;
            }
        }

        if (current is null)
        {
            // Keep the original full unique index in AddPayrollRunIndexes for
            // older installs. Switch it only after the preflight above.
            await runs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                new BsonDocument
                {
                    { "CompanyId", 1 }, { "PeriodStart", 1 }, { "RunType", 1 },
                    { "PopulationType", 1 }, { "DepartmentId", 1 }, { "PayGroupId", 1 }
                },
                new CreateIndexOptions<BsonDocument>
                {
                    // Create the new guard alongside the legacy index first,
                    // avoiding an unprotected window during index replacement.
                    Name = ActiveIndexName,
                    Unique = true,
                    PartialFilterExpression = expectedPartialFilter
                }));
        }

        var legacy = indexes.FirstOrDefault(index => index.GetValue("name", "").AsString == LegacyIndexName);
        if (legacy is not null)
            await runs.Indexes.DropOneAsync(LegacyIndexName);

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
