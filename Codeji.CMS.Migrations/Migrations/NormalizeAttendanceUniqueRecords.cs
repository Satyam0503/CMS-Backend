using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Consolidates legacy duplicate attendance documents and enforces the same
/// UserId + Date identity used by AttendanceRepository.
/// </summary>
public class NormalizeAttendanceUniqueRecords : IMigration
{
    public string Id => $"2026-07-21-02-{typeof(NormalizeAttendanceUniqueRecords).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var attendance = db.GetCollection<BsonDocument>("Attendance");
        var duplicateGroups = await attendance.Aggregate()
            .Group(new BsonDocument
            {
                { "_id", new BsonDocument { { "UserId", "$UserId" }, { "Date", "$Date" } } },
                { "documents", new BsonDocument("$push", "$$ROOT") },
                { "count", new BsonDocument("$sum", 1) },
            })
            .Match(new BsonDocument("count", new BsonDocument("$gt", 1)))
            .ToListAsync();

        foreach (var group in duplicateGroups)
        {
            var documents = group["documents"].AsBsonArray.Select(x => x.AsBsonDocument).ToList();
            var keeper = documents
                .OrderByDescending(CompletenessScore)
                .ThenByDescending(x => x["_id"].AsObjectId.CreationTime)
                .First();

            // Preserve useful values found on another duplicate without overwriting
            // the more complete record selected above.
            foreach (var field in new[] { "EmployeeId", "Status", "CheckInTime", "CheckOutTime", "TotalHours", "Remarks" })
            {
                if (HasValue(keeper, field)) continue;
                var source = documents.FirstOrDefault(x => HasValue(x, field));
                if (source != null) keeper[field] = source[field];
            }

            await attendance.ReplaceOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", keeper["_id"]),
                keeper);

            var duplicateIds = documents
                .Where(x => x["_id"] != keeper["_id"])
                .Select(x => x["_id"])
                .ToList();
            if (duplicateIds.Count > 0)
                await attendance.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id", duplicateIds));
        }

        var keys = Builders<BsonDocument>.IndexKeys
            .Ascending("UserId")
            .Ascending("Date");
        await attendance.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            keys,
            new CreateIndexOptions { Name = "ux_attendance_user_date", Unique = true }));

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static int CompletenessScore(BsonDocument document)
    {
        var score = 0;
        if (HasValue(document, "Status")) score += 1;
        if (HasValue(document, "CheckInTime")) score += 2;
        if (HasValue(document, "CheckOutTime")) score += 4;
        if (HasValue(document, "TotalHours")) score += 1;
        return score;
    }

    private static bool HasValue(BsonDocument document, string field) =>
        document.TryGetValue(field, out var value) &&
        !value.IsBsonNull &&
        (!value.IsString || !string.IsNullOrWhiteSpace(value.AsString));
}
