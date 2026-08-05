using Codeji.CMS.Migrations.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Removes the retired Balance and UsedBalance fields only after the canonical
/// TotalAllocated, Taken and Remaining values are present on every record.
/// </summary>
public sealed class RemoveLegacyEmployeeLeaveBalanceFields : IMigration
{
    public string Id => $"2026-08-04-{nameof(RemoveLegacyEmployeeLeaveBalanceFields)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<BsonDocument>("Migration");
        if (await migrations.Find(Builders<BsonDocument>.Filter.Eq("_id", Id)).AnyAsync()) return;

        var balances = db.GetCollection<BsonDocument>("EmployeeLeaveBalance");
        var missingCanonicalFields = await balances.CountDocumentsAsync(Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("TotalAllocated", false),
            Builders<BsonDocument>.Filter.Exists("Taken", false),
            Builders<BsonDocument>.Filter.Exists("Remaining", false)));
        if (missingCanonicalFields > 0)
            throw new InvalidOperationException($"Cannot remove legacy leave fields while {missingCanonicalFields} balance record(s) are missing canonical allocation fields.");

        var result = await balances.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Exists("Balance", true),
                Builders<BsonDocument>.Filter.Exists("UsedBalance", true)),
            Builders<BsonDocument>.Update.Unset("Balance").Unset("UsedBalance"));
        Console.WriteLine($"{{\"migration\":\"{Id}\",\"updated\":{result.ModifiedCount}}}");
        await migrations.InsertOneAsync(new BsonDocument { { "_id", Id }, { "ExecutedAt", DateTime.UtcNow } });
    }
}
