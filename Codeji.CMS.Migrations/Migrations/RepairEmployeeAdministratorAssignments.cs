using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Repairs employee accounts that were accidentally assigned the company
/// administrator role. A company's PrimaryContact is its account owner and is
/// therefore the only user whose administrator assignment is preserved.
/// </summary>
public sealed class RepairEmployeeAdministratorAssignments : IMigration
{
    public string Id => $"2026-07-22-{typeof(RepairEmployeeAdministratorAssignments).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var companies = db.GetCollection<Company>(typeof(Company).Name);
        var roles = db.GetCollection<Roles>("Roles");
        var users = db.GetCollection<EmpUser>(typeof(EmpUser).Name);

        var activeCompanies = await companies
            .Find(c => !c.IsDeleted)
            .ToListAsync();

        foreach (var company in activeCompanies)
        {
            var companyRoles = await roles.Find(r =>
                    r.CompanyId == company.CompanyId &&
                    !r.IsDeleted &&
                    (r.RoleType == (int)EnumsHelper.Roles.Administrator ||
                     r.RoleType == (int)EnumsHelper.Roles.Employee))
                .ToListAsync();

            var administratorRoleIds = companyRoles
                .Where(r => r.RoleType == (int)EnumsHelper.Roles.Administrator)
                .Select(r => r.RolesId)
                .ToList();
            var employeeRole = companyRoles
                .FirstOrDefault(r => r.RoleType == (int)EnumsHelper.Roles.Employee);

            if (administratorRoleIds.Count == 0 || employeeRole == null)
            {
                continue;
            }

            // Company.PrimaryContact is created together with the company and
            // represents its owner. All other users are employee records and
            // must not inherit account-owner privileges.
            await users.UpdateManyAsync(
                u => u.CompanyId == company.CompanyId &&
                     !u.IsDeleted &&
                     u.UserId != company.PrimaryContact &&
                     administratorRoleIds.Contains(u.RoleId),
                Builders<EmpUser>.Update
                    .Set(u => u.RoleId, employeeRole.RolesId)
                    .Set(u => u.UpdatedDate, DateTime.UtcNow)
                    .Set(u => u.UpdatedBy, "system-role-repair"));
        }

        await migrations.InsertOneAsync(new Migration
        {
            Id = Id,
            ExecutedAt = DateTime.UtcNow,
        });
    }
}
