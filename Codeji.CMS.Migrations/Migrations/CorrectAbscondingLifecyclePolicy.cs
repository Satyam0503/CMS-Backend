using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Corrects the initial absconding policy seed without changing employee state.</summary>
public sealed class CorrectAbscondingLifecyclePolicy : IMigration
{
    public string Id => $"2026-09-08-04-{nameof(CorrectAbscondingLifecyclePolicy)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var policies = db.GetCollection<EmployeeLifecyclePolicy>("EmployeeLifecyclePolicy");
        await policies.UpdateManyAsync(
            x => x.EventType == "AbscondingUnderReview" && !x.IsDeleted,
            Builders<EmployeeLifecyclePolicy>.Update
                .Set(x => x.DefaultEmploymentStatus, EmployeeLifecycleStatuses.Active)
                .Set(x => x.DefaultAvailabilityStatus, EmployeeLifecycleStatuses.AbscondingUnderReview)
                .Set(x => x.DefaultAccountStatus, EmployeeLifecycleStatuses.AccountLimited)
                .Set(x => x.DefaultPayrollStatus, EmployeeLifecycleStatuses.PayrollHold)
                .Set(x => x.AttendanceExpected, false)
                .Set(x => x.LeaveRequestsAllowed, false)
                .Set(x => x.WorkFromHomeRequestsAllowed, false));

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
