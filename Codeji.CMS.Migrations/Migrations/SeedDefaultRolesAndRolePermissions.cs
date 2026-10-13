using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using MongoDB.Driver;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;

namespace Codeji.CMS.Migrations.Migrations;

public class SeedDefaultRolesAndRolePermissions : IMigration
{
    // 9999-12-31 prefix forces this to run last, after every module-adding
    // migration. That guarantees all referenced ModulePermissions exist.
    public string Id => $"9999-12-31-{typeof(SeedDefaultRolesAndRolePermissions).Name}";

    private static readonly (int RoleType, string Title, string Description, bool IsNotEditable)[] DefaultRoles =
    [
        (1, "Company Administrator",
            "Administrators have all permissions in the app. Limit this role to employees who will be in charge the client account",
            true),
        (2, "HR Manager",
            "The HR employee can: Add & Edit colleagues, -Edit engagement surveys and view results for all departments, -Assign roles to other employees",
            false),
        (4, "HR Executive",
            "HR Executives have the same application permissions as HR Managers.",
            false),
        (3, "Employee",
            "Regular Employee",
            false),
    ];

    private static IEnumerable<(string Module, string Permission)> FullAccess(string module)
    {
        yield return (module, PermissionConst.View);
        yield return (module, PermissionConst.Create);
        yield return (module, PermissionConst.Edit);
        yield return (module, PermissionConst.Delete);
    }

    private static IEnumerable<(string Module, string Permission)> HrManagerPermissions()
    {
        foreach (var p in FullAccess(AppModule.Employees)) yield return p;
        foreach (var p in FullAccess(AppModule.Attendance)) yield return p;
        foreach (var p in FullAccess(AppModule.LeaveManagement)) yield return p;
        foreach (var p in FullAccess(AppModule.Calendar)) yield return p;
        foreach (var p in FullAccess(AppModule.NoticeBoard)) yield return p;
        foreach (var p in FullAccess(AppModule.Jobs)) yield return p;
        foreach (var p in FullAccess(AppModule.Applications)) yield return p;
        yield return (AppModule.ProcessLog, PermissionConst.View);
        yield return (AppModule.PayRoll, PermissionConst.Create);
        yield return (AppModule.PayRoll, PermissionConst.Edit);
        yield return (AppModule.PayRoll, PermissionConst.Delete);
        foreach (var p in FullAccess(AppModule.PayrollSettings)) yield return p;
        yield return (AppModule.Policy, PermissionConst.View);
    }

    private static IEnumerable<(string Module, string Permission)> EmployeePermissions()
    {
        yield return (AppModule.Employees, PermissionConst.Create);
        foreach (var p in FullAccess(AppModule.Calendar)) yield return p;
        yield return (AppModule.NoticeBoard, PermissionConst.Create);
        yield return (AppModule.PayRoll, PermissionConst.Edit);
        yield return (AppModule.PayrollSettings, PermissionConst.Edit);
    }

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var rolesCol = db.GetCollection<Roles>(typeof(Roles).Name);
        var rolePermissionsCol = db.GetCollection<RolePermission>(typeof(RolePermission).Name);
        var modulesCol = db.GetCollection<Module>(typeof(Module).Name);
        var permissionsCol = db.GetCollection<PermissionEntity>(typeof(PermissionEntity).Name);
        var modulePermissionsCol = db.GetCollection<ModulePermission>(typeof(ModulePermission).Name);

        var now = DateTime.UtcNow;
        var seededRoleIdsByType = new Dictionary<int, string>();

        // 1. Seed Roles (global templates - CompanyId is null).
        //    Skip ones that already exist (matched by null CompanyId + RoleType).
        foreach (var (roleType, title, description, isNotEditable) in DefaultRoles)
        {
            var existing = await rolesCol
                .Find(r => r.CompanyId == null && r.RoleType == roleType)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                seededRoleIdsByType[roleType] = existing.RolesId;
                continue;
            }

            var role = new Roles
            {
                RoleType = roleType,
                CompanyId = null!,
                Titles = title,
                Description = description,
                HasAppAccess = true,
                IsNotEditable = isNotEditable,
                IsDefault = true,
                IsDeleted = false,
                UserRoles = new List<string>(),
                CreatedDate = now,
                UpdatedDate = now,
                CreatedBy = string.Empty,
                UpdatedBy = string.Empty,
            };
            await rolesCol.InsertOneAsync(role);
            seededRoleIdsByType[roleType] = role.RolesId;
        }

        // 2. Build a (ModuleConstant, PermissionConstant) -> ModulePermissionId lookup
        //    so the per-role permission specs above can be expressed in code constants.
        var modules = await modulesCol.Find(FilterDefinition<Module>.Empty).ToListAsync();
        var permissions = await permissionsCol.Find(FilterDefinition<PermissionEntity>.Empty).ToListAsync();
        var allMps = await modulePermissionsCol.Find(FilterDefinition<ModulePermission>.Empty).ToListAsync();

        var moduleConstById = modules.ToDictionary(m => m.ModuleId, m => m.ModuleConstant);
        var permConstById = permissions.ToDictionary(p => p.PermissionId, p => p.PermissionConstant);

        var mpIdByConstants = allMps
            .Where(mp => moduleConstById.ContainsKey(mp.ModuleId) && permConstById.ContainsKey(mp.PermissionId))
            .ToDictionary(
                mp => (moduleConstById[mp.ModuleId], permConstById[mp.PermissionId]),
                mp => mp.ModulePermissionId);

        IEnumerable<int> ResolveMpIds(IEnumerable<(string Module, string Permission)> pairs) =>
            pairs
                .Where(p => mpIdByConstants.ContainsKey((p.Module, p.Permission)))
                .Select(p => mpIdByConstants[(p.Module, p.Permission)]);

        // 3. For each role, compute the MP IDs to grant.
        //    Admin gets every MP currently in the DB; HR/Employee get explicit allowlists.
        var permissionPlan = new (int RoleType, IEnumerable<int> MpIds)[]
        {
            (1, allMps.Select(mp => mp.ModulePermissionId)),
            (2, ResolveMpIds(HrManagerPermissions())),
            (4, ResolveMpIds(HrManagerPermissions())),
            (3, ResolveMpIds(EmployeePermissions())),
        };

        // 4. Insert RolePermission rows, deduping against any pre-existing pairs.
        foreach (var (roleType, mpIds) in permissionPlan)
        {
            if (!seededRoleIdsByType.TryGetValue(roleType, out var roleId)) continue;

            var existingPairs = (await rolePermissionsCol
                .Find(rp => rp.RoleId == roleId)
                .Project(rp => rp.ModulePermissionId)
                .ToListAsync())
                .ToHashSet();

            var newRolePermissions = mpIds
                .Where(mpId => !existingPairs.Contains(mpId))
                .Select(mpId => new RolePermission
                {
                    RoleId = roleId,
                    ModulePermissionId = mpId,
                    HasAccess = true,
                    IsAccessible = true,
                })
                .ToList();

            if (newRolePermissions.Count > 0)
                await rolePermissionsCol.InsertManyAsync(newRolePermissions);
        }

        await migrations.InsertOneAsync(new Migration
        {
            Id = Id,
            ExecutedAt = DateTime.UtcNow,
        });
    }
}
