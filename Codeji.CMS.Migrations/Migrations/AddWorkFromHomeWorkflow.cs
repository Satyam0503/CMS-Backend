using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Bson;
using MongoDB.Driver;
using PermissionEntity = Codeji.CMS.Repository.Entities.RolePermissions.Permission;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Idempotently adds the additive WFH collections, indexes and permission matrix.</summary>
public sealed class AddWorkFromHomeWorkflow : IMigration
{
    public string Id => $"2026-07-29-{nameof(AddWorkFromHomeWorkflow)}";
    private static readonly string[] Capabilities = [PermissionConst.ViewOwn, PermissionConst.CreateOwn, PermissionConst.EditOwn, PermissionConst.CancelOwn, PermissionConst.ViewTeam, PermissionConst.ApproveTeam, PermissionConst.ViewAll, PermissionConst.CreateForEmployee, PermissionConst.Override, PermissionConst.Revoke, PermissionConst.PolicyView, PermissionConst.PolicyEdit];

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        // RequestId is the Mongo _id and is therefore uniquely indexed by MongoDB itself.
        await CreateIndex(db, "WorkFromHomeRequest", "ix_wfh_user_dates", "CompanyId", "UserId", "FromDate", "ToDate");
        await CreateIndex(db, "WorkFromHomeRequest", "ix_wfh_user_status", "CompanyId", "UserId", "Status");
        await CreateIndex(db, "WorkFromHomeRequest", "ix_wfh_approver_status", "CompanyId", "ApproverUserId", "Status");
        await CreateIndex(db, "WorkFromHomeRequestLog", "ix_wfh_log_request_version", "CompanyId", "RequestId", "Version");
        await CreateUnique(db, "AttendanceRemarkOption", "ux_attendance_remark_company_code", "CompanyId", "Code");
        await CreateUnique(db, "Attendance", "ux_attendance_company_user_date", "CompanyId", "UserId", "Date");

        var permissions = db.GetCollection<PermissionEntity>(nameof(PermissionEntity));
        var modules = db.GetCollection<Module>(nameof(Module));
        var modulePermissions = db.GetCollection<ModulePermission>(nameof(ModulePermission));
        var roles = db.GetCollection<Roles>(nameof(Roles));
        var rolePermissions = db.GetCollection<RolePermission>(nameof(RolePermission));
        var existingPermissions = await permissions.Find(FilterDefinition<PermissionEntity>.Empty).ToListAsync();
        var nextPermissionId = existingPermissions.Select(x => x.PermissionId).DefaultIfEmpty().Max();
        foreach (var capability in Capabilities.Where(c => existingPermissions.All(x => x.PermissionConstant != c)))
        {
            var permission = new PermissionEntity { PermissionId = ++nextPermissionId, PermissionName = capability, PermissionConstant = capability };
            await permissions.InsertOneAsync(permission); existingPermissions.Add(permission);
        }
        var module = await modules.Find(x => x.ModuleConstant == AppModule.WorkFromHome).FirstOrDefaultAsync();
        if (module is null)
        {
            var moduleId = (int)(await modules.CountDocumentsAsync(FilterDefinition<Module>.Empty)) + 1;
            module = new Module { ModuleId = moduleId, ModuleName = "Work From Home", ModuleConstant = AppModule.WorkFromHome, SortOrder = moduleId };
            await modules.InsertOneAsync(module);
        }
        var existingModulePermissions = await modulePermissions.Find(x => x.ModuleId == module.ModuleId).ToListAsync();
        var nextModulePermissionId = (int)(await modulePermissions.CountDocumentsAsync(FilterDefinition<ModulePermission>.Empty));
        foreach (var permission in existingPermissions.Where(x => Capabilities.Contains(x.PermissionConstant) && existingModulePermissions.All(mp => mp.PermissionId != x.PermissionId)))
        {
            var mp = new ModulePermission { ModulePermissionId = ++nextModulePermissionId, ModuleId = module.ModuleId, PermissionId = permission.PermissionId, HasModuleAccess = true };
            await modulePermissions.InsertOneAsync(mp); existingModulePermissions.Add(mp);
        }
        foreach (var role in await roles.Find(FilterDefinition<Roles>.Empty).ToListAsync())
        foreach (var mp in existingModulePermissions)
        {
            if (await rolePermissions.Find(x => x.RoleId == role.RolesId && x.ModulePermissionId == mp.ModulePermissionId && x.CompanyId == role.CompanyId).AnyAsync()) continue;
            var capability = existingPermissions.Single(x => x.PermissionId == mp.PermissionId).PermissionConstant;
            var adminOrHr = role.RoleType is (int)EnumsHelper.Roles.Administrator or (int)EnumsHelper.Roles.HR;
            // This installation has no distinct Manager role enum. Manager approvals are
            // granted deliberately by administrators through the normal role editor.
            var granted = adminOrHr;
            if (granted) await rolePermissions.InsertOneAsync(new RolePermission { RoleId = role.RolesId, ModulePermissionId = mp.ModulePermissionId, HasAccess = true, IsAccessible = true, CompanyId = role.CompanyId, CreatedDate = DateTime.UtcNow });
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static async Task CreateIndex(IMongoDatabase db, string collection, string name, params string[] fields) => await db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(fields.Select(x => new BsonElement(x, 1))), new CreateIndexOptions { Name = name }));
    private static async Task CreateUnique(IMongoDatabase db, string collection, string name, params string[] fields)
    {
        try { await db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(fields.Select(x => new BsonElement(x, 1))), new CreateIndexOptions { Name = name, Unique = true })); }
        catch (MongoCommandException ex) when (ex.Code == 11000) { Console.WriteLine($"Skipped {name}: legacy duplicate keys require review."); }
    }
}
