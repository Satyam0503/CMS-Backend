using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Repairs the initial WFH permission seed so employee self-service is usable.</summary>
public sealed class GrantWorkFromHomeEmployeeSelfServicePermissions : IMigration
{
    public string Id => $"2026-07-31-{nameof(GrantWorkFromHomeEmployeeSelfServicePermissions)}";
    private static readonly string[] OwnPermissions = [PermissionConst.ViewOwn, PermissionConst.CreateOwn, PermissionConst.EditOwn, PermissionConst.CancelOwn];

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var modules = db.GetCollection<Module>(nameof(Module));
        var module = await modules.Find(x => x.ModuleConstant == AppModule.WorkFromHome).FirstOrDefaultAsync();
        if (module is null) throw new InvalidOperationException("WFH module is missing.");
        var permissions = db.GetCollection<PermissionEntity>(nameof(PermissionEntity));
        var allowedIds = (await permissions.Find(x => OwnPermissions.Contains(x.PermissionConstant)).ToListAsync()).Select(x => x.PermissionId).ToHashSet();
        var modulePermissions = db.GetCollection<ModulePermission>(nameof(ModulePermission));
        var allowedModulePermissions = (await modulePermissions.Find(x => x.ModuleId == module.ModuleId && allowedIds.Contains(x.PermissionId)).ToListAsync());
        var roles = db.GetCollection<Roles>(nameof(Roles));
        var employeeRoles = await roles.Find(x => x.RoleType == (int)EnumsHelper.Roles.Employee && !x.IsDeleted).ToListAsync();
        var rolePermissions = db.GetCollection<RolePermission>(nameof(RolePermission));
        foreach (var role in employeeRoles)
        foreach (var modulePermission in allowedModulePermissions)
        {
            if (await rolePermissions.Find(x => x.CompanyId == role.CompanyId && x.RoleId == role.RolesId && x.ModulePermissionId == modulePermission.ModulePermissionId).AnyAsync()) continue;
            await rolePermissions.InsertOneAsync(new RolePermission { CompanyId = role.CompanyId, RoleId = role.RolesId, ModulePermissionId = modulePermission.ModulePermissionId, HasAccess = true, IsAccessible = true, CreatedDate = DateTime.UtcNow });
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
