using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Utility.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// One-time June 2026 data preparation for the company containing the E2E test
/// employees. It is intentionally scoped and must not be used as a production
/// attendance auto-fill mechanism.
/// </summary>
public class CompleteJune2026TestCompanyAttendance : IMigration
{
    public string Id => $"2026-07-21-06-{typeof(CompleteJune2026TestCompanyAttendance).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var employeesCollection = db.GetCollection<EmpUser>("EmpUser");
        var testIds = Enumerable.Range(1, 5).Select(x => $"E2E2026-{x:000}").ToArray();
        var testUsers = await employeesCollection.Find(
            Builders<EmpUser>.Filter.In(x => x.EmployeeId, testIds)).ToListAsync();
        var companyIds = testUsers.Select(x => x.CompanyId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (companyIds.Count != 1)
            throw new InvalidOperationException("Could not uniquely identify the E2E test company.");

        var companyId = companyIds[0];
        var monthStart = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        var employees = (await employeesCollection.Find(x =>
                x.CompanyId == companyId && x.Status && !x.IsDeleted).ToListAsync())
            .Where(x => !string.IsNullOrWhiteSpace(x.EmployeeId) && !string.IsNullOrWhiteSpace(x.UserId))
            .GroupBy(x => x.EmployeeId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.OrderByDescending(e => e.UpdatedDate ?? e.CreatedDate).First())
            .ToList();

        var weeklyOffCollection = db.GetCollection<WeeklyOffSetting>("WeeklyOffSetting");
        var weeklyOff = await weeklyOffCollection.Find(x => x.CompanyId == companyId).FirstOrDefaultAsync();
        var offDays = (weeklyOff?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday]).ToHashSet();
        var calendar = db.GetCollection<CalendarEntity>("CalendarEntity");
        var holidays = await calendar.Find(x =>
            x.CompanyId == companyId && x.Type == EnumsHelper.CalendarItem.Holiday &&
            (x.Recurring || (x.Date >= monthStart && x.Date < monthEnd))).ToListAsync();
        bool IsHoliday(DateTime date) => holidays.Any(x =>
            x.Recurring ? x.Date.Month == date.Month && x.Date.Day == date.Day : x.Date.Date == date.Date);

        var attendance = db.GetCollection<BsonDocument>("Attendance");
        var employeeIds = employees.Select(x => x.EmployeeId).ToArray();
        var companyAttendance = await attendance.Find(
            Builders<BsonDocument>.Filter.In("EmployeeId", employeeIds) &
            Builders<BsonDocument>.Filter.Gte("Date", monthStart) &
            Builders<BsonDocument>.Filter.Lt("Date", monthEnd)).ToListAsync();

