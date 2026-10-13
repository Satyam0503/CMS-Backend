using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Repairs legacy WFH dates serialized as India-local midnight (18:30 UTC on
/// the prior date).  Only WFH request-owned rows are changed, and a row is
/// never moved onto an already occupied attendance date.
/// </summary>
public sealed class NormalizeLegacyWfhAttendanceBusinessDates : IMigration
{
    public string Id => $"2026-08-05-01-{typeof(NormalizeLegacyWfhAttendanceBusinessDates).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var requests = db.GetCollection<BsonDocument>("WorkFromHomeRequest");
        var attendance = db.GetCollection<BsonDocument>("Attendance");
        var legacyRequests = await requests.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();

        foreach (var request in legacyRequests)
        {
            if (!TryLegacyLocalMidnight(request, "FromDate", out var from) ||
                !TryLegacyLocalMidnight(request, "ToDate", out var to) ||
                !request.TryGetValue("RequestId", out var requestId) || !requestId.IsString ||
                !request.TryGetValue("CompanyId", out var companyId) || !companyId.IsString ||
                !request.TryGetValue("UserId", out var userId) || !userId.IsString)
                continue;

            var correctedFrom = BusinessDateUtc(from.ToUniversalTime().Date.AddDays(1));
            var correctedTo = BusinessDateUtc(to.ToUniversalTime().Date.AddDays(1));
            await requests.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", request["_id"]),
                Builders<BsonDocument>.Update.Set("FromDate", correctedFrom).Set("ToDate", correctedTo));

            var sourceRows = await attendance.Find(
                Builders<BsonDocument>.Filter.Eq("CompanyId", companyId.AsString) &
                Builders<BsonDocument>.Filter.Eq("UserId", userId.AsString) &
                Builders<BsonDocument>.Filter.Eq("SourceType", "WFH_REQUEST") &
                Builders<BsonDocument>.Filter.Eq("SourceId", requestId.AsString)).ToListAsync();

            foreach (var row in sourceRows)
            {
                if (!TryLegacyLocalMidnight(row, "Date", out var rowDate)) continue;
                var correctedDate = BusinessDateUtc(rowDate.ToUniversalTime().Date.AddDays(1));
                var occupied = await attendance.Find(
                    Builders<BsonDocument>.Filter.Eq("CompanyId", companyId.AsString) &
                    Builders<BsonDocument>.Filter.Eq("UserId", userId.AsString) &
                    Builders<BsonDocument>.Filter.Eq("Date", correctedDate) &
                    Builders<BsonDocument>.Filter.Ne("_id", row["_id"])).AnyAsync();
                if (!occupied)
                    await attendance.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", row["_id"]),
                        Builders<BsonDocument>.Update.Set("Date", correctedDate));
            }
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static DateTime BusinessDateUtc(DateTime date) => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

    private static bool TryLegacyLocalMidnight(BsonDocument document, string field, out BsonDateTime value)
    {
        value = null!;
        if (!document.TryGetValue(field, out var raw) || !raw.IsBsonDateTime) return false;
        value = raw.AsBsonDateTime;
        return value.ToUniversalTime().TimeOfDay == TimeSpan.FromHours(18.5);
    }
}
