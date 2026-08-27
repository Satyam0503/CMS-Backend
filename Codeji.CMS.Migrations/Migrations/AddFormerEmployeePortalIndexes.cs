using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
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
        await tokens.Indexes.CreateOneAsync(new CreateIndexModel<FormerEmployeeAccessToken>(Builders<FormerEmployeeAccessToken>.IndexKeys.Ascending(x => x.TokenHash), new CreateIndexOptions { Name = "ux_former_employee_portal_token_hash", Unique = true }));
        await tokens.Indexes.CreateOneAsync(new CreateIndexModel<FormerEmployeeAccessToken>(Builders<FormerEmployeeAccessToken>.IndexKeys.Ascending(x => x.SessionTokenHash), new CreateIndexOptions { Name = "ux_former_employee_portal_session_hash", Unique = true, Sparse = true }));
        await tokens.Indexes.CreateOneAsync(new CreateIndexModel<FormerEmployeeAccessToken>(Builders<FormerEmployeeAccessToken>.IndexKeys.Ascending(x => x.CompanyId).Ascending(x => x.EmployeeExitId).Ascending(x => x.RevokedAtUtc), new CreateIndexOptions { Name = "ix_former_employee_portal_exit_revocation" }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
