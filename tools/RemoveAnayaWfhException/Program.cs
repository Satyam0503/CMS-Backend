using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

var employeeCode = args.FirstOrDefault(x => x.StartsWith("--employee=", StringComparison.OrdinalIgnoreCase))?.Split('=', 2)[1] ?? "EMP1011";
var apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
using var config = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "Codeji.CMS.API", "appsettings.json")));
var connection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("mongodb").GetString()!;
var db = new MongoClient(connection).GetDatabase(MongoUrl.Create(connection).DatabaseName);
var exceptions = db.GetCollection<BsonDocument>("AttendancePayrollException");
var filter = Builders<BsonDocument>.Filter.Eq("EmployeeId", employeeCode) & Builders<BsonDocument>.Filter.Eq("ExceptionType", "WFH_INSUFFICIENT_HOURS") & Builders<BsonDocument>.Filter.Eq("Status", "PENDING_REVIEW");
var rows = await exceptions.Find(filter).ToListAsync();
Console.WriteLine($"Found {rows.Count} pending WFH insufficient-hours exception(s) for {employeeCode}.");
foreach (var row in rows) Console.WriteLine($"{row.GetValue("_id", "")} | {row.GetValue("CompanyId", "")} | {row.GetValue("AttendanceDate", "")} | {row.GetValue("LeaveRequestId", "")}");
if (!apply || rows.Count == 0) return;
var deleted = await exceptions.DeleteManyAsync(filter);
Console.WriteLine($"Deleted {deleted.DeletedCount} exception(s) for {employeeCode}.");
