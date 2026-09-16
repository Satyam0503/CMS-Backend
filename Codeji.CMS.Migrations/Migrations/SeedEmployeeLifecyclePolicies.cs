using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Seeds explicit company lifecycle defaults without modifying employee state.</summary>
public sealed class SeedEmployeeLifecyclePolicies : IMigration
{
    public string Id => $"2026-09-08-01-{nameof(SeedEmployeeLifecyclePolicies)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migration = db.GetCollection<Migration>("Migration");
        if (await migration.Find(x => x.Id == Id).AnyAsync()) return;

        var companies = db.GetCollection<BsonDocument>("Company");
        var policies = db.GetCollection<EmployeeLifecyclePolicy>("EmployeeLifecyclePolicy");
        var companyRows = await companies.Find(Builders<BsonDocument>.Filter.Ne("IsDeleted", true)).ToListAsync();
        var defaults = new[]
        {
            new { EventType = "Sabbatical", Hr = false, ReturnDate = true, Exit = false, Employment = "Active", Availability = "Sabbatical", Account = "Limited", Payroll = "Paused", Attendance = false, Leave = false, Wfh = false },
            new { EventType = "UnauthorizedAbsence", Hr = false, ReturnDate = false, Exit = false, Employment = "Active", Availability = "UnauthorizedAbsence", Account = "Active", Payroll = "Hold", Attendance = false, Leave = false, Wfh = false },
            new { EventType = "AbscondingUnderReview", Hr = true, ReturnDate = false, Exit = false, Employment = "Active", Availability = "AbscondingUnderReview", Account = "Limited", Payroll = "Hold", Attendance = false, Leave = false, Wfh = false },
            new { EventType = "Suspension", Hr = true, ReturnDate = false, Exit = false, Employment = "Suspended", Availability = "Suspended", Account = "Suspended", Payroll = "Hold", Attendance = false, Leave = false, Wfh = false },
        };

        foreach (var company in companyRows)
        {
            var companyIdValue = company.TryGetValue("CompanyId", out var legacyCompanyId) ? legacyCompanyId : company.GetValue("_id", BsonNull.Value);
            if (!companyIdValue.IsString || string.IsNullOrWhiteSpace(companyIdValue.AsString)) continue;
            var companyId = companyIdValue.AsString;
            foreach (var item in defaults)
            {
                var exists = await policies.Find(x => x.CompanyId == companyId && x.EventType == item.EventType && !x.IsDeleted).AnyAsync();
                if (exists) continue;
                await policies.InsertOneAsync(new EmployeeLifecyclePolicy
                {
                    CompanyId = companyId,
                    CreatedBy = "migration",
                    EventType = item.EventType,
                    RequiresHrApproval = item.Hr,
                    RequiresExpectedReturnDate = item.ReturnDate,
                    CreatesEmployeeExit = item.Exit,
                    DefaultEmploymentStatus = item.Employment,
                    DefaultAvailabilityStatus = item.Availability,
                    DefaultAccountStatus = item.Account,
                    DefaultPayrollStatus = item.Payroll,
                    AttendanceExpected = item.Attendance,
                    LeaveRequestsAllowed = item.Leave,
                    WorkFromHomeRequestsAllowed = item.Wfh,
                });
            }
        }

        await migration.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}

