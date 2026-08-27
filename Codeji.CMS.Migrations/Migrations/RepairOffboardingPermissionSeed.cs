using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using MongoDB.Driver;
using PermissionConstant = Codeji.CMS.Utility.Constraints.Permission;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Repairs the canonical permission/module records without changing any company's role grants.</summary>
public sealed class RepairOffboardingPermissionSeed : IMigration
{
    public string Id => $"2026-08-27-{nameof(RepairOffboardingPermissionSeed)}";
    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration)); if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var permissions = db.GetCollection<PermissionEntity>(typeof(PermissionEntity).Name); var modules = db.GetCollection<Module>(nameof(Module)); var links = db.GetCollection<ModulePermission>(nameof(ModulePermission));
        string[] constants = [PermissionConstant.View, PermissionConstant.Create, PermissionConstant.Edit, PermissionConstant.Delete, PermissionConstant.Submit, PermissionConstant.Review, PermissionConstant.Approve, PermissionConstant.Complete, PermissionConstant.Evidence, PermissionConstant.Download, PermissionConstant.Override];
        var allPermissions = await permissions.Find(FilterDefinition<PermissionEntity>.Empty).ToListAsync(); var nextPermission = allPermissions.DefaultIfEmpty().Max(x => x?.PermissionId ?? 0);
        foreach (var constant in constants.Where(value => allPermissions.All(x => x.PermissionConstant != value))) { var row = new PermissionEntity { PermissionId = ++nextPermission, PermissionName = constant, PermissionConstant = constant }; await permissions.InsertOneAsync(row); allPermissions.Add(row); }
        var module = await modules.Find(x => x.ModuleConstant == AppModule.Offboarding).FirstOrDefaultAsync();
        if (module is not null)
        {
            var existingLinks = await links.Find(x => x.ModuleId == module.ModuleId).ToListAsync(); var nextLink = (await links.Find(FilterDefinition<ModulePermission>.Empty).ToListAsync()).DefaultIfEmpty().Max(x => x?.ModulePermissionId ?? 0);
            foreach (var permission in allPermissions.Where(x => constants.Contains(x.PermissionConstant) && existingLinks.All(link => link.PermissionId != x.PermissionId))) { var link = new ModulePermission { ModulePermissionId = ++nextLink, ModuleId = module.ModuleId, PermissionId = permission.PermissionId, HasModuleAccess = true }; await links.InsertOneAsync(link); existingLinks.Add(link); }
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