        // Consolidate cross-user duplicates by the payroll identity EmployeeId + Date.
        foreach (var group in companyAttendance.GroupBy(x => new
                 {
                     EmployeeId = x.GetValue("EmployeeId", "").AsString,
                     Date = x["Date"].ToUniversalTime().Date,
                 }).Where(x => x.Count() > 1))
        {
            var documents = group.ToList();
            var keeper = documents.OrderByDescending(CompletenessScore)
                .ThenByDescending(x => x["_id"].AsObjectId.CreationTime).First();
            foreach (var field in new[] { "UserId", "Status", "CheckInTime", "CheckOutTime", "TotalHours", "Remarks" })
            {
                if (HasValue(keeper, field)) continue;
                var source = documents.FirstOrDefault(x => HasValue(x, field));
                if (source != null) keeper[field] = source[field];
            }
            await attendance.ReplaceOneAsync(Builders<BsonDocument>.Filter.Eq("_id", keeper["_id"]), keeper);
            await attendance.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id",
                documents.Where(x => x["_id"] != keeper["_id"]).Select(x => x["_id"])));
        }

        // Reload after consolidation, repair required times, then fill missing workdays.
        companyAttendance = await attendance.Find(
            Builders<BsonDocument>.Filter.In("EmployeeId", employeeIds) &
            Builders<BsonDocument>.Filter.Gte("Date", monthStart) &
            Builders<BsonDocument>.Filter.Lt("Date", monthEnd)).ToListAsync();
        var timeStatuses = new HashSet<string>(["P", "WFH", "HD", "ED", "LHD", "WFH+WFO", "WFH-HD"]);

        foreach (var record in companyAttendance.Where(x =>
                     timeStatuses.Contains(x.GetValue("Status", "").AsString) && !HasValue(x, "CheckOutTime")))
        {
            var date = record["Date"].ToUniversalTime().Date;
            var status = record.GetValue("Status", "P").AsString;
            var checkIn = HasValue(record, "CheckInTime") ? record["CheckInTime"].ToUniversalTime() : date.AddHours(status == "LHD" ? 11 : 9);
            var duration = status is "HD" or "LHD" or "WFH-HD" ? 4 : status == "ED" ? 7 : 9;
            await attendance.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", record["_id"]),
                Builders<BsonDocument>.Update.Set("CheckInTime", checkIn)
                    .Set("CheckOutTime", checkIn.AddHours(duration))
                    .Set("TotalHours", (decimal)Math.Max(duration - (duration > 4 ? 1 : 0), 0)));
        }

        foreach (var employee in employees)
        {
            var from = monthStart;
            if (DateTime.TryParse(employee.DateOfJoining, out var joining) && joining.Date > from.Date)
                from = DateTime.SpecifyKind(joining.Date, DateTimeKind.Utc);
            var to = monthEnd.AddDays(-1);
            if (DateTime.TryParse(employee.ExitDate, out var exit) && exit.Date < to.Date)
                to = DateTime.SpecifyKind(exit.Date, DateTimeKind.Utc);
            if (from > to) continue;

            var recordedDates = (await attendance.Find(
                    Builders<BsonDocument>.Filter.Eq("EmployeeId", employee.EmployeeId) &
                    Builders<BsonDocument>.Filter.Gte("Date", from) &
                    Builders<BsonDocument>.Filter.Lte("Date", to)).ToListAsync())
                .Select(x => x["Date"].ToUniversalTime().Date).ToHashSet();

            for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
            {
                if (offDays.Contains((int)date.DayOfWeek) || IsHoliday(date) || recordedDates.Contains(date)) continue;
                await attendance.InsertOneAsync(new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId() },
                    { "CompanyId", companyId },
                    { "UserId", employee.UserId },
                    { "EmployeeId", employee.EmployeeId },
                    { "Date", DateTime.SpecifyKind(date, DateTimeKind.Utc) },
                    { "CheckInTime", DateTime.SpecifyKind(date.AddHours(9), DateTimeKind.Utc) },
                    { "CheckOutTime", DateTime.SpecifyKind(date.AddHours(18), DateTimeKind.Utc) },
                    { "TotalHours", 8m },
                    { "Status", "P" },
                    { "Remarks", "June 2026 payroll test attendance" },
                    { "LateCount", 0 },
                    { "EarlyExitCount", 0 },
                });
            }
        }

        var exceptions = db.GetCollection<BsonDocument>("AttendancePayrollException");
        await exceptions.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Eq("CompanyId", companyId) &
            Builders<BsonDocument>.Filter.Eq("PayrollMonth", monthStart) &
            Builders<BsonDocument>.Filter.In("ExceptionType", new[] { "MISSING_ATTENDANCE", "MISSING_CHECKOUT", "DUPLICATE_ATTENDANCE" }) &
            Builders<BsonDocument>.Filter.Eq("Status", "PENDING_REVIEW"),
            Builders<BsonDocument>.Update.Set("Status", "RESOLVED")
                .Set("Resolution", "June 2026 test attendance data completed and consolidated.")
                .Set("UpdatedDate", DateTime.UtcNow));

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static int CompletenessScore(BsonDocument document) =>
        (HasValue(document, "Status") ? 1 : 0) +
        (HasValue(document, "CheckInTime") ? 2 : 0) +
        (HasValue(document, "CheckOutTime") ? 4 : 0) +
        (HasValue(document, "TotalHours") ? 1 : 0);

    private static bool HasValue(BsonDocument document, string field) =>
        document.TryGetValue(field, out var value) && !value.IsBsonNull &&
        (!value.IsString || !string.IsNullOrWhiteSpace(value.AsString));
}
