using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class RepairWorkFromHomeOutboxMissingRequestId : IMigration
{
    public string Id => $"2026-10-27-A-{nameof(RepairWorkFromHomeOutboxMissingRequestId)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var outboxColl = db.GetCollection<BsonDocument>("WorkFromHomeOutboxEvent");
        var requestsColl = db.GetCollection<BsonDocument>("WorkFromHomeRequest");

        var missingFilter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Eq("RequestId", ""),
            Builders<BsonDocument>.Filter.Eq("RequestId", BsonNull.Value),
            Builders<BsonDocument>.Filter.Exists("RequestId", false)
        );

        var stuckEvents = await outboxColl.Find(missingFilter).ToListAsync();

        foreach (var evt in stuckEvents)
        {
            var eventId = evt.GetValue("_id", "").AsString;
            var companyId = evt.GetValue("CompanyId", "").AsString;
            var actorUserId = evt.GetValue("ActorUserId", "").AsString;
            var createdDate = evt.Contains("CreatedDate") && !evt["CreatedDate"].IsBsonNull
                ? evt["CreatedDate"].ToUniversalTime()
                : (DateTime?)null;

            string? resolvedRequestId = null;

            // 1. Try DedupeKey
            var dedupeKey = evt.GetValue("DedupeKey", "").AsString;
            var parts = dedupeKey.Split(':');
            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                resolvedRequestId = parts[1];
            }

            // 2. Try matching WorkFromHomeRequest by company, actor, and timestamp window
            if (string.IsNullOrWhiteSpace(resolvedRequestId) && createdDate.HasValue && !string.IsNullOrWhiteSpace(companyId))
            {
                var windowStart = createdDate.Value.AddMinutes(-10);
                var windowEnd = createdDate.Value.AddMinutes(10);

                var reqFilter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("CompanyId", companyId),
                    Builders<BsonDocument>.Filter.Or(
                        Builders<BsonDocument>.Filter.Eq("UserId", actorUserId),
                        Builders<BsonDocument>.Filter.Eq("CreatedBy", actorUserId)
                    ),
                    Builders<BsonDocument>.Filter.Gte("CreatedDate", windowStart),
                    Builders<BsonDocument>.Filter.Lte("CreatedDate", windowEnd)
                );

                var matchingReq = await requestsColl.Find(reqFilter).FirstOrDefaultAsync();
                if (matchingReq is not null)
                {
                    resolvedRequestId = matchingReq.GetValue("_id", "").AsString;
                }
            }

            if (!string.IsNullOrWhiteSpace(resolvedRequestId))
            {
                var updatedDedupeKey = dedupeKey.Replace("::", $":{resolvedRequestId}:");
                var update = Builders<BsonDocument>.Update
                    .Set("RequestId", resolvedRequestId)
                    .Set("DedupeKey", updatedDedupeKey)
                    .Set("Status", "Pending")
                    .Set("AttemptCount", 0)
                    .Set("AvailableAtUtc", DateTime.UtcNow)
                    .Set("LastError", BsonNull.Value);

                await outboxColl.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", eventId), update);
            }
            else
            {
                var update = Builders<BsonDocument>.Update
                    .Set("Status", "DeadLetter")
                    .Set("CompletedAtUtc", DateTime.UtcNow)
                    .Set("LastError", "Missing tenant or request identity");

                await outboxColl.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", eventId), update);
            }
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
