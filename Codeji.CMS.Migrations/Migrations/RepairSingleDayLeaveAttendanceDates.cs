using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Repairs legacy, source-owned attendance rows whose stored date does not match
/// the approved date of a single-day leave request. Rows with an existing record
/// on the intended day are deliberately left untouched for manual resolution.
/// </summary>
public sealed class RepairSingleDayLeaveAttendanceDates : IMigration
{
    public string Id => $"2026-07-31-{typeof(RepairSingleDayLeaveAttendanceDates).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var attendance = db.GetCollection<BsonDocument>("Attendance");
        var leaves = db.GetCollection<BsonDocument>("LeaveRequest");
        var sourceRows = await attendance.Find(
            Builders<BsonDocument>.Filter.Eq("SourceType", "LEAVE") &
            Builders<BsonDocument>.Filter.Exists("SourceId", true)).ToListAsync();

        foreach (var row in sourceRows)
        {
            if (!row.TryGetValue("SourceId", out var sourceId) || !sourceId.IsString ||
                !row.TryGetValue("CompanyId", out var companyId) || !companyId.IsString ||
                !row.TryGetValue("UserId", out var userId) || !userId.IsString)
                continue;

            var leave = await leaves.Find(Builders<BsonDocument>.Filter.Eq("LeaveRequestId", sourceId.AsString) &
                                          Builders<BsonDocument>.Filter.Eq("CompanyId", companyId.AsString) &
                                          Builders<BsonDocument>.Filter.Eq("EmployeeId", userId.AsString) &
                                          Builders<BsonDocument>.Filter.Eq("Status", 2))
                .FirstOrDefaultAsync();
            if (leave is null || !leave.TryGetValue("StartDate", out var start) || !start.IsBsonDateTime ||
                !leave.TryGetValue("EndDate", out var end) || !end.IsBsonDateTime)
                continue;

            var startDate = start.ToUniversalTime().Date;
            if (end.ToUniversalTime().Date != startDate || !row.TryGetValue("Date", out var storedDate) ||
                !storedDate.IsBsonDateTime || storedDate.ToUniversalTime().Date == startDate)
                continue;

            var target = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            var occupied = await attendance.Find(Builders<BsonDocument>.Filter.Eq("UserId", userId.AsString) &
                                                Builders<BsonDocument>.Filter.Eq("Date", target) &
                                                Builders<BsonDocument>.Filter.Ne("_id", row["_id"]))
                .AnyAsync();
            if (occupied) continue;

            await attendance.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", row["_id"]),
                Builders<BsonDocument>.Update.Set("Date", target));
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
