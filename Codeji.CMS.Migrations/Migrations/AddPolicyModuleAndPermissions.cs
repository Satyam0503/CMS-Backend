using AngleSharp.Dom;
using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public class AddPolicyModuleAndItsModulePermissions : IMigration
{
    public string Id => $"2026-01-01-{typeof(AddPolicyModuleAndItsModulePermissions).Name}";

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

        if (await modules.Find(m => m.ModuleConstant.Equals(AppModule.Policy)).AnyAsync()) return;

        List<ModulePermission> policymodulePermissions = [];

        if (allPermissions.Count != 0)
        {
            Module module = new()
            {
                ModuleId = (int)totalModules + 1,
                ModuleName = AppModule.Policy,
                ModuleConstant = AppModule.Policy,
                SortOrder = (int)totalModules + 1,
            };

            await modules.InsertOneAsync(module);

            foreach (var perm in allPermissions)
            {
                policymodulePermissions.Add(new()
                {
                    ModulePermissionId = (int)(++totalModulePermission),
                    ModuleId = module.ModuleId,
                    PermissionId = perm.PermissionId,
                    HasModuleAccess = true,
                });
            }
            await modulePermission.InsertManyAsync(policymodulePermissions);

            // get all roles 

            var allRoles = await roles.Find(FilterDefinition<Roles>.Empty).ToListAsync();

            List<RolePermission> policyRolePermission = [];

            foreach (var role in allRoles)
            {
                foreach (var policyPermission in policymodulePermissions)
                {
                    if (role.RoleType == (int)EnumsHelper.Roles.Administrator || role.RoleType == (int)EnumsHelper.Roles.HR)
                    {
                        policyRolePermission.Add(new()
                        {
                            RoleId = role.RolesId,
                            ModulePermissionId = policyPermission.ModulePermissionId,
                            HasAccess = true,
                            IsAccessible = true,
                            CompanyId = role.CompanyId,
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                    else if (policyPermission.PermissionId == 2)
                    {
                        policyRolePermission.Add(new()
                        {
                            RoleId = role.RolesId,
                            ModulePermissionId = policyPermission.ModulePermissionId,
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
            if (policyRolePermission.Count > 0)
                await rolePermission.InsertManyAsync(policyRolePermission);
            await migrations.InsertOneAsync(new()
            {
                Id = Id,
                ExecutedAt = DateTime.UtcNow,
            });
        }
    }

}
