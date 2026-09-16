using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using MongoDB.Driver;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Extends the original offboarding migration without changing existing role assignments.</summary>
public sealed class AddOffboardingOperationPermissions : IMigration
{
    public string Id => $"2026-08-27-{nameof(AddOffboardingOperationPermissions)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var permissions = db.GetCollection<PermissionEntity>(nameof(PermissionEntity));
        var modules = db.GetCollection<Module>(nameof(Module));
        var links = db.GetCollection<ModulePermission>(nameof(ModulePermission));
        string[] values = [PermissionConst.PolicyManage, PermissionConst.CaseManage, PermissionConst.AssetManage, PermissionConst.AccessManage, PermissionConst.FinanceManage, PermissionConst.BenefitsManage, PermissionConst.EvidenceManage, PermissionConst.PortalManage];
        var rows = await permissions.Find(FilterDefinition<PermissionEntity>.Empty).ToListAsync();
        var nextPermissionId = rows.Count == 0 ? 0 : rows.Max(x => x.PermissionId);
        foreach (var value in values.Where(value => rows.All(x => x.PermissionConstant != value)))
        {
            var permission = new PermissionEntity { PermissionId = ++nextPermissionId, PermissionName = value, PermissionConstant = value };
            await permissions.InsertOneAsync(permission); rows.Add(permission);
        }
        var module = await modules.Find(x => x.ModuleConstant == AppModule.Offboarding).FirstOrDefaultAsync();
        if (module is not null)
        {
            var existing = await links.Find(x => x.ModuleId == module.ModuleId).ToListAsync();
            var nextLinkId = (await links.Find(FilterDefinition<ModulePermission>.Empty).ToListAsync()).DefaultIfEmpty().Max(x => x?.ModulePermissionId ?? 0);
            foreach (var permission in rows.Where(x => values.Contains(x.PermissionConstant) && existing.All(link => link.PermissionId != x.PermissionId)))
                await links.InsertOneAsync(new ModulePermission { ModulePermissionId = ++nextLinkId, ModuleId = module.ModuleId, PermissionId = permission.PermissionId, HasModuleAccess = true });
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
