using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Repairs only the five June 2026 E2E payroll-test employees created for the
/// attendance/payroll test flow. Production employee attendance is untouched.
/// </summary>
public class RepairJune2026TestAttendanceTimes : IMigration
{
    public string Id => $"2026-07-21-03-{typeof(RepairJune2026TestAttendanceTimes).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var attendance = db.GetCollection<BsonDocument>("Attendance");
        var monthStart = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var employeeIds = Enumerable.Range(1, 5).Select(x => $"E2E2026-{x:000}").ToArray();
        var timeStatuses = new[] { "P", "WFH", "HD", "ED", "LHD", "WFH+WFO", "WFH-HD" };

        var filter = Builders<BsonDocument>.Filter.In("EmployeeId", employeeIds) &
                     Builders<BsonDocument>.Filter.Gte("Date", monthStart) &
                     Builders<BsonDocument>.Filter.Lt("Date", monthEnd) &
                     Builders<BsonDocument>.Filter.In("Status", timeStatuses) &
                     (Builders<BsonDocument>.Filter.Exists("CheckOutTime", false) |
                      Builders<BsonDocument>.Filter.Eq("CheckOutTime", BsonNull.Value));

        var records = await attendance.Find(filter).ToListAsync();
        foreach (var record in records)
        {
            var date = record["Date"].ToUniversalTime().Date;
            var status = record.GetValue("Status", "P").AsString;
            var checkIn = record.TryGetValue("CheckInTime", out var storedCheckIn) && !storedCheckIn.IsBsonNull
                ? storedCheckIn.ToUniversalTime()
                : date.AddHours(status == "LHD" ? 11 : 9);
            var hours = status switch
            {
                "HD" or "LHD" or "WFH-HD" => 4,
                "ED" => 7,
                _ => 9,
            };
            var checkOut = checkIn.AddHours(hours);
            var totalHours = Math.Max((checkOut - checkIn).TotalHours - (hours > 4 ? 1 : 0), 0);

            await attendance.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", record["_id"]),
                Builders<BsonDocument>.Update
                    .Set("CheckInTime", checkIn)
                    .Set("CheckOutTime", checkOut)
                    .Set("TotalHours", (decimal)totalHours));
        }

        // The next UI recalculation will also resolve these, but resolving here
        // makes the one-time data repair immediately consistent.
        var exceptions = db.GetCollection<BsonDocument>("AttendancePayrollException");
        foreach (var employeeId in employeeIds)
        {
            var stillMissing = await attendance.Find(
                Builders<BsonDocument>.Filter.Eq("EmployeeId", employeeId) &
                Builders<BsonDocument>.Filter.Gte("Date", monthStart) &
                Builders<BsonDocument>.Filter.Lt("Date", monthEnd) &
                Builders<BsonDocument>.Filter.In("Status", timeStatuses) &
                (Builders<BsonDocument>.Filter.Exists("CheckOutTime", false) |
                 Builders<BsonDocument>.Filter.Eq("CheckOutTime", BsonNull.Value))).AnyAsync();

            if (!stillMissing)
            {
                await exceptions.UpdateManyAsync(
                    Builders<BsonDocument>.Filter.Eq("EmployeeId", employeeId) &
                    Builders<BsonDocument>.Filter.Eq("PayrollMonth", monthStart) &
                    Builders<BsonDocument>.Filter.Eq("ExceptionType", "MISSING_CHECKOUT") &
                    Builders<BsonDocument>.Filter.Eq("Status", "PENDING_REVIEW"),
                    Builders<BsonDocument>.Update
                        .Set("Status", "RESOLVED")
                        .Set("Resolution", "Missing test checkout repaired.")
                        .Set("UpdatedDate", DateTime.UtcNow));
            }
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
