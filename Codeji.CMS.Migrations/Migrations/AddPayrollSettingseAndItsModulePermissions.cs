using AngleSharp.Dom;
using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public class AddPayrollSettingseAndItsModulePermissions : IMigration
{
    public string Id => $"2025-02-04-{typeof(AddPayrollSettingseAndItsModulePermissions).Name}";


    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        bool isMigrationExist = await migrations.Find(m => m.Id.Equals(Id)).AnyAsync();
        if (isMigrationExist) return;

        // get module collection 
        var modules = db.GetCollection<Module>(typeof(Module).Name);
        var modulePermission = db.GetCollection<ModulePermission>(typeof(ModulePermission).Name);
        var permission = db.GetCollection<Repository.Entities.RolePermissions.Permission>(typeof(Repository.Entities.RolePermissions.Permission).Name);
        var roles = db.GetCollection<Roles>("Roles");
        var rolePermission = db.GetCollection<RolePermission>("RolePermission");

        long totalModules = await modules.CountDocumentsAsync(FilterDefinition<Module>.Empty);
        long totalModulePermission = await modulePermission.CountDocumentsAsync(FilterDefinition<ModulePermission>.Empty);

        var allPermissions = await permission.Find(FilterDefinition<Repository.Entities.RolePermissions.Permission>.Empty).ToListAsync();

        if (await modules.Find(m => m.ModuleConstant.Equals(AppModule.PayrollSettings)).AnyAsync()) return;

        List<ModulePermission> payrollSettingsmodulePermissions = [];

        if (allPermissions.Count != 0)
        {
            Module module = new()
            {
                ModuleId = (int)totalModules + 1,
                ModuleName = AppModule.PayrollSettings,
                ModuleConstant = AppModule.PayrollSettings,
                SortOrder = (int)totalModules + 1,
            };

            await modules.InsertOneAsync(module);

            foreach (var perm in allPermissions)
            {
                payrollSettingsmodulePermissions.Add(new()
                {
                    ModulePermissionId = (int)(++totalModulePermission),
                    ModuleId = module.ModuleId,
                    PermissionId = perm.PermissionId,
                    HasModuleAccess = true,
                });
            }
            await modulePermission.InsertManyAsync(payrollSettingsmodulePermissions);

            // get all roles 

            var allRoles = await roles.Find(FilterDefinition<Roles>.Empty).ToListAsync();

            List<RolePermission> payrollSettingsRolePermission = [];

            foreach (var role in allRoles)
            {
                foreach (var payrollSettingsPermission in payrollSettingsmodulePermissions)
                {
                    if (role.RoleType == (int)EnumsHelper.Roles.Administrator || role.RoleType == (int)EnumsHelper.Roles.HR)
                    {
                        payrollSettingsRolePermission.Add(new()
                        {
                            RoleId = role.RolesId,
                            ModulePermissionId = payrollSettingsPermission.ModulePermissionId,
                            HasAccess = true,
                            IsAccessible = true,
                            CompanyId = role.CompanyId,
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                    else if (payrollSettingsPermission.PermissionId == 2)
                    {
                        payrollSettingsRolePermission.Add(new()
                        {
                            RoleId = role.RolesId,
                            ModulePermissionId = payrollSettingsPermission.ModulePermissionId,
                            HasAccess = true,
                            IsAccessible = true,
                            CompanyId = role.CompanyId,
                            CreatedDate = DateTime.UtcNow,
                        });
                    }
                }
            }
            // Skip when no roles exist yet — happens on a fresh DB before any company has registered.
            // New companies will receive permissions for this module via RoleServices.AddDefaultRole during registration.
            if (payrollSettingsRolePermission.Count > 0)
                await rolePermission.InsertManyAsync(payrollSettingsRolePermission);
            await migrations.InsertOneAsync(new()
            {
                Id = Id,
                ExecutedAt = DateTime.UtcNow,
            });
        }
    }

}
