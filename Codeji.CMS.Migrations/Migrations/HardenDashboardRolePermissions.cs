using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Creates the company-dashboard permission and grants it only to default HR/Admin roles.</summary>
public sealed class HardenDashboardRolePermissions : IMigration
{
    public string Id => $"2026-08-01-{nameof(HardenDashboardRolePermissions)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var modules = db.GetCollection<Module>(nameof(Module));
        var permissions = db.GetCollection<PermissionEntity>(nameof(PermissionEntity));
        var modulePermissions = db.GetCollection<ModulePermission>(nameof(ModulePermission));
        var roles = db.GetCollection<Roles>(nameof(Roles));
        var rolePermissions = db.GetCollection<RolePermission>(nameof(RolePermission));

        var viewAll = await permissions.Find(x => x.PermissionConstant == PermissionConst.ViewAll).FirstOrDefaultAsync();
        if (viewAll is null)
        {
            var nextPermissionId = ((await permissions.Find(FilterDefinition<PermissionEntity>.Empty).SortByDescending(x => x.PermissionId).FirstOrDefaultAsync())?.PermissionId ?? 0) + 1;
            viewAll = new PermissionEntity { PermissionId = nextPermissionId, PermissionName = PermissionConst.ViewAll, PermissionConstant = PermissionConst.ViewAll };
            await permissions.InsertOneAsync(viewAll);
        }

        var dashboard = await modules.Find(x => x.ModuleConstant == AppModule.Dashboard).FirstOrDefaultAsync();
        if (dashboard is null)
        {
            var nextModuleId = ((await modules.Find(FilterDefinition<Module>.Empty).SortByDescending(x => x.ModuleId).FirstOrDefaultAsync())?.ModuleId ?? 0) + 1;
            dashboard = new Module { ModuleId = nextModuleId, ModuleName = "Dashboard", ModuleConstant = AppModule.Dashboard, SortOrder = nextModuleId };
            await modules.InsertOneAsync(dashboard);
        }

        var modulePermission = await modulePermissions.Find(x => x.ModuleId == dashboard.ModuleId && x.PermissionId == viewAll.PermissionId).FirstOrDefaultAsync();
        if (modulePermission is null)
        {
            var nextModulePermissionId = ((await modulePermissions.Find(FilterDefinition<ModulePermission>.Empty).SortByDescending(x => x.ModulePermissionId).FirstOrDefaultAsync())?.ModulePermissionId ?? 0) + 1;
            modulePermission = new ModulePermission { ModulePermissionId = nextModulePermissionId, ModuleId = dashboard.ModuleId, PermissionId = viewAll.PermissionId, HasModuleAccess = true };
            await modulePermissions.InsertOneAsync(modulePermission);
        }

        foreach (var role in await roles.Find(x => !x.IsDeleted).ToListAsync())
        {
            var allowed = role.RoleType is (int)EnumsHelper.Roles.Administrator or (int)EnumsHelper.Roles.HR;
            var existing = await rolePermissions.Find(x => x.CompanyId == role.CompanyId && x.RoleId == role.RolesId && x.ModulePermissionId == modulePermission.ModulePermissionId).FirstOrDefaultAsync();
            if (existing is null)
                await rolePermissions.InsertOneAsync(new RolePermission { RolePermissionId = Guid.NewGuid().ToString(), CompanyId = role.CompanyId, RoleId = role.RolesId, ModulePermissionId = modulePermission.ModulePermissionId, HasAccess = allowed, IsAccessible = allowed, CreatedDate = DateTime.UtcNow });
            else
                await rolePermissions.UpdateOneAsync(x => x.RolePermissionId == existing.RolePermissionId, Builders<RolePermission>.Update.Set(x => x.HasAccess, allowed).Set(x => x.IsAccessible, allowed));
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
