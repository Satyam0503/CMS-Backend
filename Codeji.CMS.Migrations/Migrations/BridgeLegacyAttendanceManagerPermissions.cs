using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Preserves existing manager access when legacy attendance permissions are replaced by
/// granular permissions. Employee roles are deliberately excluded from company access.
/// </summary>
public sealed class BridgeLegacyAttendanceManagerPermissions : IMigration
{
    public string Id => $"2026-08-02-{nameof(BridgeLegacyAttendanceManagerPermissions)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var modules = db.GetCollection<Module>(nameof(Module));
        var permissions = db.GetCollection<PermissionEntity>(nameof(PermissionEntity));
        var modulePermissions = db.GetCollection<ModulePermission>(nameof(ModulePermission));
        var rolePermissions = db.GetCollection<RolePermission>(nameof(RolePermission));
        var roles = await db.GetCollection<Roles>(nameof(Roles)).Find(x => !x.IsDeleted).ToListAsync();

        var attendance = await modules.Find(x => x.ModuleConstant == AppModule.Attendance).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Attendance module is missing.");
        var dashboard = await modules.Find(x => x.ModuleConstant == AppModule.Dashboard).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Dashboard module is missing. Run the dashboard permission migration first.");

        var permissionConstants = new[]
        {
            PermissionConst.View, PermissionConst.Create, PermissionConst.Edit,
            PermissionConst.ViewAll, PermissionConst.CreateForEmployee, PermissionConst.Override
        };
        var permissionList = await permissions.Find(x => permissionConstants.Contains(x.PermissionConstant)).ToListAsync();
        var permissionIds = permissionList.ToDictionary(x => x.PermissionConstant, x => x.PermissionId);

        var attendanceLinks = await modulePermissions.Find(x => x.ModuleId == attendance.ModuleId).ToListAsync();
        var linkByPermission = attendanceLinks
            .Where(x => permissionList.Any(p => p.PermissionId == x.PermissionId))
            .ToDictionary(x => permissionList.First(p => p.PermissionId == x.PermissionId).PermissionConstant, x => x);

        var required = new[] { PermissionConst.View, PermissionConst.Create, PermissionConst.Edit, PermissionConst.ViewAll, PermissionConst.CreateForEmployee, PermissionConst.Override };
        var missing = required.Where(x => !linkByPermission.ContainsKey(x)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Attendance permission links are missing: {string.Join(", ", missing)}.");

        var dashboardViewAll = await modulePermissions.Find(x => x.ModuleId == dashboard.ModuleId && x.PermissionId == permissionIds[PermissionConst.ViewAll]).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Dashboard ViewAll permission link is missing.");

        var legacyLinkIds = new[]
        {
            linkByPermission[PermissionConst.View].ModulePermissionId,
            linkByPermission[PermissionConst.Create].ModulePermissionId,
            linkByPermission[PermissionConst.Edit].ModulePermissionId
        };
        var existingAccess = await rolePermissions.Find(x => legacyLinkIds.Contains(x.ModulePermissionId) && x.HasAccess && x.IsAccessible).ToListAsync();

        foreach (var role in roles.Where(x => x.RoleType != (int)EnumsHelper.Roles.Employee))
        {
            var roleAccess = existingAccess
                .Where(x => x.CompanyId == role.CompanyId && x.RoleId == role.RolesId)
                .Select(x => x.ModulePermissionId)
                .ToHashSet();

            var grants = new List<int>();
            if (roleAccess.Contains(linkByPermission[PermissionConst.View].ModulePermissionId))
                grants.Add(linkByPermission[PermissionConst.ViewAll].ModulePermissionId);
            if (roleAccess.Contains(linkByPermission[PermissionConst.Create].ModulePermissionId))
                grants.Add(linkByPermission[PermissionConst.CreateForEmployee].ModulePermissionId);
            if (roleAccess.Contains(linkByPermission[PermissionConst.Edit].ModulePermissionId))
                grants.Add(linkByPermission[PermissionConst.Override].ModulePermissionId);

            foreach (var grant in grants)
                await GrantAsync(rolePermissions, role, grant);

            // A role that could previously see attendance for the organisation is a company manager.
            if (grants.Count > 0)
                await GrantAsync(rolePermissions, role, dashboardViewAll.ModulePermissionId);
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static async Task GrantAsync(IMongoCollection<RolePermission> rolePermissions, Roles role, int modulePermissionId)
    {
        var existing = await rolePermissions.Find(x => x.CompanyId == role.CompanyId && x.RoleId == role.RolesId && x.ModulePermissionId == modulePermissionId).FirstOrDefaultAsync();
        if (existing is null)
        {
            await rolePermissions.InsertOneAsync(new RolePermission
            {
                RolePermissionId = Guid.NewGuid().ToString(),
                CompanyId = role.CompanyId,
                RoleId = role.RolesId,
                ModulePermissionId = modulePermissionId,
                HasAccess = true,
                IsAccessible = true,
                CreatedDate = DateTime.UtcNow
            });
            return;
        }

        await rolePermissions.UpdateOneAsync(
            x => x.RolePermissionId == existing.RolePermissionId,
            Builders<RolePermission>.Update.Set(x => x.HasAccess, true).Set(x => x.IsAccessible, true));
    }
}
