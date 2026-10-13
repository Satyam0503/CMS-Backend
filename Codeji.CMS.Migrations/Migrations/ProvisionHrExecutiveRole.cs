using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Provisions HR Executive as a distinct assignable role and keeps its grants
/// identical to the HR Manager role in each tenant.
/// </summary>
public sealed class ProvisionHrExecutiveRole : IMigration
{
    public string Id => $"2026-08-03-{nameof(ProvisionHrExecutiveRole)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var roles = db.GetCollection<Roles>(nameof(Roles));
        var rolePermissions = db.GetCollection<RolePermission>(nameof(RolePermission));
        var managers = await roles.Find(x => !x.IsDeleted && x.RoleType == (int)EnumsHelper.Roles.HR).ToListAsync();

        foreach (var manager in managers)
        {
            var executive = await roles.Find(x => !x.IsDeleted
                && x.RoleType == (int)EnumsHelper.Roles.HRExecutive
                && x.CompanyId == manager.CompanyId).FirstOrDefaultAsync();

            if (executive is null)
            {
                executive = new Roles
                {
                    RolesId = Guid.NewGuid().ToString(),
                    RoleType = (int)EnumsHelper.Roles.HRExecutive,
                    CompanyId = manager.CompanyId,
                    Titles = "HR Executive",
                    Description = "HR Executives have the same application permissions as HR Managers.",
                    HasAppAccess = manager.HasAppAccess,
                    IsNotEditable = false,
                    IsDefault = manager.IsDefault,
                    IsDeleted = false,
                    UserRoles = [],
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    CreatedBy = "migration",
                    UpdatedBy = "migration"
                };
                await roles.InsertOneAsync(executive);
            }

            var managerPermissions = await rolePermissions.Find(x => x.RoleId == manager.RolesId).ToListAsync();
            var executivePermissions = await rolePermissions.Find(x => x.RoleId == executive.RolesId).ToListAsync();
            var managerPermissionIds = managerPermissions.Select(x => x.ModulePermissionId).ToHashSet();

            var staleIds = executivePermissions.Where(x => !managerPermissionIds.Contains(x.ModulePermissionId)).Select(x => x.RolePermissionId).ToList();
            if (staleIds.Count > 0)
                await rolePermissions.DeleteManyAsync(x => staleIds.Contains(x.RolePermissionId));

            foreach (var managerPermission in managerPermissions)
            {
                var existing = executivePermissions.FirstOrDefault(x => x.ModulePermissionId == managerPermission.ModulePermissionId);
                if (existing is null)
                {
                    await rolePermissions.InsertOneAsync(new RolePermission
                    {
                        CompanyId = executive.CompanyId,
                        RoleId = executive.RolesId,
                        ModulePermissionId = managerPermission.ModulePermissionId,
                        HasAccess = managerPermission.HasAccess,
                        IsAccessible = managerPermission.IsAccessible,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    });
                }
                else if (existing.HasAccess != managerPermission.HasAccess || existing.IsAccessible != managerPermission.IsAccessible)
                {
                    await rolePermissions.UpdateOneAsync(x => x.RolePermissionId == existing.RolePermissionId,
                        Builders<RolePermission>.Update
                            .Set(x => x.HasAccess, managerPermission.HasAccess)
                            .Set(x => x.IsAccessible, managerPermission.IsAccessible)
                            .Set(x => x.UpdatedDate, DateTime.UtcNow));
                }
            }
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
