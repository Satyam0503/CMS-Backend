using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Least-privilege attendance roles: employee self-view only; HR/admin explicit management grants.</summary>
public sealed class HardenAttendanceRolePermissions : IMigration
{
    public string Id => $"2026-08-01-{nameof(HardenAttendanceRolePermissions)}";
    private static readonly string[] Required = [PermissionConst.ViewOwn, PermissionConst.ViewAll, PermissionConst.CreateForEmployee, PermissionConst.Override];

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var permissions = db.GetCollection<PermissionEntity>(nameof(PermissionEntity));
        var module = await db.GetCollection<Module>(nameof(Module)).Find(x => x.ModuleConstant == AppModule.Attendance).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Attendance module is missing.");
        var modulePermissions = db.GetCollection<ModulePermission>(nameof(ModulePermission));
        var allPermissions = await permissions.Find(x => Required.Contains(x.PermissionConstant)).ToListAsync();
        var missing = Required.Where(code => allPermissions.All(x => x.PermissionConstant != code)).ToList();
        if (missing.Count > 0)
        {
            var next = ((await permissions.Find(FilterDefinition<PermissionEntity>.Empty).SortByDescending(x => x.PermissionId).FirstOrDefaultAsync())?.PermissionId ?? 0) + 1;
            var additions = missing.Select(code => new PermissionEntity { PermissionId = next++, PermissionName = code, PermissionConstant = code }).ToList();
            await permissions.InsertManyAsync(additions);
            allPermissions.AddRange(additions);
        }
        var links = new Dictionary<string, ModulePermission>();
        foreach (var permission in allPermissions)
        {
            var link = await modulePermissions.Find(x => x.ModuleId == module.ModuleId && x.PermissionId == permission.PermissionId).FirstOrDefaultAsync();
            if (link is null)
            {
                var id = ((await modulePermissions.Find(FilterDefinition<ModulePermission>.Empty).SortByDescending(x => x.ModulePermissionId).FirstOrDefaultAsync())?.ModulePermissionId ?? 0) + 1;
                link = new ModulePermission { ModulePermissionId = id, ModuleId = module.ModuleId, PermissionId = permission.PermissionId, HasModuleAccess = true };
                await modulePermissions.InsertOneAsync(link);
            }
            links[permission.PermissionConstant] = link;
        }
        var roles = await db.GetCollection<Roles>(nameof(Roles)).Find(x => !x.IsDeleted).ToListAsync();
        var rolePermissions = db.GetCollection<RolePermission>(nameof(RolePermission));
        foreach (var role in roles)
        {
            var grants = role.RoleType == (int)EnumsHelper.Roles.Employee ? [PermissionConst.ViewOwn]
                : role.RoleType is (int)EnumsHelper.Roles.Administrator or (int)EnumsHelper.Roles.HR ? Required : [];
            foreach (var grant in grants)
            {
                var link = links[grant];
                var existing = await rolePermissions.Find(x => x.CompanyId == role.CompanyId && x.RoleId == role.RolesId && x.ModulePermissionId == link.ModulePermissionId).FirstOrDefaultAsync();
                if (existing is null) await rolePermissions.InsertOneAsync(new RolePermission { CompanyId = role.CompanyId, RoleId = role.RolesId, ModulePermissionId = link.ModulePermissionId, HasAccess = true, IsAccessible = true, CreatedDate = DateTime.UtcNow });
                else await rolePermissions.UpdateOneAsync(x => x.RolePermissionId == existing.RolePermissionId, Builders<RolePermission>.Update.Set(x => x.HasAccess, true).Set(x => x.IsAccessible, true));
            }
            if (role.RoleType == (int)EnumsHelper.Roles.Employee)
            {
                var blockedIds = new[] { links[PermissionConst.ViewAll].ModulePermissionId, links[PermissionConst.CreateForEmployee].ModulePermissionId, links[PermissionConst.Override].ModulePermissionId };
                await rolePermissions.UpdateManyAsync(x => x.CompanyId == role.CompanyId && x.RoleId == role.RolesId && blockedIds.Contains(x.ModulePermissionId), Builders<RolePermission>.Update.Set(x => x.HasAccess, false));
            }
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
