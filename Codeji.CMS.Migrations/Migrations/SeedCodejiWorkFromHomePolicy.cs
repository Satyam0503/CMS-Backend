using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Company;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Initial company-wide WFH policy requested for the Codeji company administrator.</summary>
public sealed class SeedCodejiWorkFromHomePolicy : IMigration
{
    public string Id => $"2026-07-30-{nameof(SeedCodejiWorkFromHomePolicy)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var employee = await db.GetCollection<EmpUser>(nameof(EmpUser))
            .Find(x => x.Email == "satyam@codeji.in" && !x.IsDeleted)
            .FirstOrDefaultAsync();
        var company = employee is null
            ? await db.GetCollection<Company>(nameof(Company)).Find(x => x.PrimaryContact == "satyam@codeji.in" && !x.IsDeleted).FirstOrDefaultAsync()
            : null;
        var companyId = employee?.CompanyId ?? company?.CompanyId;
        if (string.IsNullOrWhiteSpace(companyId))
        {
            // A customer-specific seed must never block unrelated schema migrations when
            // the target account is absent from another environment.
            Console.WriteLine("Skipped Codeji WFH policy seed: satyam@codeji.in was not found in this database.");
            await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
            return;
        }

        var policies = db.GetCollection<WorkFromHomePolicy>(nameof(WorkFromHomePolicy));
        var policy = await policies.Find(x => x.CompanyId == companyId && !x.IsDeleted).FirstOrDefaultAsync()
            ?? new WorkFromHomePolicy { CompanyId = companyId };

        policy.IsEnabled = true;
        policy.MaxDaysPerWeek = 1;
        policy.ApplyToAllEmployees = true;
        policy.ApplicableDepartments = [];
        policy.ApplicableEmployeeIds = [];
        policy.FullDayAttendanceStatusCode = "WFH";
        policy.HalfDayAttendanceStatusCode = "WFH-HD";
        policy.MixedAttendanceStatusCode = "WFH+WFO";
        policy.UpdatedDate = DateTime.UtcNow;
        policy.UpdatedBy = employee?.UserId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(policy.PolicyId)) await policies.InsertOneAsync(policy);
        else await policies.ReplaceOneAsync(x => x.PolicyId == policy.PolicyId && x.CompanyId == companyId, policy);

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
