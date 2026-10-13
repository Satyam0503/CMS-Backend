using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;

namespace Codeji.CMS.Migrations.Migrations;

// Permissions are administrator-configured policy. Remove the non-View grants
// introduced by RestoreEmployeeSelfServicePermissions so the saved role matrix
// is authoritative again (the Employee role shown in the UI is View-only).
public sealed class UndoForcedEmployeeSelfServicePermissions : IMigration
{
    public string Id => $"2026-07-22-03-{typeof(UndoForcedEmployeeSelfServicePermissions).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var roles = db.GetCollection<Roles>("Roles");
        var modules = db.GetCollection<Module>(typeof(Module).Name);
        var permissions = db.GetCollection<Repository.Entities.RolePermissions.Permission>(typeof(Repository.Entities.RolePermissions.Permission).Name);
        var modulePermissions = db.GetCollection<ModulePermission>(typeof(ModulePermission).Name);
        var rolePermissions = db.GetCollection<RolePermission>(typeof(RolePermission).Name);

        var employeeRoleIds = await roles.Find(r => !r.IsDeleted && r.RoleType == (int)EnumsHelper.Roles.Employee)
            .Project(r => r.RolesId).ToListAsync();
        var moduleIds = await modules.Find(m => m.ModuleConstant == AppModule.Attendance || m.ModuleConstant == AppModule.LeaveManagement)
            .Project(m => m.ModuleId).ToListAsync();
        var nonViewPermissionIds = await permissions.Find(p =>
                p.PermissionConstant == PermissionConst.Create || p.PermissionConstant == PermissionConst.Edit || p.PermissionConstant == PermissionConst.Delete)
            .Project(p => p.PermissionId).ToListAsync();
        var modulePermissionIds = await modulePermissions.Find(mp =>
                moduleIds.Contains(mp.ModuleId) && nonViewPermissionIds.Contains(mp.PermissionId))
            .Project(mp => mp.ModulePermissionId).ToListAsync();

        await rolePermissions.DeleteManyAsync(rp =>
            employeeRoleIds.Contains(rp.RoleId) && modulePermissionIds.Contains(rp.ModulePermissionId));

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
