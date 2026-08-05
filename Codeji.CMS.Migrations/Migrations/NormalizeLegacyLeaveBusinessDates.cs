using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Converts legacy India-local-midnight leave timestamps (18:30 UTC on the
/// previous day) to the UTC-midnight business-date contract. Single-day
/// source-owned attendance is aligned only where the target date is free.
/// </summary>
public sealed class NormalizeLegacyLeaveBusinessDates : IMigration
{
    public string Id => $"2026-07-31-02-{typeof(NormalizeLegacyLeaveBusinessDates).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var leaves = db.GetCollection<BsonDocument>("LeaveRequest");
        var attendance = db.GetCollection<BsonDocument>("Attendance");
        var legacyLeaves = await leaves.Find(Builders<BsonDocument>.Filter.Eq("Status", 2)).ToListAsync();

        foreach (var leave in legacyLeaves)
        {
            if (!TryLegacyLocalMidnight(leave, "StartDate", out var start) ||
                !TryLegacyLocalMidnight(leave, "EndDate", out var end)) continue;

            var correctedStart = DateTime.SpecifyKind(start.ToUniversalTime().Date.AddDays(1), DateTimeKind.Utc);
            var correctedEnd = DateTime.SpecifyKind(end.ToUniversalTime().Date.AddDays(1), DateTimeKind.Utc);
            await leaves.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", leave["_id"]),
                Builders<BsonDocument>.Update.Set("StartDate", correctedStart).Set("EndDate", correctedEnd));

            if (correctedStart != correctedEnd ||
                !leave.TryGetValue("LeaveRequestId", out var requestId) || !requestId.IsString ||
                !leave.TryGetValue("CompanyId", out var companyId) || !companyId.IsString ||
                !leave.TryGetValue("EmployeeId", out var userId) || !userId.IsString)
                continue;

            var sourceRows = await attendance.Find(Builders<BsonDocument>.Filter.Eq("CompanyId", companyId.AsString) &
                                                   Builders<BsonDocument>.Filter.Eq("UserId", userId.AsString) &
                                                   Builders<BsonDocument>.Filter.Eq("SourceType", "LEAVE") &
                                                   Builders<BsonDocument>.Filter.Eq("SourceId", requestId.AsString)).ToListAsync();
            foreach (var row in sourceRows)
            {
                var occupied = await attendance.Find(Builders<BsonDocument>.Filter.Eq("UserId", userId.AsString) &
                                                    Builders<BsonDocument>.Filter.Eq("Date", correctedStart) &
                                                    Builders<BsonDocument>.Filter.Ne("_id", row["_id"]))
                    .AnyAsync();
                if (!occupied)
                    await attendance.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", row["_id"]), Builders<BsonDocument>.Update.Set("Date", correctedStart));
            }
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static bool TryLegacyLocalMidnight(BsonDocument document, string field, out BsonDateTime value)
    {
        value = null!;
        if (!document.TryGetValue(field, out var raw) || !raw.IsBsonDateTime) return false;
        value = raw.AsBsonDateTime;
        return value.ToUniversalTime().TimeOfDay == TimeSpan.FromHours(18.5);
    }
}
