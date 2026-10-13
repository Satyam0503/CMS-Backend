using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddAttendanceViewAllPermission : IMigration
{
    public string Id => $"2026-07-30-{nameof(AddAttendanceViewAllPermission)}";
    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var permissions = db.GetCollection<PermissionEntity>("Permission");
        var modules = db.GetCollection<Module>("Module");
        var modulePermissions = db.GetCollection<ModulePermission>("ModulePermission");
        var roles = db.GetCollection<Roles>("Roles");
        var rolePermissions = db.GetCollection<RolePermission>("RolePermission");
        var permission = await permissions.Find(x => x.PermissionConstant == PermissionConst.ViewAll).FirstOrDefaultAsync();
        if (permission is null)
        {
            permission = new PermissionEntity { PermissionId = (await permissions.Find(FilterDefinition<PermissionEntity>.Empty).SortByDescending(x => x.PermissionId).FirstOrDefaultAsync())?.PermissionId + 1 ?? 1, PermissionName = "View all", PermissionConstant = PermissionConst.ViewAll };
            await permissions.InsertOneAsync(permission);
        }
        var module = await modules.Find(x => x.ModuleConstant == AppModule.Attendance).FirstOrDefaultAsync();
        if (module is null) throw new InvalidOperationException("Attendance module is missing.");
        var modulePermission = await modulePermissions.Find(x => x.ModuleId == module.ModuleId && x.PermissionId == permission.PermissionId).FirstOrDefaultAsync();
        if (modulePermission is null)
        {
            var nextId = ((await modulePermissions.Find(FilterDefinition<ModulePermission>.Empty).SortByDescending(x => x.ModulePermissionId).FirstOrDefaultAsync())?.ModulePermissionId ?? 0) + 1;
            modulePermission = new ModulePermission { ModulePermissionId = nextId, ModuleId = module.ModuleId, PermissionId = permission.PermissionId, HasModuleAccess = true };
            await modulePermissions.InsertOneAsync(modulePermission);
        }
        var privilegedRoles = await roles.Find(x => !x.IsDeleted && (x.RoleType == (int)EnumsHelper.Roles.Administrator || x.RoleType == (int)EnumsHelper.Roles.HR)).ToListAsync();
        foreach (var role in privilegedRoles)
            if (!await rolePermissions.Find(x => x.RoleId == role.RolesId && x.ModulePermissionId == modulePermission.ModulePermissionId).AnyAsync())
                await rolePermissions.InsertOneAsync(new RolePermission { RoleId = role.RolesId, ModulePermissionId = modulePermission.ModulePermissionId, HasAccess = true, IsAccessible = true, CompanyId = role.CompanyId });
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
