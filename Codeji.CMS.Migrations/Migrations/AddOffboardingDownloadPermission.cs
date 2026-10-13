using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using MongoDB.Driver;
using PermissionConstant = Codeji.CMS.Utility.Constraints.Permission;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Seeds a permission only; companies must explicitly assign confidential-evidence download access.</summary>
public sealed class AddOffboardingDownloadPermission : IMigration
{
    public string Id => $"2026-08-27-{nameof(AddOffboardingDownloadPermission)}";
    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration"); if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var permissions = db.GetCollection<PermissionEntity>(typeof(PermissionEntity).Name); var modules = db.GetCollection<Module>(nameof(Module)); var links = db.GetCollection<ModulePermission>(nameof(ModulePermission));
        var permission = await permissions.Find(x => x.PermissionConstant == PermissionConstant.Download).FirstOrDefaultAsync();
        if (permission is null) { var all = await permissions.Find(FilterDefinition<PermissionEntity>.Empty).ToListAsync(); permission = new PermissionEntity { PermissionId = (all.Count == 0 ? 1 : all.Max(x => x.PermissionId) + 1), PermissionName = PermissionConstant.Download, PermissionConstant = PermissionConstant.Download }; await permissions.InsertOneAsync(permission); }
        var module = await modules.Find(x => x.ModuleConstant == AppModule.Offboarding).FirstOrDefaultAsync();
        if (module is not null && !await links.Find(x => x.ModuleId == module.ModuleId && x.PermissionId == permission.PermissionId).AnyAsync()) { var all = await links.Find(FilterDefinition<ModulePermission>.Empty).ToListAsync(); await links.InsertOneAsync(new ModulePermission { ModulePermissionId = all.Count == 0 ? 1 : all.Max(x => x.ModulePermissionId) + 1, ModuleId = module.ModuleId, PermissionId = permission.PermissionId, HasModuleAccess = true }); }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
