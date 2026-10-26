using Codeji.CMS.Migrations.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations;

public class MigrationRunner
{
    private readonly IEnumerable<IMigration> _migrations;
    private readonly IMongoDatabase _db;
    private readonly MigrationTarget _target;

    public MigrationRunner(IEnumerable<IMigration> migrations, IMongoDatabase db, MigrationTarget target)
    {
        _migrations = migrations;
        _db = db;
        _target = target;
    }

    public async Task RunAsync()
    {
        var hello = await _db.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
        var isReplicaSet = hello.TryGetValue("setName", out var replicaSetName) && replicaSetName.IsString && !string.IsNullOrWhiteSpace(replicaSetName.AsString);
        var isShardedCluster = hello.TryGetValue("msg", out var topologyMessage) && topologyMessage.IsString && topologyMessage.AsString == "isdbgrid";
        var supportsTransactions = hello.TryGetValue("maxWireVersion", out var wireVersion) && wireVersion.IsNumeric && wireVersion.ToInt32() >= 7;
        if ((!isReplicaSet && !isShardedCluster) || !supportsTransactions)
        {
            throw new InvalidOperationException("WFH requires MongoDB transaction support (replica set or supported sharded cluster, wire version 7+). Configure the target database before applying WFH migrations.");
        }

        var migrationCollection = _db.GetCollection<BsonDocument>("Migration");

        // 1. Pre-fetch all applied migration IDs from MongoDB
        var appliedDocs = await migrationCollection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var appliedMigrationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var doc in appliedDocs)
        {
            if (doc.Contains("_id") && doc["_id"].IsString)
                appliedMigrationIds.Add(doc["_id"].AsString);
            else if (doc.Contains("Id") && doc["Id"].IsString)
                appliedMigrationIds.Add(doc["Id"].AsString);
        }

        int appliedCount = 0;
        int skippedCount = 0;
        int withheldBackfillCount = 0;

        foreach (var migration in _migrations)
        {
            // 2. Skip already applied migrations completely
            if (appliedMigrationIds.Contains(migration.Id))
            {
                skippedCount++;
                continue;
            }

            if (MigrationTargetPolicy.ShouldWithhold(migration, _target))
            {
                withheldBackfillCount++;
                Console.WriteLine($"Withholding data backfill {migration.Id} for {_target}; existing records were not changed.");
                continue;
            }

            // 3. Execute only pending migrations
            Console.WriteLine($"Running migration {migration.Id}...");
            try
            {
                await migration.ExecuteAsync(_db);

                // 4. Ensure migration ledger record exists in MongoDB upon success
                var alreadyRecorded = await migrationCollection.Find(
                    Builders<BsonDocument>.Filter.Or(
                        Builders<BsonDocument>.Filter.Eq("_id", migration.Id),
                        Builders<BsonDocument>.Filter.Eq("Id", migration.Id)
                    )).AnyAsync();

                if (!alreadyRecorded)
                {
                    await migrationCollection.InsertOneAsync(new BsonDocument
                    {
                        { "_id", migration.Id },
                        { "ExecutedAt", DateTime.UtcNow }
                    });
                }

                appliedMigrationIds.Add(migration.Id);
                appliedCount++;
                Console.WriteLine($"Completed migration {migration.Id}.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"FAILED migration {migration.Id}: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        Console.WriteLine($"Migrations summary: {appliedCount} applied, {skippedCount} already applied, {withheldBackfillCount} data backfills withheld.");
        Console.WriteLine("All migrations completed.");
    }
}

public enum MigrationTarget
{
    Development,
    Uat,
    Production
}
