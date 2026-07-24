using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Repairs duplicate built-in roles produced when more than one global template
/// existed for a role type. Custom roles (RoleType <= 0) are not modified.
/// </summary>
public sealed class DeduplicateDefaultCompanyRoles : IMigration
{
    public string Id => $"2026-07-23-01-{typeof(DeduplicateDefaultCompanyRoles).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var roles = db.GetCollection<Roles>(typeof(Roles).Name);
        var permissions = db.GetCollection<RolePermission>(typeof(RolePermission).Name);
        var employees = db.GetCollection<EmpUser>(typeof(EmpUser).Name);
        var builtInRoles = await roles.Find(x => x.RoleType > 0 && !x.IsDeleted).ToListAsync();

        var duplicateGroups = builtInRoles
            .GroupBy(x => new { CompanyId = x.CompanyId ?? string.Empty, x.RoleType })
            .Where(group => group.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups)
        {
            var candidates = group.OrderBy(x => x.CreatedDate).ToList();
            var referencedRoleIds = (await employees
                .Find(x => candidates.Select(role => role.RolesId).Contains(x.RoleId))
                .Project(x => x.RoleId)
                .ToListAsync())
                .ToHashSet();
            var keeper = candidates.FirstOrDefault(x => referencedRoleIds.Contains(x.RolesId))
                ?? candidates[0];

            foreach (var duplicate in candidates.Where(x => x.RolesId != keeper.RolesId))
            {
                var duplicatePermissions = await permissions
                    .Find(x => x.RoleId == duplicate.RolesId)
                    .ToListAsync();
                var keeperPermissions = await permissions
                    .Find(x => x.RoleId == keeper.RolesId)
                    .ToListAsync();
                var keeperByModulePermission = keeperPermissions
                    .GroupBy(x => x.ModulePermissionId)
                    .ToDictionary(x => x.Key, x => x.First());

                foreach (var permission in duplicatePermissions)
                {
                    if (!keeperByModulePermission.TryGetValue(permission.ModulePermissionId, out var existing))
                    {
                        permission.RolePermissionId = Guid.NewGuid().ToString();
                        permission.RoleId = keeper.RolesId;
                        permission.CompanyId = keeper.CompanyId;
                        await permissions.InsertOneAsync(permission);
                    }
                    else if ((!existing.HasAccess && permission.HasAccess) ||
                             (!existing.IsAccessible && permission.IsAccessible))
                    {
                        await permissions.UpdateOneAsync(
                            x => x.RolePermissionId == existing.RolePermissionId,
                            Builders<RolePermission>.Update
                                .Set(x => x.HasAccess, existing.HasAccess || permission.HasAccess)
                                .Set(x => x.IsAccessible, existing.IsAccessible || permission.IsAccessible));
                    }
                }

                await employees.UpdateManyAsync(
                    x => x.RoleId == duplicate.RolesId,
                    Builders<EmpUser>.Update.Set(x => x.RoleId, keeper.RolesId));
                await permissions.DeleteManyAsync(x => x.RoleId == duplicate.RolesId);
                await roles.DeleteOneAsync(x => x.RolesId == duplicate.RolesId);
            }
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
