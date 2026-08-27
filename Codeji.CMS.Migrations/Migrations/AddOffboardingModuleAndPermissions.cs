using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Creates the configurable offboarding permission surface for existing companies.</summary>
public sealed class AddOffboardingModuleAndPermissions : IMigration
{
    public string Id => $"2026-08-27-{nameof(AddOffboardingModuleAndPermissions)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var modules = db.GetCollection<Module>(nameof(Module));
        var permissions = db.GetCollection<PermissionEntity>(typeof(PermissionEntity).Name);
        var modulePermissions = db.GetCollection<ModulePermission>(nameof(ModulePermission));
        var roles = db.GetCollection<Roles>(nameof(Roles));
        var rolePermissions = db.GetCollection<RolePermission>(nameof(RolePermission));

        var permissionConstants = new[] { PermissionConst.View, PermissionConst.Create, PermissionConst.Edit, PermissionConst.Delete, PermissionConst.Submit, PermissionConst.Review, PermissionConst.Approve, PermissionConst.Complete, PermissionConst.Evidence, PermissionConst.Download, PermissionConst.Override };
        var existingPermissions = await permissions.Find(FilterDefinition<PermissionEntity>.Empty).ToListAsync();
        var nextPermissionId = existingPermissions.Count == 0 ? 0 : existingPermissions.Max(x => x.PermissionId);
        foreach (var constant in permissionConstants.Where(value => existingPermissions.All(x => x.PermissionConstant != value)))
        {
            var row = new PermissionEntity { PermissionId = ++nextPermissionId, PermissionName = constant, PermissionConstant = constant };
            await permissions.InsertOneAsync(row); existingPermissions.Add(row);
        }

        var module = await modules.Find(x => x.ModuleConstant == AppModule.Offboarding).FirstOrDefaultAsync();
        if (module is null)
        {
            var moduleId = (await modules.Find(FilterDefinition<Module>.Empty).ToListAsync()).DefaultIfEmpty().Max(x => x?.ModuleId ?? 0) + 1;
            module = new Module { ModuleId = moduleId, ModuleName = "Offboarding", ModuleConstant = AppModule.Offboarding, SortOrder = moduleId };
            await modules.InsertOneAsync(module);
        }

        var existingLinks = await modulePermissions.Find(x => x.ModuleId == module.ModuleId).ToListAsync();
        var nextLinkId = (await modulePermissions.Find(FilterDefinition<ModulePermission>.Empty).ToListAsync()).DefaultIfEmpty().Max(x => x?.ModulePermissionId ?? 0);
        foreach (var permission in existingPermissions.Where(x => permissionConstants.Contains(x.PermissionConstant) && existingLinks.All(link => link.PermissionId != x.PermissionId)))
        {
            var link = new ModulePermission { ModulePermissionId = ++nextLinkId, ModuleId = module.ModuleId, PermissionId = permission.PermissionId, HasModuleAccess = true };
            await modulePermissions.InsertOneAsync(link); existingLinks.Add(link);
        }

        var privilegedRoles = await roles.Find(x => x.RoleType == (int)EnumsHelper.Roles.Administrator || x.RoleType == (int)EnumsHelper.Roles.HR || x.RoleType == (int)EnumsHelper.Roles.HRExecutive).ToListAsync();
        foreach (var role in privilegedRoles)
            foreach (var link in existingLinks)
                if (!await rolePermissions.Find(x => x.CompanyId == role.CompanyId && x.RoleId == role.RolesId && x.ModulePermissionId == link.ModulePermissionId).AnyAsync())
                    await rolePermissions.InsertOneAsync(new RolePermission { CompanyId = role.CompanyId, RoleId = role.RolesId, ModulePermissionId = link.ModulePermissionId, HasAccess = true, IsAccessible = true, CreatedDate = DateTime.UtcNow });
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
