using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;

namespace Codeji.CMS.Migrations.Migrations;

// AddPayrollSettingseAndItsModulePermissions incorrectly granted every non-Admin/HR role a
// default "Create" permission on Payroll Settings (with no matching View/Edit/Delete access).
// Revokes those over-permissive grants for environments where that migration already ran;
// Admin/HR role permissions are left untouched.
public class RevokeDefaultPayrollSettingsCreateForNonAdminRoles : IMigration
{
    public string Id => $"2026-07-20-{typeof(RevokeDefaultPayrollSettingsCreateForNonAdminRoles).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var modules = db.GetCollection<Module>(typeof(Module).Name);
        var modulePermissions = db.GetCollection<ModulePermission>(typeof(ModulePermission).Name);
        var permissions = db.GetCollection<Repository.Entities.RolePermissions.Permission>(typeof(Repository.Entities.RolePermissions.Permission).Name);
        var roles = db.GetCollection<Roles>("Roles");
        var rolePermissions = db.GetCollection<RolePermission>("RolePermission");

        var payrollSettingsModule = await modules.Find(m => m.ModuleConstant == AppModule.PayrollSettings).FirstOrDefaultAsync();
        var createPermission = await permissions.Find(p => p.PermissionConstant == PermissionConst.Create).FirstOrDefaultAsync();

        if (payrollSettingsModule != null && createPermission != null)
        {
            var payrollSettingsCreateModulePermission = await modulePermissions
                .Find(mp => mp.ModuleId == payrollSettingsModule.ModuleId && mp.PermissionId == createPermission.PermissionId)
                .FirstOrDefaultAsync();

            if (payrollSettingsCreateModulePermission != null)
            {
                var nonAdminHrRoleIds = await roles
                    .Find(r => r.RoleType != (int)EnumsHelper.Roles.Administrator && r.RoleType != (int)EnumsHelper.Roles.HR)
                    .Project(r => r.RolesId)
                    .ToListAsync();

                await rolePermissions.DeleteManyAsync(rp =>
                    rp.ModulePermissionId == payrollSettingsCreateModulePermission.ModulePermissionId &&
                    nonAdminHrRoleIds.Contains(rp.RoleId));
            }
        }

        await migrations.InsertOneAsync(new Migration
        {
            Id = Id,
            ExecutedAt = DateTime.UtcNow,
        });
    }
}
