using Codeji.CMS.Migrations.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Safely prepares EmployeeLeaveBalance for the company/user/policy unique index.
/// Only exact duplicate records are removed automatically. Conflicting balances are
/// retained and written to LeaveBalanceDuplicateReview for manual resolution.
/// </summary>
public sealed class DeduplicateEmployeeLeaveBalanceRecords : IMigration
{
    public string Id => $"2026-08-11-{nameof(DeduplicateEmployeeLeaveBalanceRecords)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<BsonDocument>("Migration");
        if (await migrations.Find(Builders<BsonDocument>.Filter.Eq("_id", Id)).AnyAsync()) return;

        var balances = db.GetCollection<BsonDocument>("EmployeeLeaveBalance");
        var reviews = db.GetCollection<BsonDocument>("LeaveBalanceDuplicateReview");
        var rows = await balances.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var groupedRows = rows
            .Where(HasIdentity)
            .GroupBy(row => $"{row["CompanyId"].AsString}\u001f{row["UserId"].AsString}\u001f{row["LeavePolicyId"].AsString}")
            .Where(group => group.Count() > 1)
            .ToList();

        var removed = 0;
        var unresolved = 0;
        foreach (var group in groupedRows)
        {
            var records = group.ToList();
            var exactDuplicates = records
                .Select(BalanceSignature)
                .Distinct(StringComparer.Ordinal)
                .Count() == 1;

            if (!exactDuplicates)
            {
                unresolved++;
                await reviews.ReplaceOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", group.Key),
                    new BsonDocument
                    {
                        { "_id", group.Key },
                        { "CompanyId", records[0]["CompanyId"] },
                        { "UserId", records[0]["UserId"] },
                        { "LeavePolicyId", records[0]["LeavePolicyId"] },
                        { "Status", "MANUAL_REVIEW_REQUIRED" },
                        { "Reason", "Duplicate balances have different allocation, taken, remaining, or manual-allocation values." },
                        { "Records", new BsonArray(records) },
                        { "DetectedAtUtc", DateTime.UtcNow }
                    },
                    new ReplaceOptions { IsUpsert = true });
                continue;
            }

            var canonical = records
                .OrderByDescending(row => LongValue(row, "Version"))
                .ThenByDescending(row => DateValue(row, "UpdatedDate"))
                .ThenBy(row => row["_id"].ToString(), StringComparer.Ordinal)
                .First();
            var duplicateIds = records.Where(row => row["_id"] != canonical["_id"]).Select(row => row["_id"]).ToList();
            if (duplicateIds.Count == 0) continue;

            await balances.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id", duplicateIds));
            removed += duplicateIds.Count;
            await reviews.ReplaceOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", group.Key),
                new BsonDocument
                {
                    { "_id", group.Key },
                    { "CompanyId", canonical["CompanyId"] },
                    { "UserId", canonical["UserId"] },
                    { "LeavePolicyId", canonical["LeavePolicyId"] },
                    { "Status", "DEDUPLICATED_EXACT" },
                    { "CanonicalBalanceId", canonical["_id"] },
                    { "RemovedBalanceIds", new BsonArray(duplicateIds) },
                    { "ResolvedAtUtc", DateTime.UtcNow }
                },
                new ReplaceOptions { IsUpsert = true });
        }

        if (unresolved > 0)
            throw new InvalidOperationException($"LEAVE_BALANCE_DUPLICATE_DATA: {unresolved} conflicting employee leave-balance group(s) were recorded in LeaveBalanceDuplicateReview and require manual resolution before the unique index can be created.");

        var indexName = "ux_leave_balance_company_user_policy";
        using var cursor = await balances.Indexes.ListAsync();
        var existingIndexes = await cursor.ToListAsync();
        if (!existingIndexes.Any(index => index.GetValue("name", string.Empty).AsString == indexName))
        {
            var keys = Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("UserId").Ascending("LeavePolicyId");
            await balances.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = indexName, Unique = true }));
        }

        await migrations.InsertOneAsync(new BsonDocument
        {
            { "_id", Id },
            { "ExecutedAt", DateTime.UtcNow },
            { "ExactDuplicateRowsRemoved", removed },
            { "DuplicateGroups", groupedRows.Count }
        });
        Console.WriteLine($"{{\"migration\":\"{Id}\",\"exactDuplicateRowsRemoved\":{removed},\"duplicateGroups\":{groupedRows.Count}}}");
    }

    private static bool HasIdentity(BsonDocument row) =>
        row.TryGetValue("CompanyId", out var company) && company.IsString && !string.IsNullOrWhiteSpace(company.AsString) &&
        row.TryGetValue("UserId", out var user) && user.IsString && !string.IsNullOrWhiteSpace(user.AsString) &&
        row.TryGetValue("LeavePolicyId", out var policy) && policy.IsString && !string.IsNullOrWhiteSpace(policy.AsString);

    private static string BalanceSignature(BsonDocument row) => string.Join("|",
        DecimalValue(row, "TotalAllocated"), DecimalValue(row, "Taken"), DecimalValue(row, "Remaining"),
        row.GetValue("IsManualAllocation", false).ToString(), row.GetValue("LastAccrual", BsonNull.Value).ToString(),
        LongValue(row, "Version"));

    private static decimal DecimalValue(BsonDocument row, string field) =>
        row.TryGetValue(field, out var value) && !value.IsBsonNull
            ? Convert.ToDecimal(value.ToString(), System.Globalization.CultureInfo.InvariantCulture)
            : 0m;

    private static long LongValue(BsonDocument row, string field) =>
        row.TryGetValue(field, out var value) && !value.IsBsonNull ? Convert.ToInt64(value.ToString(), System.Globalization.CultureInfo.InvariantCulture) : 0L;

    private static DateTime DateValue(BsonDocument row, string field) =>
        row.TryGetValue(field, out var value) && value.IsValidDateTime ? value.ToUniversalTime() : DateTime.MinValue;
}
