using Codeji.CMS.Migrations.Interface;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations;

public class MigrationRunner
{
    private readonly IEnumerable<IMigration> _migrations;
    private readonly IMongoDatabase _db;

    public MigrationRunner(IEnumerable<IMigration> migrations, IMongoDatabase db)
    {
        _migrations = migrations;
        _db = db;
    }

    public async Task RunAsync()
    {
        foreach (var migration in _migrations)
        {
            Console.WriteLine($"Running migration {migration.Id}...");
            await migration.ExecuteAsync(_db);
        }
        Console.WriteLine("All migrations completed.");
    }
}
