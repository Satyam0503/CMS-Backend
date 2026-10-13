using Codeji.CMS.Migrations.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Removes duplicate tenant role rows created before role seeding was made
/// idempotent. Users and unique role permissions are retained on one role.
/// </summary>
public sealed class DeduplicateCompanyRoles : IMigration
{
    public string Id => $"2026-08-04-{nameof(DeduplicateCompanyRoles)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<BsonDocument>("Migration");
        if (await migrations.Find(Builders<BsonDocument>.Filter.Eq("_id", Id)).AnyAsync()) return;

        var roles = db.GetCollection<BsonDocument>("Roles");
        var users = db.GetCollection<BsonDocument>("EmpUser");
        var permissions = db.GetCollection<BsonDocument>("RolePermission");
        var rows = await roles.Find(Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Exists("CompanyId", true),
            Builders<BsonDocument>.Filter.Ne("CompanyId", ""),
            Builders<BsonDocument>.Filter.Ne("IsDeleted", true))).ToListAsync();

        var removedRoles = 0;
        var movedUsers = 0;
        var movedPermissions = 0;
        foreach (var group in rows.GroupBy(role => (role.GetValue("CompanyId", "").AsString, role.GetValue("RoleType", 0).ToInt32())))
        {
            var duplicates = group.OrderByDescending(role => role.GetValue("IsNotEditable", false).ToBoolean())
                .ThenByDescending(role => role.GetValue("UpdatedDate", BsonNull.Value))
                .ThenByDescending(role => role.GetValue("CreatedDate", BsonNull.Value))
                .ToList();
            if (duplicates.Count < 2) continue;

            var keeper = duplicates[0];
            var keeperId = keeper["_id"].AsString;
            foreach (var duplicate in duplicates.Skip(1))
            {
                var duplicateId = duplicate["_id"].AsString;
                movedUsers += (int)(await users.UpdateManyAsync(
                    Builders<BsonDocument>.Filter.Eq("RoleId", duplicateId),
                    Builders<BsonDocument>.Update.Set("RoleId", keeperId))).ModifiedCount;

                var duplicatePermissions = await permissions.Find(Builders<BsonDocument>.Filter.Eq("RoleId", duplicateId)).ToListAsync();
                foreach (var permission in duplicatePermissions)
                {
                    var modulePermissionId = permission.GetValue("ModulePermissionId", 0).ToInt32();
                    var existing = await permissions.Find(Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("RoleId", keeperId),
                        Builders<BsonDocument>.Filter.Eq("ModulePermissionId", modulePermissionId))).FirstOrDefaultAsync();
                    if (existing == null)
                    {
                        await permissions.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", permission["_id"]), Builders<BsonDocument>.Update.Set("RoleId", keeperId));
                        movedPermissions++;
                    }
                    else
                    {
                        await permissions.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", permission["_id"]));
                    }
                }
                await roles.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", duplicate["_id"]));
                removedRoles++;
            }
        }

        var tenantRoleIndex = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("RoleType"),
            new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_roles_company_role_type",
                Unique = true,
                // Older MongoDB versions do not allow $ne in a partial index.
                // $gt keeps empty/global template roles outside this tenant-only index.
                PartialFilterExpression = Builders<BsonDocument>.Filter.Gt("CompanyId", "")
            });
        await roles.Indexes.CreateOneAsync(tenantRoleIndex);

        Console.WriteLine($"{{\"migration\":\"{Id}\",\"removedRoles\":{removedRoles},\"movedUsers\":{movedUsers},\"movedPermissions\":{movedPermissions}}}");
        await migrations.InsertOneAsync(new BsonDocument { { "_id", Id }, { "ExecutedAt", DateTime.UtcNow } });
    }
}
