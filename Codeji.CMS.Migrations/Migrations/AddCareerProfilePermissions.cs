using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddCareerProfilePermissions : IMigration
{
    public string Id => $"2026-07-24-07-{typeof(AddCareerProfilePermissions).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var modules = db.GetCollection<Module>("Module");
        var permissions = db.GetCollection<Repository.Entities.RolePermissions.Permission>("Permission");
        var modulePermissions = db.GetCollection<ModulePermission>("ModulePermission");
        var roles = db.GetCollection<Roles>("Roles");
        var rolePermissions = db.GetCollection<RolePermission>("RolePermission");

        var module = await modules.Find(x => x.ModuleConstant == AppModule.CareerProfile).FirstOrDefaultAsync();
        if (module is null)
        {
            var existing = await modules.Find(FilterDefinition<Module>.Empty).SortByDescending(x => x.ModuleId).FirstOrDefaultAsync();
            module = new Module
            {
                ModuleId = (existing?.ModuleId ?? 0) + 1,
                ModuleName = "Career Profile",
                ModuleConstant = AppModule.CareerProfile,
                SortOrder = (existing?.SortOrder ?? 0) + 1
            };
            await modules.InsertOneAsync(module);
        }

        var allowed = await permissions.Find(x =>
            x.PermissionConstant == Utility.Constraints.Permission.View ||
            x.PermissionConstant == Utility.Constraints.Permission.Edit).ToListAsync();
        var last = await modulePermissions.Find(FilterDefinition<ModulePermission>.Empty)
            .SortByDescending(x => x.ModulePermissionId).FirstOrDefaultAsync();
        var nextId = last?.ModulePermissionId ?? 0;
        var created = new List<ModulePermission>();
        foreach (var permission in allowed)
        {
            var existing = await modulePermissions.Find(x =>
                x.ModuleId == module.ModuleId && x.PermissionId == permission.PermissionId).FirstOrDefaultAsync();
            if (existing is not null) { created.Add(existing); continue; }
            var item = new ModulePermission
            {
                ModulePermissionId = ++nextId, ModuleId = module.ModuleId,
                PermissionId = permission.PermissionId, HasModuleAccess = true
            };
            await modulePermissions.InsertOneAsync(item);
            created.Add(item);
        }

        var adminRoles = await roles.Find(x =>
            !x.IsDeleted && (x.RoleType == (int)EnumsHelper.Roles.Administrator ||
                             x.RoleType == (int)EnumsHelper.Roles.HR)).ToListAsync();
        foreach (var role in adminRoles)
        foreach (var permission in created)
        {
            if (await rolePermissions.Find(x => x.RoleId == role.RolesId &&
                x.ModulePermissionId == permission.ModulePermissionId).AnyAsync()) continue;
            await rolePermissions.InsertOneAsync(new RolePermission
            {
                RoleId = role.RolesId, ModulePermissionId = permission.ModulePermissionId,
                HasAccess = true, IsAccessible = true, CompanyId = role.CompanyId,
                CreatedBy = "migration", CreatedDate = DateTime.UtcNow
            });
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
