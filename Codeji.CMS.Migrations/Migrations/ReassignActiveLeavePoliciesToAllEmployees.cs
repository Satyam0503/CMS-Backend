using Codeji.CMS.Migrations.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Makes every active policy available to every active employee without changing
/// an existing employee's accrued or consumed leave. WFH eligibility is stored
/// separately and is also reset to the all-employees setting.
/// </summary>
public sealed class ReassignActiveLeavePoliciesToAllEmployees : IMigration
{
    public string Id => $"2026-08-04-{nameof(ReassignActiveLeavePoliciesToAllEmployees)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var dryRun = string.Equals(Environment.GetEnvironmentVariable("LEAVE_POLICY_REASSIGN_DRY_RUN"), "true", StringComparison.OrdinalIgnoreCase);
        var migrations = db.GetCollection<BsonDocument>("Migration");
        if (!dryRun && await migrations.Find(Builders<BsonDocument>.Filter.Eq("_id", Id)).AnyAsync()) return;

        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var policies = db.GetCollection<BsonDocument>("LeavePolicy");
        var balances = db.GetCollection<BsonDocument>("EmployeeLeaveBalance");
        var wfhPolicies = db.GetCollection<BsonDocument>("WorkFromHomePolicy");

        var activeEmployees = await employees.Find(new BsonDocument("Status", true)).ToListAsync();
        var employeeIdsByCompany = activeEmployees
            .Where(employee => employee.TryGetValue("_id", out var id) && id.IsString && !string.IsNullOrWhiteSpace(id.AsString) &&
                               employee.TryGetValue("CompanyId", out var companyId) && companyId.IsString && !string.IsNullOrWhiteSpace(companyId.AsString))
            .GroupBy(employee => employee["CompanyId"].AsString, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(employee => employee["_id"].AsString).Distinct(StringComparer.Ordinal).ToList(), StringComparer.Ordinal);

        var activePolicies = (await policies.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())
            .Where(IsActiveLeavePolicy)
            .ToList();
        var activeWfhPolicyIds = (await policies.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())
            .Where(IsActiveWorkFromHomePolicy)
            .Select(policy => policy["_id"].AsString)
            .ToList();

        var policyUpdates = 0;
        var balanceAssignments = 0;
        foreach (var policy in activePolicies)
        {
            var policyId = policy["_id"].AsString;
            var companyId = policy.GetValue("CompanyId", BsonNull.Value);
            if (!companyId.IsString || string.IsNullOrWhiteSpace(companyId.AsString)) continue;
            var employeeIds = employeeIdsByCompany.GetValueOrDefault(companyId.AsString, []);

            if (!dryRun)
            {
                await policies.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", policy["_id"]),
                    Builders<BsonDocument>.Update.Set("ApplicableTo", new BsonArray()));
            }
            policyUpdates++;

            var accrual = DecimalValue(policy, "AccrualAmount");
            foreach (var userId in employeeIds)
            {
                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("CompanyId", companyId),
                    Builders<BsonDocument>.Filter.Eq("UserId", userId),
                    Builders<BsonDocument>.Filter.Eq("LeavePolicyId", policyId));
                if (!dryRun)
                {
                    var now = DateTime.UtcNow;
                    await balances.UpdateOneAsync(filter, Builders<BsonDocument>.Update
                        .SetOnInsert("CompanyId", companyId)
                        .SetOnInsert("UserId", userId)
                        .SetOnInsert("LeavePolicyId", policyId)
                        .SetOnInsert("TotalAllocated", accrual)
                        .SetOnInsert("Taken", 0m)
                        .SetOnInsert("Remaining", accrual)
                        .SetOnInsert("LastAccrual", now)
                        .SetOnInsert("CreatedDate", now),
                        new UpdateOptions { IsUpsert = true });
                }
                balanceAssignments++;
            }
        }

        var wfhUpdates = 0;
        foreach (var leavePolicyId in activeWfhPolicyIds)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("LeavePolicyId", leavePolicyId);
            if (!dryRun)
            {
                var update = Builders<BsonDocument>.Update
                    .Set("ApplyToAllEmployees", true)
                    .Set("ApplicableEmployeeIds", new BsonArray())
                    .Set("UpdatedDate", DateTime.UtcNow);
                var result = await wfhPolicies.UpdateManyAsync(filter, update);
                wfhUpdates += (int)result.ModifiedCount;
            }
        }

        Console.WriteLine($"{{\"migration\":\"{Id}\",\"dryRun\":{dryRun.ToString().ToLowerInvariant()},\"activeEmployees\":{activeEmployees.Count},\"leavePoliciesAssigned\":{policyUpdates},\"leaveBalanceAssignmentsChecked\":{balanceAssignments},\"wfhPoliciesUpdated\":{wfhUpdates}}}");
        if (!dryRun)
            await migrations.InsertOneAsync(new BsonDocument { { "_id", Id }, { "ExecutedAt", DateTime.UtcNow } });
    }

    private static bool IsActiveLeavePolicy(BsonDocument policy) =>
        IsActive(policy) && !IsWorkFromHome(policy);

    private static bool IsActiveWorkFromHomePolicy(BsonDocument policy) =>
        IsActive(policy) && IsWorkFromHome(policy) && policy.TryGetValue("_id", out var id) && id.IsString;

    private static bool IsActive(BsonDocument policy) =>
        !policy.TryGetValue("Status", out var status) || !status.IsBoolean || status.AsBoolean;

    private static bool IsWorkFromHome(BsonDocument policy)
    {
        if (!policy.TryGetValue("PolicyType", out var type) || type.IsBsonNull) return false;
        return type.IsInt32 && type.AsInt32 == 2 ||
               type.IsInt64 && type.AsInt64 == 2 ||
               type.IsString && string.Equals(type.AsString, "WorkFromHome", StringComparison.OrdinalIgnoreCase);
    }

    private static decimal DecimalValue(BsonDocument row, string field) =>
        row.TryGetValue(field, out var value) && !value.IsBsonNull
            ? Convert.ToDecimal(value.ToString(), System.Globalization.CultureInfo.InvariantCulture)
            : 0m;
}
