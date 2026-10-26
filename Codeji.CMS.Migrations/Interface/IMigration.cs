using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Interface;

public interface IMigration
{
    string Id { get; }
    Task ExecuteAsync(IMongoDatabase db);
}

/// <summary>
/// Marks a migration that mutates existing business records. These migrations
/// are intentionally withheld from Dev and UAT deployments.
/// </summary>
public interface IDataBackfillMigration : IMigration
{
}

/// <summary>Marks a historical migration replaced by a safer tenant-scoped migration.</summary>
public interface IRetiredMigration : IMigration
{
}

