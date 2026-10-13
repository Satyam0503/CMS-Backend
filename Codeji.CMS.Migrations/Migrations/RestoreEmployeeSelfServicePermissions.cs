using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class RestoreEmployeeSelfServicePermissions : IMigration
{
    public string Id => $"2026-07-22-02-{typeof(RestoreEmployeeSelfServicePermissions).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var roles = db.GetCollection<Roles>("Roles");
        var modules = db.GetCollection<Module>(typeof(Module).Name);
        var permissions = db.GetCollection<Repository.Entities.RolePermissions.Permission>(typeof(Repository.Entities.RolePermissions.Permission).Name);
        var modulePermissions = db.GetCollection<ModulePermission>(typeof(ModulePermission).Name);
        var rolePermissions = db.GetCollection<RolePermission>(typeof(RolePermission).Name);

        var employeeRoles = await roles.Find(r =>
            !r.IsDeleted && r.RoleType == (int)EnumsHelper.Roles.Employee).ToListAsync();
        var moduleList = await modules.Find(m =>
            m.ModuleConstant == AppModule.Attendance || m.ModuleConstant == AppModule.LeaveManagement).ToListAsync();
        var permissionList = await permissions.Find(p =>
            p.PermissionConstant == PermissionConst.View || p.PermissionConstant == PermissionConst.Create ||
            p.PermissionConstant == PermissionConst.Edit || p.PermissionConstant == PermissionConst.Delete).ToListAsync();

        var allowed = new Dictionary<string, string[]>
        {
            [AppModule.Attendance] = [PermissionConst.View, PermissionConst.Create, PermissionConst.Edit],
            [AppModule.LeaveManagement] = [PermissionConst.View, PermissionConst.Create, PermissionConst.Edit, PermissionConst.Delete],
        };

        foreach (var role in employeeRoles)
        foreach (var module in moduleList)
        foreach (var permission in permissionList.Where(p => allowed[module.ModuleConstant].Contains(p.PermissionConstant)))
        {
            var mp = await modulePermissions.Find(x =>
                x.ModuleId == module.ModuleId && x.PermissionId == permission.PermissionId).FirstOrDefaultAsync();
            if (mp == null) continue;
            if (await rolePermissions.Find(x => x.RoleId == role.RolesId && x.ModulePermissionId == mp.ModulePermissionId).AnyAsync()) continue;

            await rolePermissions.InsertOneAsync(new RolePermission
            {
                RolePermissionId = Guid.NewGuid().ToString(),
                RoleId = role.RolesId,
                ModulePermissionId = mp.ModulePermissionId,
                HasAccess = true,
                IsAccessible = true,
                CompanyId = role.CompanyId,
            });
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
