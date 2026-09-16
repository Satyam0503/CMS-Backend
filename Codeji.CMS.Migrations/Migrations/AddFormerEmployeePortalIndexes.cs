using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Indexes confidential portal tokens without persisting any raw token material.</summary>
public sealed class AddFormerEmployeePortalIndexes : IMigration
{
    public string Id => $"2026-08-27-{nameof(AddFormerEmployeePortalIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        var tokens = db.GetCollection<FormerEmployeeAccessToken>(typeof(FormerEmployeeAccessToken).Name);
        // Older records may have the non-nullable hash properties persisted as an
        // empty string. A normal unique index treats every empty string as the
        // same value and prevents the migration from completing. Keep those
        // legacy records readable, while enforcing uniqueness for real hashes.
        // Empty hashes are not usable credentials. Remove only those legacy
        // placeholder fields before indexing; this does not affect valid portal
        // sessions or tokens and keeps the migration compatible with MongoDB
        // versions that reject $ne in partial index expressions.
        await tokens.UpdateManyAsync(
            Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.TokenHash, string.Empty),
            Builders<FormerEmployeeAccessToken>.Update.Unset(x => x.TokenHash));
        await tokens.UpdateManyAsync(
            Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.SessionTokenHash, string.Empty),
            Builders<FormerEmployeeAccessToken>.Update.Unset(x => x.SessionTokenHash));

        // $type is supported by the deployed MongoDB version and excludes
        // null/missing legacy placeholders without using unsupported $ne.
        var tokenHashFilter = Builders<FormerEmployeeAccessToken>.Filter.Type(x => x.TokenHash, BsonType.String);
        var sessionHashFilter = Builders<FormerEmployeeAccessToken>.Filter.Type(x => x.SessionTokenHash, BsonType.String);

        // This migration may have partially completed in an earlier run. An
        // existing index with the same name but an older filter cannot be
        // altered by CreateOneAsync, so replace only these managed indexes.
        var existingIndexNames = (await tokens.Indexes.ListAsync()).ToList()
            .Select(index => index.GetValue("name", string.Empty).AsString)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var indexName in new[] { "ux_former_employee_portal_token_hash", "ux_former_employee_portal_session_hash" })
        {
            if (existingIndexNames.Contains(indexName))
                await tokens.Indexes.DropOneAsync(indexName);
        }

        await tokens.Indexes.CreateOneAsync(new CreateIndexModel<FormerEmployeeAccessToken>(
            Builders<FormerEmployeeAccessToken>.IndexKeys.Ascending(x => x.TokenHash),
            new CreateIndexOptions<FormerEmployeeAccessToken> { Name = "ux_former_employee_portal_token_hash", Unique = true, PartialFilterExpression = tokenHashFilter }));
        await tokens.Indexes.CreateOneAsync(new CreateIndexModel<FormerEmployeeAccessToken>(
            Builders<FormerEmployeeAccessToken>.IndexKeys.Ascending(x => x.SessionTokenHash),
            new CreateIndexOptions<FormerEmployeeAccessToken> { Name = "ux_former_employee_portal_session_hash", Unique = true, PartialFilterExpression = sessionHashFilter }));
        await tokens.Indexes.CreateOneAsync(new CreateIndexModel<FormerEmployeeAccessToken>(Builders<FormerEmployeeAccessToken>.IndexKeys.Ascending(x => x.CompanyId).Ascending(x => x.EmployeeExitId).Ascending(x => x.RevokedAtUtc), new CreateIndexOptions { Name = "ix_former_employee_portal_exit_revocation" }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
