using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Removes cross-user duplicate attendance for the five June 2026 E2E test
/// employees. Records are grouped by EmployeeId + calendar date and the most
/// complete record is retained.
/// </summary>
public class RemoveJune2026E2EDuplicateAttendance : IMigration
{
    public string Id => $"2026-07-21-04-{typeof(RemoveJune2026E2EDuplicateAttendance).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var employeeIds = Enumerable.Range(1, 5).Select(x => $"E2E2026-{x:000}").ToArray();
        var monthStart = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var attendance = db.GetCollection<BsonDocument>("Attendance");

        var records = await attendance.Find(
            Builders<BsonDocument>.Filter.In("EmployeeId", employeeIds) &
            Builders<BsonDocument>.Filter.Gte("Date", monthStart) &
            Builders<BsonDocument>.Filter.Lt("Date", monthEnd)).ToListAsync();

        var duplicateGroups = records
            .GroupBy(x => new
            {
                EmployeeId = x.GetValue("EmployeeId", "").AsString,
                Date = x["Date"].ToUniversalTime().Date,
            })
            .Where(x => x.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups)
        {
            var documents = group.ToList();
            var keeper = documents
                .OrderByDescending(CompletenessScore)
                .ThenByDescending(x => x["_id"].AsObjectId.CreationTime)
                .First();

            foreach (var field in new[] { "UserId", "Status", "CheckInTime", "CheckOutTime", "TotalHours", "Remarks" })
            {
                if (HasValue(keeper, field)) continue;
                var source = documents.FirstOrDefault(x => HasValue(x, field));
                if (source != null) keeper[field] = source[field];
            }

            await attendance.ReplaceOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", keeper["_id"]), keeper);

            var removeIds = documents
                .Where(x => x["_id"] != keeper["_id"])
                .Select(x => x["_id"])
                .ToArray();
            if (removeIds.Length > 0)
                await attendance.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id", removeIds));
        }

        var exceptions = db.GetCollection<BsonDocument>("AttendancePayrollException");
        foreach (var employeeId in employeeIds)
        {
            var employeeRecords = await attendance.Find(
                Builders<BsonDocument>.Filter.Eq("EmployeeId", employeeId) &
                Builders<BsonDocument>.Filter.Gte("Date", monthStart) &
                Builders<BsonDocument>.Filter.Lt("Date", monthEnd)).ToListAsync();
            var stillDuplicated = employeeRecords.GroupBy(x => x["Date"].ToUniversalTime().Date).Any(x => x.Count() > 1);
            if (!stillDuplicated)
            {
                await exceptions.UpdateManyAsync(
                    Builders<BsonDocument>.Filter.Eq("EmployeeId", employeeId) &
                    Builders<BsonDocument>.Filter.Eq("PayrollMonth", monthStart) &
                    Builders<BsonDocument>.Filter.Eq("ExceptionType", "DUPLICATE_ATTENDANCE") &
                    Builders<BsonDocument>.Filter.Eq("Status", "PENDING_REVIEW"),
                    Builders<BsonDocument>.Update
                        .Set("Status", "RESOLVED")
                        .Set("Resolution", "Duplicate test attendance consolidated.")
                        .Set("UpdatedDate", DateTime.UtcNow));
            }
        }

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
