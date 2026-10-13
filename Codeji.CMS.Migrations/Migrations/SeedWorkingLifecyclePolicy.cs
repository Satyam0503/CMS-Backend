using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class SeedWorkingLifecyclePolicy : IMigration
{
    public string Id => $"2026-09-08-03-{nameof(SeedWorkingLifecyclePolicy)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migration = db.GetCollection<Migration>("Migration");
        if (await migration.Find(x => x.Id == Id).AnyAsync()) return;
        var companies = db.GetCollection<BsonDocument>("Company");
        var policies = db.GetCollection<EmployeeLifecyclePolicy>("EmployeeLifecyclePolicy");
        var rows = await companies.Find(Builders<BsonDocument>.Filter.Ne("IsDeleted", true)).ToListAsync();
        foreach (var company in rows)
        {
            var companyIdValue = company.TryGetValue("CompanyId", out var legacyCompanyId) ? legacyCompanyId : company.GetValue("_id", BsonNull.Value);
            if (!companyIdValue.IsString || string.IsNullOrWhiteSpace(companyIdValue.AsString)) continue;
            var companyId = companyIdValue.AsString;
            if (await policies.Find(x => x.CompanyId == companyId && x.EventType == EmployeeLifecycleStatuses.Working && !x.IsDeleted).AnyAsync()) continue;
            await policies.InsertOneAsync(new EmployeeLifecyclePolicy
            {
                CompanyId = companyId,
                CreatedBy = "migration",
                EventType = EmployeeLifecycleStatuses.Working,
                RequiresReason = false,
                DefaultEmploymentStatus = EmployeeLifecycleStatuses.Active,
                DefaultAvailabilityStatus = EmployeeLifecycleStatuses.Working,
                DefaultAccountStatus = EmployeeLifecycleStatuses.AccountActive,
                DefaultPayrollStatus = EmployeeLifecycleStatuses.PayrollActive,
                AttendanceExpected = true,
                LeaveRequestsAllowed = true,
                WorkFromHomeRequestsAllowed = true,
            });
        }
        await migration.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}

