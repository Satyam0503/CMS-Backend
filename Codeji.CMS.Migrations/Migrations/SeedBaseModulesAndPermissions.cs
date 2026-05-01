using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using MongoDB.Driver;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;

namespace Codeji.CMS.Migrations.Migrations;

public class SeedBaseModulesAndPermissions : IMigration
{
    public string Id => $"2024-12-01-{typeof(SeedBaseModulesAndPermissions).Name}";

    private static readonly (int Id, string Name, string Constant)[] BasePermissions =
    [
        (1, "View",   PermissionConst.View),
        (2, "Create", PermissionConst.Create),
        (3, "Edit",   PermissionConst.Edit),
        (4, "Delete", PermissionConst.Delete),
    ];

    private static readonly (string Constant, string DisplayName)[] BaseModules =
    [
        (AppModule.Employees,       "Employees"),
        (AppModule.Attendance,      "Attendance"),
        (AppModule.LeaveManagement, "Leave Management"),
        (AppModule.Calendar,        "Calendar"),
        (AppModule.NoticeBoard,     "Notice Board"),
        (AppModule.Jobs,            "Jobs"),
        (AppModule.Applications,    "Applications"),
        (AppModule.ProcessLog,      "Process Log"),
        (AppModule.PayRoll,         "PayRoll"),
    ];

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var permissions = db.GetCollection<Repository.Entities.RolePermissions.Permission>(
            typeof(Repository.Entities.RolePermissions.Permission).Name);
        var modules = db.GetCollection<Module>(typeof(Module).Name);
        var modulePermissions = db.GetCollection<ModulePermission>(typeof(ModulePermission).Name);

        // 1. Seed Permissions (skip any already present by PermissionId)
        var existingPermissionIds = (await permissions
            .Find(FilterDefinition<Repository.Entities.RolePermissions.Permission>.Empty)
            .Project(p => p.PermissionId)
            .ToListAsync()).ToHashSet();

        var newPermissions = BasePermissions
            .Where(p => !existingPermissionIds.Contains(p.Id))
            .Select(p => new Repository.Entities.RolePermissions.Permission
            {
                PermissionId = p.Id,
                PermissionName = p.Name,
                PermissionConstant = p.Constant,
            })
            .ToList();

        if (newPermissions.Count > 0)
            await permissions.InsertManyAsync(newPermissions);

        // 2. Seed Modules (skip any already present by ModuleConstant)
        var existingModuleConstants = (await modules
            .Find(FilterDefinition<Module>.Empty)
            .Project(m => m.ModuleConstant)
            .ToListAsync()).ToHashSet();

        long currentModuleCount = await modules.CountDocumentsAsync(FilterDefinition<Module>.Empty);
        var newModules = new List<Module>();
        foreach (var (constant, displayName) in BaseModules)
        {
            if (existingModuleConstants.Contains(constant)) continue;
            currentModuleCount++;
            newModules.Add(new Module
            {
                ModuleId = (int)currentModuleCount,
                ModuleName = displayName,
                ModuleConstant = constant,
                SortOrder = (int)currentModuleCount,
            });
        }
        if (newModules.Count > 0)
            await modules.InsertManyAsync(newModules);

        // 3. Seed ModulePermissions (cartesian product of all modules x all permissions,
        //    skipping any (ModuleId, PermissionId) pair already present).
        var allPermissions = await permissions
            .Find(FilterDefinition<Repository.Entities.RolePermissions.Permission>.Empty)
            .ToListAsync();
        var allModules = await modules
            .Find(FilterDefinition<Module>.Empty)
            .ToListAsync();

        var existingPairs = (await modulePermissions
            .Find(FilterDefinition<ModulePermission>.Empty)
            .Project(mp => new { mp.ModuleId, mp.PermissionId })
            .ToListAsync())
            .Select(x => (x.ModuleId, x.PermissionId))
            .ToHashSet();

        long currentModulePermissionCount = await modulePermissions
            .CountDocumentsAsync(FilterDefinition<ModulePermission>.Empty);

        var newModulePermissions = new List<ModulePermission>();
        foreach (var module in allModules)
        {
            foreach (var perm in allPermissions)
            {
                if (existingPairs.Contains((module.ModuleId, perm.PermissionId))) continue;
                currentModulePermissionCount++;
                newModulePermissions.Add(new ModulePermission
                {
                    ModulePermissionId = (int)currentModulePermissionCount,
                    ModuleId = module.ModuleId,
                    PermissionId = perm.PermissionId,
                    HasModuleAccess = true,
                });
            }
        }
        if (newModulePermissions.Count > 0)
            await modulePermissions.InsertManyAsync(newModulePermissions);

        await migrations.InsertOneAsync(new Migration
        {
            Id = Id,
            ExecutedAt = DateTime.UtcNow,
        });
    }
}
