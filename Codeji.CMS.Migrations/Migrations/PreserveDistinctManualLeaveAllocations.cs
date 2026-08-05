using Codeji.CMS.Migrations.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Preserves existing employee-specific allocations that differ from the policy default.</summary>
public sealed class PreserveDistinctManualLeaveAllocations : IMigration
{
    public string Id => $"2026-08-04-{nameof(PreserveDistinctManualLeaveAllocations)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<BsonDocument>("Migration");
        if (await migrations.Find(Builders<BsonDocument>.Filter.Eq("_id", Id)).AnyAsync()) return;

        var policies = await db.GetCollection<BsonDocument>("LeavePolicy").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var defaultAllocationByPolicy = policies
            .Where(policy => policy.GetValue("_id", BsonNull.Value).IsString)
            .ToDictionary(policy => policy["_id"].AsString, policy => DecimalValue(policy, "AccrualAmount"), StringComparer.Ordinal);
        var balances = db.GetCollection<BsonDocument>("EmployeeLeaveBalance");
        var rows = await balances.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var markedManual = 0;
        foreach (var balance in rows)
        {
            var policyId = balance.GetValue("LeavePolicyId", BsonNull.Value);
            if (!policyId.IsString || !defaultAllocationByPolicy.TryGetValue(policyId.AsString, out var policyAllocation)) continue;
            if (DecimalValue(balance, "TotalAllocated") == policyAllocation) continue;
            await balances.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", balance["_id"]),
                Builders<BsonDocument>.Update.Set("IsManualAllocation", true));
            markedManual++;
        }

        Console.WriteLine($"{{\"migration\":\"{Id}\",\"markedManual\":{markedManual}}}");
        await migrations.InsertOneAsync(new BsonDocument { { "_id", Id }, { "ExecutedAt", DateTime.UtcNow } });
    }

    private static decimal DecimalValue(BsonDocument row, string field) =>
        row.TryGetValue(field, out var value) && !value.IsBsonNull
            ? Convert.ToDecimal(value.ToString(), System.Globalization.CultureInfo.InvariantCulture)
            : 0m;
}
