using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

const string anchorEmail = "satyam@codeji.in";
var apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
var month = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
var monthEnd = month.AddMonths(1);
var supportedTypes = new HashSet<string>(StringComparer.Ordinal)
{
    "MISSING_ATTENDANCE", "MISSING_CHECKOUT", "WFH_INSUFFICIENT_HOURS"
};

var configPath = Path.Combine(Directory.GetCurrentDirectory(), "Codeji.CMS.API", "appsettings.json");
using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
var connection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("mongodb").GetString()
    ?? throw new InvalidOperationException("Mongo connection is missing.");
var database = new MongoClient(connection).GetDatabase(MongoUrl.Create(connection).DatabaseName);
var users = database.GetCollection<BsonDocument>("EmpUser");
var anchor = await users.Find(new BsonDocument("Email", new BsonRegularExpression($"^\\s*{System.Text.RegularExpressions.Regex.Escape(anchorEmail)}\\s*$", "i"))).FirstOrDefaultAsync()
    ?? throw new InvalidOperationException($"No company administrator was found for {anchorEmail}.");
var companyId = anchor["CompanyId"].AsString;
var actorUserId = anchor.TryGetValue("UserId", out var actor) ? actor.ToString() : anchor["_id"].ToString();

var exceptions = database.GetCollection<BsonDocument>("AttendancePayrollException");
var filter = new BsonDocument
{
    { "CompanyId", companyId },
    { "PayrollMonth", month },
    { "Status", "PENDING_REVIEW" },
    { "Severity", "BLOCKING" }
};
var pending = await exceptions.Find(filter).ToListAsync();
var companyMonthExceptions = await exceptions.Find(new BsonDocument { { "CompanyId", companyId }, { "PayrollMonth", month } }).ToListAsync();
if (pending.Count == 0)
{
    Console.WriteLine($"Stored July exception rows for this tenant: {companyMonthExceptions.Count}.");
    foreach (var group in companyMonthExceptions.GroupBy(x => $"{x.GetValue("Status", "").AsString}/{x.GetValue("ExceptionType", "").AsString}").OrderBy(x => x.Key))
        Console.WriteLine($"- {group.Key}: {group.Count()}");
}
var unsupported = pending.Where(x => !supportedTypes.Contains(x.GetValue("ExceptionType", "").AsString)).ToList();
if (unsupported.Count > 0)
    throw new InvalidOperationException($"Refusing to change unsupported blocking exception(s): {string.Join(", ", unsupported.Select(x => x.GetValue("ExceptionType", "UNKNOWN").AsString).Distinct())}.");

var fixes = pending.SelectMany(exception =>
{
    var dates = exception.TryGetValue("AffectedDates", out var affected) && affected.IsBsonArray
        ? affected.AsBsonArray.Where(x => x.IsValidDateTime).Select(x => x.ToUniversalTime().Date)
        : exception.TryGetValue("AttendanceDate", out var oneDate) && oneDate.IsValidDateTime
            ? [oneDate.ToUniversalTime().Date]
            : Enumerable.Empty<DateTime>();
    return dates.Select(date => new
    {
        Exception = exception,
        Date = date,
        UserId = exception["UserId"].AsString,
        EmployeeId = exception["EmployeeId"].AsString
    });
}).DistinctBy(x => (x.UserId, x.Date)).ToList();

Console.WriteLine($"Company: {companyId}; month: {month:MMMM yyyy}; pending blocking exceptions: {pending.Count}; working days to mark Present: {fixes.Count}.");
foreach (var item in pending)
    Console.WriteLine($"- {item.GetValue("EmployeeId", "").AsString}: {item.GetValue("ExceptionType", "").AsString} ({item.GetValue("AffectedDates", new BsonArray()).AsBsonArray.Count} date(s))");
if (!apply)
{
    Console.WriteLine("Dry run only. Re-run with --apply after reviewing this exact scope.");
    return;
}

var attendance = database.GetCollection<BsonDocument>("Attendance");
var now = DateTime.UtcNow;
foreach (var item in fixes)
{
    var start = DateTime.SpecifyKind(item.Date, DateTimeKind.Utc);
    var end = start.AddDays(1);
    var existing = await attendance.Find(new BsonDocument { { "CompanyId", companyId }, { "UserId", item.UserId }, { "Date", new BsonDocument { { "$gte", start }, { "$lt", end } } } }).ToListAsync();
    var update = Builders<BsonDocument>.Update
        .Set("EmployeeId", item.EmployeeId).Set("Status", "P")
        .Set("CheckInTime", start.AddHours(9)).Set("CheckOutTime", start.AddHours(18))
        .Set("TotalHours", 9m).Set("BreakMinutes", 0)
        .Set("SourceType", "HR_PAYROLL_RESOLUTION").Unset("SourceId")
        .Set("RemarkCode", "PAYROLL_EXCEPTION_RESOLVED")
        .Set("Remarks", "Marked Present by HR/Admin to resolve July payroll attendance exception.")
        .Set("ModifiedByUserId", actorUserId).Set("ModifiedAtUtc", now);
    if (existing.Count == 0)
    {
        await attendance.InsertOneAsync(new BsonDocument
        {
            { "CompanyId", companyId }, { "UserId", item.UserId }, { "EmployeeId", item.EmployeeId }, { "Date", start }, { "Status", "P" },
            { "CheckInTime", start.AddHours(9) }, { "CheckOutTime", start.AddHours(18) }, { "TotalHours", 9m }, { "BreakMinutes", 0 },
            { "SourceType", "HR_PAYROLL_RESOLUTION" }, { "RemarkCode", "PAYROLL_EXCEPTION_RESOLVED" },
            { "Remarks", "Marked Present by HR/Admin to resolve July payroll attendance exception." }, { "MarkedByUserId", actorUserId }, { "MarkedAtUtc", now }
        });
    }
    else
    {
        await attendance.UpdateOneAsync(new BsonDocument("_id", existing[0]["_id"]), update);
        foreach (var duplicate in existing.Skip(1)) await attendance.DeleteOneAsync(new BsonDocument("_id", duplicate["_id"]));
    }
}

foreach (var exception in pending)
{
    await exceptions.UpdateOneAsync(new BsonDocument("_id", exception["_id"]), Builders<BsonDocument>.Update
        .Set("Status", "RESOLVED")
        .Set("Resolution", "Underlying attendance was marked Present by HR/Admin for July payroll processing.")
        .Set("ReviewedBy", actorUserId).Set("ReviewedAt", now).Set("UpdatedBy", actorUserId).Set("UpdatedDate", now)
        .Inc("Version", 1));
}

var remaining = await exceptions.CountDocumentsAsync(filter);
Console.WriteLine($"Marked {fixes.Count} working day(s) Present and resolved {pending.Count} exception(s). Remaining blocking exceptions: {remaining}.");
