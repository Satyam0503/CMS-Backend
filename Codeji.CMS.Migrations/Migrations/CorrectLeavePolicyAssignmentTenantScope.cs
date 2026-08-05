using Codeji.CMS.Migrations.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Repairs a prior all-company eligibility backfill to respect CompanyId.</summary>
public sealed class CorrectLeavePolicyAssignmentTenantScope : IMigration
{
    public string Id => $"2026-08-04-{nameof(CorrectLeavePolicyAssignmentTenantScope)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<BsonDocument>("Migration");
        if (await migrations.Find(Builders<BsonDocument>.Filter.Eq("_id", Id)).AnyAsync()) return;

        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var policies = db.GetCollection<BsonDocument>("LeavePolicy");
        var balances = db.GetCollection<BsonDocument>("EmployeeLeaveBalance");
        var allEmployees = await employees.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var employeeCompanyByUserId = allEmployees
            .Where(employee => employee.TryGetValue("_id", out var id) && id.IsString && employee.TryGetValue("CompanyId", out var companyId) && companyId.IsString)
            .ToDictionary(employee => employee["_id"].AsString, employee => employee["CompanyId"].AsString, StringComparer.Ordinal);
        var activeEmployeeIdsByCompany = allEmployees
            .Where(employee => employee.GetValue("Status", false).ToBoolean() && employee.TryGetValue("_id", out var id) && id.IsString && employee.TryGetValue("CompanyId", out var companyId) && companyId.IsString)
            .GroupBy(employee => employee["CompanyId"].AsString, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(employee => employee["_id"].AsString).Distinct(StringComparer.Ordinal).ToList(), StringComparer.Ordinal);

        var policyRows = await policies.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var correctedBalances = 0;
        var createdBalances = 0;
        foreach (var policy in policyRows.Where(IsActiveLeavePolicy))
        {
            var companyId = policy.GetValue("CompanyId", BsonNull.Value);
            if (!companyId.IsString || !policy.TryGetValue("_id", out var policyId) || !policyId.IsString) continue;
            var activeUserIds = activeEmployeeIdsByCompany.GetValueOrDefault(companyId.AsString, []);
            var policyFilter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("CompanyId", companyId),
                Builders<BsonDocument>.Filter.Eq("LeavePolicyId", policyId));
            var policyBalances = await balances.Find(policyFilter).ToListAsync();

            foreach (var balance in policyBalances)
            {
                var userId = balance.GetValue("UserId", BsonNull.Value);
                if (!userId.IsString || !employeeCompanyByUserId.TryGetValue(userId.AsString, out var employeeCompany) || employeeCompany == companyId.AsString) continue;
                var taken = DecimalValue(balance, "Taken");
                if (taken == 0m)
                {
                    await balances.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", balance["_id"]));
                    correctedBalances++;
                }
            }

            var existingUserIds = policyBalances
                .Where(balance => balance.GetValue("UserId", BsonNull.Value).IsString)
                .Select(balance => balance["UserId"].AsString)
                .Where(userId => employeeCompanyByUserId.GetValueOrDefault(userId) == companyId.AsString)
                .ToHashSet(StringComparer.Ordinal);
            var accrual = DecimalValue(policy, "AccrualAmount");
            foreach (var userId in activeUserIds.Except(existingUserIds))
            {
                var now = DateTime.UtcNow;
                await balances.InsertOneAsync(new BsonDocument
                {
                    { "CompanyId", companyId }, { "UserId", userId }, { "LeavePolicyId", policyId },
                    { "TotalAllocated", accrual }, { "Taken", 0m }, { "Remaining", accrual },
                    { "LastAccrual", now }, { "CreatedDate", now }
                });
                createdBalances++;
            }
        }

        Console.WriteLine($"{{\"migration\":\"{Id}\",\"removedCrossTenantZeroUseBalances\":{correctedBalances},\"createdMissingTenantBalances\":{createdBalances}}}");
        await migrations.InsertOneAsync(new BsonDocument { { "_id", Id }, { "ExecutedAt", DateTime.UtcNow } });
    }

    private static bool IsActiveLeavePolicy(BsonDocument policy) =>
        (!policy.TryGetValue("Status", out var status) || !status.IsBoolean || status.AsBoolean) &&
        (!policy.TryGetValue("PolicyType", out var type) || type.IsBsonNull ||
         !(type.IsInt32 && type.AsInt32 == 2) && !(type.IsInt64 && type.AsInt64 == 2) && !(type.IsString && string.Equals(type.AsString, "WorkFromHome", StringComparison.OrdinalIgnoreCase)));

    private static decimal DecimalValue(BsonDocument row, string field) =>
        row.TryGetValue(field, out var value) && !value.IsBsonNull
            ? Convert.ToDecimal(value.ToString(), System.Globalization.CultureInfo.InvariantCulture)
            : 0m;
}
