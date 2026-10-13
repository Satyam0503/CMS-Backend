using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Repairs legacy role data so company Admin and HR can review WFH requests.</summary>
public sealed class GrantWorkFromHomeReviewerPermissions : IMigration
{
    public string Id => $"2026-08-11-{nameof(GrantWorkFromHomeReviewerPermissions)}";
    private static readonly string[] ReviewerPermissions = [PermissionConst.ViewTeam, PermissionConst.ViewAll, PermissionConst.ApproveTeam];

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var module = await db.GetCollection<Module>(nameof(Module)).Find(x => x.ModuleConstant == AppModule.WorkFromHome).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Work From Home module is missing.");
        var permissions = await db.GetCollection<PermissionEntity>(nameof(PermissionEntity)).Find(x => ReviewerPermissions.Contains(x.PermissionConstant)).ToListAsync();
        var permissionIds = permissions.Select(x => x.PermissionId).ToHashSet();
        var modulePermissions = await db.GetCollection<ModulePermission>(nameof(ModulePermission)).Find(x => x.ModuleId == module.ModuleId && permissionIds.Contains(x.PermissionId)).ToListAsync();
        if (modulePermissions.Count != ReviewerPermissions.Length) throw new InvalidOperationException("Work From Home reviewer permissions are missing.");

        var roles = await db.GetCollection<Roles>(nameof(Roles)).Find(x => !x.IsDeleted && (x.RoleType == (int)EnumsHelper.Roles.Administrator || x.RoleType == (int)EnumsHelper.Roles.HR)).ToListAsync();
        var rolePermissions = db.GetCollection<RolePermission>(nameof(RolePermission));
        foreach (var role in roles)
        foreach (var modulePermission in modulePermissions)
        {
            if (await rolePermissions.Find(x => x.CompanyId == role.CompanyId && x.RoleId == role.RolesId && x.ModulePermissionId == modulePermission.ModulePermissionId).AnyAsync()) continue;
            await rolePermissions.InsertOneAsync(new RolePermission { CompanyId = role.CompanyId, RoleId = role.RolesId, ModulePermissionId = modulePermission.ModulePermissionId, HasAccess = true, IsAccessible = true, CreatedDate = DateTime.UtcNow });
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
