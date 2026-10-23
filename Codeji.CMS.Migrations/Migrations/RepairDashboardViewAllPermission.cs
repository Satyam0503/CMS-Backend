using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Corrects the Dashboard module permission from the erroneous "Submit" permission
/// to the intended "View all" (ViewAll) permission, ensuring company dashboard
/// analytics permissions can be configured accurately.
/// </summary>
public sealed class RepairDashboardViewAllPermission : IMigration
{
    public string Id => $"2026-10-21-{nameof(RepairDashboardViewAllPermission)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var modules = db.GetCollection<Module>("Module");
        var permissions = db.GetCollection<PermissionEntity>("Permission");
        var modulePermissions = db.GetCollection<ModulePermission>("ModulePermission");
        var roles = db.GetCollection<Roles>("Roles");
        var rolePermissions = db.GetCollection<RolePermission>("RolePermission");

        // 1. Locate or create ViewAll permission
        var viewAll = await permissions.Find(x => x.PermissionConstant == PermissionConst.ViewAll).FirstOrDefaultAsync();
        if (viewAll is null)
        {
            var nextPermissionId = ((await permissions.Find(FilterDefinition<PermissionEntity>.Empty).SortByDescending(x => x.PermissionId).FirstOrDefaultAsync())?.PermissionId ?? 0) + 1;
            viewAll = new PermissionEntity { PermissionId = nextPermissionId, PermissionName = "View all", PermissionConstant = PermissionConst.ViewAll };
            await permissions.InsertOneAsync(viewAll);
        }

        // 2. Locate or create Dashboard module
        var dashboard = await modules.Find(x => x.ModuleConstant == AppModule.Dashboard).FirstOrDefaultAsync();
        if (dashboard is null)
        {
            var nextModuleId = ((await modules.Find(FilterDefinition<Module>.Empty).SortByDescending(x => x.ModuleId).FirstOrDefaultAsync())?.ModuleId ?? 0) + 1;
            dashboard = new Module { ModuleId = nextModuleId, ModuleName = "Dashboard", ModuleConstant = AppModule.Dashboard, SortOrder = nextModuleId };
            await modules.InsertOneAsync(dashboard);
        }

        // 3. Ensure ModulePermission links Dashboard to ViewAll
        var dashboardModulePermissions = await modulePermissions.Find(x => x.ModuleId == dashboard.ModuleId).ToListAsync();
        ModulePermission correctLink;

        var existingCorrect = dashboardModulePermissions.FirstOrDefault(x => x.PermissionId == viewAll.PermissionId);
        if (existingCorrect != null)
        {
            correctLink = existingCorrect;
        }
        else if (dashboardModulePermissions.Count > 0)
        {
            var primary = dashboardModulePermissions[0];
            await modulePermissions.UpdateOneAsync(
                x => x.ModulePermissionId == primary.ModulePermissionId,
                Builders<ModulePermission>.Update.Set(x => x.PermissionId, viewAll.PermissionId).Set(x => x.HasModuleAccess, true)
            );
            primary.PermissionId = viewAll.PermissionId;
            correctLink = primary;
        }
        else
        {
            var nextModulePermissionId = ((await modulePermissions.Find(FilterDefinition<ModulePermission>.Empty).SortByDescending(x => x.ModulePermissionId).FirstOrDefaultAsync())?.ModulePermissionId ?? 0) + 1;
            correctLink = new ModulePermission { ModulePermissionId = nextModulePermissionId, ModuleId = dashboard.ModuleId, PermissionId = viewAll.PermissionId, HasModuleAccess = true };
            await modulePermissions.InsertOneAsync(correctLink);
        }

        // Clean up any extra redundant module permission links for Dashboard
        var redundant = dashboardModulePermissions.Where(x => x.ModulePermissionId != correctLink.ModulePermissionId).ToList();
        foreach (var r in redundant)
        {
            await modulePermissions.DeleteOneAsync(x => x.ModulePermissionId == r.ModulePermissionId);
            await rolePermissions.UpdateManyAsync(
                x => x.ModulePermissionId == r.ModulePermissionId,
                Builders<RolePermission>.Update.Set(x => x.ModulePermissionId, correctLink.ModulePermissionId)
            );
        }

        // 4. Ensure all Administrator and HR roles have access to this ModulePermission
        foreach (var role in await roles.Find(x => !x.IsDeleted).ToListAsync())
        {
            var shouldHaveAccess = role.RoleType is (int)EnumsHelper.Roles.Administrator or (int)EnumsHelper.Roles.HR or (int)EnumsHelper.Roles.HRExecutive;
            var existingRp = await rolePermissions.Find(x => x.CompanyId == role.CompanyId && x.RoleId == role.RolesId && x.ModulePermissionId == correctLink.ModulePermissionId).FirstOrDefaultAsync();
            if (existingRp is null)
            {
                await rolePermissions.InsertOneAsync(new RolePermission
                {
                    RolePermissionId = Guid.NewGuid().ToString(),
                    CompanyId = role.CompanyId,
                    RoleId = role.RolesId,
                    ModulePermissionId = correctLink.ModulePermissionId,
                    HasAccess = shouldHaveAccess,
                    IsAccessible = shouldHaveAccess,
                    CreatedDate = DateTime.UtcNow
                });
            }
            else if (shouldHaveAccess && (!existingRp.HasAccess || !existingRp.IsAccessible))
            {
                await rolePermissions.UpdateOneAsync(
                    x => x.RolePermissionId == existingRp.RolePermissionId,
                    Builders<RolePermission>.Update.Set(x => x.HasAccess, true).Set(x => x.IsAccessible, true)
                );
            }
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
