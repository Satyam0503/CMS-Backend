using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Aligns active employees in the company containing the E2E payroll-test users
/// to a June 1, 2026 joining date when their current date is missing or later.
/// Earlier joining dates are preserved.
/// </summary>
public class SetCurrentTestCompanyJoiningDatesToJune2026 : IMigration
{
    public string Id => $"2026-07-21-05-{typeof(SetCurrentTestCompanyJoiningDatesToJune2026).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var employees = db.GetCollection<EmpUser>("EmpUser");
        var testEmployeeIds = Enumerable.Range(1, 5).Select(x => $"E2E2026-{x:000}").ToArray();
        var testUsers = await employees.Find(
            Builders<EmpUser>.Filter.In(x => x.EmployeeId, testEmployeeIds)).ToListAsync();
        var companyIds = testUsers.Select(x => x.CompanyId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (companyIds.Count != 1)
        {
            Console.WriteLine(
                $"Skipping {Id}: expected one E2E test company but found {companyIds.Count}.");
            return;
        }

        var companyId = companyIds[0];
        var activeEmployees = await employees.Find(x =>
            x.CompanyId == companyId && x.Status && !x.IsDeleted).ToListAsync();
        var effectiveJoiningDate = new DateTime(2026, 6, 1);

        foreach (var employee in activeEmployees)
        {
            if (DateTime.TryParse(employee.DateOfJoining, out var existingDate) &&
                existingDate.Date <= effectiveJoiningDate)
                continue;

            await employees.UpdateOneAsync(
                Builders<EmpUser>.Filter.Eq(x => x.UserId, employee.UserId),
                Builders<EmpUser>.Update
                    .Set(x => x.DateOfJoining, "2026-06-01")
                    .Set(x => x.UpdatedDate, DateTime.UtcNow));
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
