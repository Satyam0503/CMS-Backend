using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

const string anchorEmail = "satyam@codeji.in";
var apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
var dateArg = args.FirstOrDefault(x => x.StartsWith("--date=", StringComparison.OrdinalIgnoreCase) || x.StartsWith("--date:", StringComparison.OrdinalIgnoreCase));

DateTime start;
DateTime endExclusive;
if (!string.IsNullOrWhiteSpace(dateArg))
{
    var separator = dateArg.Contains('=') ? '=' : ':';
    var value = dateArg.Split(separator, 2)[1];
    if (!DateTime.TryParse(value, out var parsedDate))
        throw new InvalidOperationException($"Invalid date format: {value}. Use YYYY-MM-DD.");

    start = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
    endExclusive = start.AddDays(1);
}
else
{
    start = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    endExclusive = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
}

var configPath = Path.Combine(Directory.GetCurrentDirectory(), "Codeji.CMS.API", "appsettings.json");
using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
var connection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("mongodb").GetString()
    ?? throw new InvalidOperationException("Mongo connection is missing.");
var url = MongoUrl.Create(connection);
var db = new MongoClient(connection).GetDatabase(url.DatabaseName);
var users = db.GetCollection<BsonDocument>("EmpUser");
var anchor = await users.Find(new BsonDocument("Email", new BsonRegularExpression($"^\\s*{System.Text.RegularExpressions.Regex.Escape(anchorEmail)}\\s*$", "i"))).FirstOrDefaultAsync()
    ?? throw new InvalidOperationException($"No user was found for {anchorEmail}.");
var companyId = anchor["CompanyId"].AsString;
var employees = await users.Find(new BsonDocument { { "CompanyId", companyId }, { "Status", true }, { "IsDeleted", new BsonDocument("$ne", true) } }).ToListAsync();
var attendance = db.GetCollection<BsonDocument>("Attendance");
var holidays = db.GetCollection<BsonDocument>("Holidays");
var holidayDates = (await holidays.Find(new BsonDocument { { "CompanyId", companyId }, { "Date", new BsonDocument { { "$gte", start }, { "$lt", endExclusive } } } }).ToListAsync())
    .Where(x => x.TryGetValue("Date", out _))
    .Select(x => x["Date"].ToUniversalTime().Date)
    .ToHashSet();

var candidates = new List<BsonDocument>();
foreach (var employee in employees)
{
    var userId = employee.TryGetValue("UserId", out var userIdValue) ? userIdValue.AsString : employee["_id"].ToString();
    var employeeId = employee.TryGetValue("EmployeeId", out var employeeCode) && !employeeCode.IsBsonNull ? employeeCode.ToString() : string.Empty;

    for (var date = start; date < endExclusive; date = date.AddDays(1))
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidayDates.Contains(date.Date))
            continue;

        var absent = (Math.Abs(HashCode.Combine(userId, date.Day)) % 10) == 0;
        var checkIn = DateTime.SpecifyKind(date.AddHours(8).AddMinutes(30), DateTimeKind.Utc);
        var checkOut = DateTime.SpecifyKind(date.AddHours(19), DateTimeKind.Utc);
        var totalHours = absent ? 0m : 8m;

        candidates.Add(new BsonDocument
        {
            { "UserId", userId },
            { "EmployeeId", employeeId },
            { "CompanyId", companyId },
            { "Date", date },
            { "Status", absent ? "A" : "P" },
            { "CheckInTime", absent ? BsonNull.Value : checkIn },
            { "CheckOutTime", absent ? BsonNull.Value : checkOut },
            { "TotalHours", totalHours },
            { "BreakMinutes", absent ? 0 : 60 },
            { "Remarks", absent ? "Seeded absence" : "Seeded July attendance" },
            { "SourceType", "MANUAL_SEED" },
            { "SourceVersion", 1 },
            { "LateCount", 0 },
            { "EarlyExitCount", 0 }
        });
    }
}

var existing = await attendance.Find(new BsonDocument { { "CompanyId", companyId }, { "Date", new BsonDocument { { "$gte", start }, { "$lt", endExclusive } } } }).ToListAsync();
Console.WriteLine($"Company: {companyId}; active employees: {employees.Count}; current July rows: {existing.Count}; new rows to insert: {candidates.Count}.");
if (!apply)
{
    Console.WriteLine("Dry run only. Re-run with --apply to delete July attendance and insert new rows.");
    return;
}

var deleteResult = await attendance.DeleteManyAsync(new BsonDocument { { "CompanyId", companyId }, { "Date", new BsonDocument { { "$gte", start }, { "$lt", endExclusive } } } });
Console.WriteLine($"Deleted {deleteResult.DeletedCount} existing July attendance rows.");

if (candidates.Count > 0)
{
    await attendance.InsertManyAsync(candidates);
    Console.WriteLine($"Inserted {candidates.Count} new July attendance rows.");
}
else
{
    Console.WriteLine("No attendance rows generated for July.");
}
