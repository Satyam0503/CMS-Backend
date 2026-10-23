using MongoDB.Bson;
using MongoDB.Driver;

var configPath = @"d:\CMS\CMS-Backend-Core\Codeji.CMS.API\appsettings.json";
using var config = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
var connection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("mongodb").GetString();
var db = new MongoClient(connection).GetDatabase(MongoUrl.Create(connection).DatabaseName);

var applicants = db.GetCollection<BsonDocument>("Applicant");
var vacancies = db.GetCollection<BsonDocument>("JobVacancy");

var app = await applicants.Find(new BsonDocument("_id", "4c5adde3-7ba3-499d-91f2-9c850fa083e0")).FirstOrDefaultAsync();
if (app != null)
{
    var fn = app.Contains("FirstName") ? app["FirstName"].AsString : "";
    var ln = app.Contains("LastName") ? app["LastName"].AsString : "";
    var vacId = app.Contains("VacancyId") ? app["VacancyId"].AsString : "";
    Console.WriteLine($"Applicant: {fn} {ln}, VacancyId: {vacId}");

    var vac = await vacancies.Find(new BsonDocument { { "$or", new BsonArray { new BsonDocument("_id", vacId), new BsonDocument("JobId", vacId), new BsonDocument("PublicJobId", vacId) } } }).FirstOrDefaultAsync();
    if (vac != null)
    {
        var title = vac.Contains("Title") ? vac["Title"].AsString : "";
        var jid = vac.Contains("JobId") ? vac["JobId"].ToString() : (vac.Contains("_id") ? vac["_id"].ToString() : "");
        var passScore = vac.Contains("ScreeningPassScore") ? vac["ScreeningPassScore"].ToString() : "NOT PRESENT";
        var criteria = vac.Contains("ScreeningCriteria") ? vac["ScreeningCriteria"].ToString() : "NOT PRESENT";
        Console.WriteLine($"Vacancy found: Title={title}, Id={jid}, ScreeningPassScore={passScore}, ScreeningCriteria={criteria}");
    }
    else
    {
        Console.WriteLine($"Vacancy NOT found for id: {vacId}");
    }
}
else
{
    Console.WriteLine("Applicant 4c5adde3-7ba3-499d-91f2-9c850fa083e0 not found by _id!");
}
