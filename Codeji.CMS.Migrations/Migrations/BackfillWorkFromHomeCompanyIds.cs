using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Repairs legacy WFH documents which predate tenant-scoped writes. A tenant is
/// assigned only from an unambiguous related document; ambiguous rows are left
/// untouched rather than risking a cross-company data disclosure.
/// </summary>
public sealed class BackfillWorkFromHomeCompanyIds : IMigration
{
    public string Id => $"2026-08-10-{nameof(BackfillWorkFromHomeCompanyIds)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var missingCompany = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("CompanyId", false),
            Builders<BsonDocument>.Filter.Eq("CompanyId", BsonNull.Value),
            Builders<BsonDocument>.Filter.Eq("CompanyId", ""));
        var policies = db.GetCollection<BsonDocument>("WorkFromHomePolicy");
        var leavePolicies = db.GetCollection<BsonDocument>("LeavePolicy");
        var requests = db.GetCollection<BsonDocument>("WorkFromHomeRequest");
        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var logs = db.GetCollection<BsonDocument>("WorkFromHomeRequestLog");
        var policyCount = 0;
        var requestCount = 0;
        var logCount = 0;

        foreach (var policy in await policies.Find(missingCompany).ToListAsync())
        {
            if (!policy.TryGetValue("LeavePolicyId", out var leavePolicyId) || !leavePolicyId.IsString || string.IsNullOrWhiteSpace(leavePolicyId.AsString)) continue;
            var owner = await leavePolicies.Find(Builders<BsonDocument>.Filter.Eq("_id", leavePolicyId.AsString) & Builders<BsonDocument>.Filter.Ne("CompanyId", "")).FirstOrDefaultAsync();
            if (owner is null || !owner.TryGetValue("CompanyId", out var companyId) || !companyId.IsString || string.IsNullOrWhiteSpace(companyId.AsString)) continue;
            var result = await policies.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", policy["_id"]) & missingCompany, Builders<BsonDocument>.Update.Set("CompanyId", companyId.AsString));
            policyCount += (int)result.ModifiedCount;
        }

        foreach (var request in await requests.Find(missingCompany).ToListAsync())
        {
            if (!request.TryGetValue("UserId", out var userId) || !userId.IsString || string.IsNullOrWhiteSpace(userId.AsString)) continue;
            var owners = (await employees.Find(Builders<BsonDocument>.Filter.Eq("UserId", userId.AsString) & Builders<BsonDocument>.Filter.Ne("CompanyId", "")).Project(Builders<BsonDocument>.Projection.Include("CompanyId")).ToListAsync())
                .Where(x => x.TryGetValue("CompanyId", out var companyId) && companyId.IsString && !string.IsNullOrWhiteSpace(companyId.AsString)).Select(x => x["CompanyId"].AsString).Distinct(StringComparer.Ordinal).ToList();
            if (owners.Count != 1) continue;
            var result = await requests.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", request["_id"]) & missingCompany, Builders<BsonDocument>.Update.Set("CompanyId", owners[0]));
            requestCount += (int)result.ModifiedCount;
        }

        foreach (var log in await logs.Find(missingCompany).ToListAsync())
        {
            if (!log.TryGetValue("RequestId", out var requestId) || !requestId.IsString || string.IsNullOrWhiteSpace(requestId.AsString)) continue;
            var owners = (await requests.Find(Builders<BsonDocument>.Filter.Eq("_id", requestId.AsString) & Builders<BsonDocument>.Filter.Ne("CompanyId", "")).Project(Builders<BsonDocument>.Projection.Include("CompanyId")).ToListAsync())
                .Where(x => x.TryGetValue("CompanyId", out var companyId) && companyId.IsString && !string.IsNullOrWhiteSpace(companyId.AsString)).Select(x => x["CompanyId"].AsString).Distinct(StringComparer.Ordinal).ToList();
            if (owners.Count != 1) continue;
            var result = await logs.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", log["_id"]) & missingCompany, Builders<BsonDocument>.Update.Set("CompanyId", owners[0]));
            logCount += (int)result.ModifiedCount;
        }

        Console.WriteLine($"{{\"migration\":\"{Id}\",\"policiesBackfilled\":{policyCount},\"requestsBackfilled\":{requestCount},\"logsBackfilled\":{logCount},\"remarksSkipped\":true}}");
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
