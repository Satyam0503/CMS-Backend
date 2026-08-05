using Codeji.CMS.Migrations.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Backfills explicit leave-allocation fields without removing legacy fields.</summary>
public sealed class MigrateEmployeeLeaveBalanceAllocationFields : IMigration
{
    public string Id => $"2026-08-04-{nameof(MigrateEmployeeLeaveBalanceAllocationFields)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var dryRun = string.Equals(Environment.GetEnvironmentVariable("LEAVE_BALANCE_MIGRATION_DRY_RUN"), "true", StringComparison.OrdinalIgnoreCase);
        var migrations = db.GetCollection<BsonDocument>("Migration");
        if (!dryRun && await migrations.Find(Builders<BsonDocument>.Filter.Eq("_id", Id)).AnyAsync()) return;

        var balances = db.GetCollection<BsonDocument>("EmployeeLeaveBalance");
        var rows = await balances.Find(Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("TotalAllocated", false),
            Builders<BsonDocument>.Filter.Exists("Taken", false),
            Builders<BsonDocument>.Filter.Exists("Remaining", false))).ToListAsync();
        var updated = 0; var skipped = 0; var failed = 0;
        foreach (var row in rows)
        {
            try
            {
                var remaining = DecimalValue(row, "Balance");
                var taken = DecimalValue(row, "UsedBalance");
                if (remaining < 0 || taken < 0) { skipped++; continue; }
                var total = remaining + taken;
                if (!dryRun)
                {
                    await balances.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", row["_id"]), Builders<BsonDocument>.Update
                        .Set("TotalAllocated", total).Set("Taken", taken).Set("Remaining", remaining));
                }
                updated++;
            }
            catch { failed++; }
        }

        var keys = Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("UserId").Ascending("LeavePolicyId");
        if (!dryRun) await balances.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = "ux_leave_balance_company_user_policy", Unique = true }));
        Console.WriteLine($"{{\"migration\":\"{Id}\",\"dryRun\":{dryRun.ToString().ToLowerInvariant()},\"updated\":{updated},\"skipped\":{skipped},\"failed\":{failed}}}");
        if (!dryRun) await migrations.InsertOneAsync(new BsonDocument { { "_id", Id }, { "ExecutedAt", DateTime.UtcNow } });
    }

    private static decimal DecimalValue(BsonDocument row, string field) =>
        row.TryGetValue(field, out var value) && !value.IsBsonNull ? Convert.ToDecimal(value.ToString(), System.Globalization.CultureInfo.InvariantCulture) : 0m;
}
