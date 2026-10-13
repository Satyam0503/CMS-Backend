using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Adds explicit policy rows for leave availability states.</summary>
public sealed class SeedEmployeeLeaveLifecyclePolicies : IMigration
{
    public string Id => $"2026-09-08-02-{nameof(SeedEmployeeLeaveLifecyclePolicies)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migration = db.GetCollection<Migration>("Migration");
        if (await migration.Find(x => x.Id == Id).AnyAsync()) return;

        var companies = db.GetCollection<BsonDocument>("Company");
        var policies = db.GetCollection<EmployeeLifecyclePolicy>("EmployeeLifecyclePolicy");
        var companyRows = await companies.Find(Builders<BsonDocument>.Filter.Ne("IsDeleted", true)).ToListAsync();
        var leaveStatuses = new[]
        {
            EmployeeLifecycleStatuses.AnnualLeave,
            EmployeeLifecycleStatuses.SickLeave,
            EmployeeLifecycleStatuses.MedicalLeave,
            EmployeeLifecycleStatuses.SickMedicalLeave,
            EmployeeLifecycleStatuses.MaternityLeave,
            EmployeeLifecycleStatuses.PaternityLeave,
            EmployeeLifecycleStatuses.LongTermLeave,
            EmployeeLifecycleStatuses.AdministrativeLeave,
        };

        foreach (var company in companyRows)
        {
            var companyIdValue = company.TryGetValue("CompanyId", out var legacyCompanyId) ? legacyCompanyId : company.GetValue("_id", BsonNull.Value);
            if (!companyIdValue.IsString || string.IsNullOrWhiteSpace(companyIdValue.AsString)) continue;
            var companyId = companyIdValue.AsString;
            foreach (var eventType in leaveStatuses)
            {
                if (await policies.Find(x => x.CompanyId == companyId && x.EventType == eventType && !x.IsDeleted).AnyAsync()) continue;
                await policies.InsertOneAsync(new EmployeeLifecyclePolicy
                {
                    CompanyId = companyId,
                    CreatedBy = "migration",
                    EventType = eventType,
                    RequiresReason = false,
                    DefaultEmploymentStatus = EmployeeLifecycleStatuses.Active,
                    DefaultAvailabilityStatus = eventType,
                    DefaultAccountStatus = EmployeeLifecycleStatuses.AccountActive,
                    DefaultPayrollStatus = EmployeeLifecycleStatuses.PayrollActive,
                    AttendanceExpected = false,
                    LeaveRequestsAllowed = false,
                    WorkFromHomeRequestsAllowed = false,
                });
            }
        }

        await migration.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}

