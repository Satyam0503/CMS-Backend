using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

var configPath = @"d:\CMS\CMS-Backend-Core\Codeji.CMS.API\appsettings.json";
using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
var connection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("mongodb").GetString();
var db = new MongoClient(connection).GetDatabase(MongoUrl.Create(connection).DatabaseName);

var companyId = "b125eefc-153e-48d8-8b68-5501770e3ff2";
var aaravUserId = "0e94eb40-53ee-42ea-8598-f3c8edf02863";

var dateUtc = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc);

var wfhCol = db.GetCollection<BsonDocument>("WorkFromHomeRequest");
var attCol = db.GetCollection<BsonDocument>("Attendance");
var segCol = db.GetCollection<BsonDocument>("AttendanceDaySegment");

var wfhs = await wfhCol.Find(new BsonDocument { { "CompanyId", companyId }, { "UserId", aaravUserId }, { "FromDate", dateUtc } }).ToListAsync();
Console.WriteLine($"WFH count on 2026-10-28: {wfhs.Count}");
foreach (var w in wfhs)
{
    Console.WriteLine($"WFH: Id={w["_id"]}, Duration={w.GetValue("DurationType", "")}, Status={w.GetValue("Status", "")}");
    if (w.Contains("OfficeConversions"))
    {
        Console.WriteLine($"  OfficeConversions: {w["OfficeConversions"].AsBsonArray.Count}");
    }
}

var atts = await attCol.Find(new BsonDocument { { "CompanyId", companyId }, { "UserId", aaravUserId }, { "Date", dateUtc } }).ToListAsync();
Console.WriteLine($"Attendance count on 2026-10-28: {atts.Count}");
foreach (var a in atts)
{
    Console.WriteLine($"Att: Status={a.GetValue("Status", "")}, SourceType={a.GetValue("SourceType", "")}, CheckIn={a.GetValue("CheckInTime", BsonNull.Value)}, CheckOut={a.GetValue("CheckOutTime", BsonNull.Value)}");
}

var segs = await segCol.Find(new BsonDocument { { "CompanyId", companyId }, { "UserId", aaravUserId }, { "Date", dateUtc } }).ToListAsync();
Console.WriteLine($"Segments count on 2026-10-28: {segs.Count}");
foreach (var s in segs)
{
    Console.WriteLine($"Seg: Segment={s.GetValue("Segment", "")}, Status={s.GetValue("Status", "")}, SourceType={s.GetValue("SourceType", "")}, CheckIn={s.GetValue("CheckInTime", BsonNull.Value)}, CheckOut={s.GetValue("CheckOutTime", BsonNull.Value)}");
}
