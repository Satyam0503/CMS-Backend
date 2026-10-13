using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

// Maps only known default titles where exactly one same-company default
// department exists. Custom/ambiguous legacy rows remain unmapped for an
// administrator to decide; no cross-company or guessed relationship is made.
public sealed class MapLegacyCompanyJobTitlesToDepartments : IMigration
{
    public string Id => $"2026-08-12-01-{nameof(MapLegacyCompanyJobTitlesToDepartments)}";

    private static readonly IReadOnlyDictionary<string, string> Mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Software Engineer"] = "Engineering", ["Senior Software Engineer"] = "Engineering",
        ["Team Lead"] = "Engineering", ["Quality Analyst"] = "Engineering", ["UI/UX Designer"] = "Engineering",
        ["Project Manager"] = "Operations", ["Business Analyst"] = "Operations",
        ["HR Executive"] = "Human Resources", ["HR Manager"] = "Human Resources",
        ["Accountant"] = "Finance & Accounts", ["Sales Executive"] = "Sales", ["Marketing Executive"] = "Marketing"
    };

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var departments = db.GetCollection<BsonDocument>("Department");
        var jobTitles = db.GetCollection<BsonDocument>("JobTitles");
        var allDepartments = await departments.Find(Builders<BsonDocument>.Filter.Eq("IsDeleted", false)).ToListAsync();
        var allTitles = await jobTitles.Find(Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Eq("DepartmentId", BsonNull.Value),
            Builders<BsonDocument>.Filter.Exists("DepartmentId", false))).ToListAsync();

        foreach (var title in allTitles)
        {
            var companyId = title.GetValue("CompanyId", "").AsString;
            var titleName = EnglishLabel(title.GetValue("Titles", new BsonArray()).AsBsonArray);
            if (string.IsNullOrWhiteSpace(companyId) || titleName is null || !Mapping.TryGetValue(titleName, out var departmentName)) continue;
            var matches = allDepartments.Where(d => d.GetValue("CompanyId", "").AsString == companyId &&
                d.GetValue("IsActive", true).ToBoolean() && EnglishLabel(d.GetValue("Titles", new BsonArray()).AsBsonArray)?.Equals(departmentName, StringComparison.OrdinalIgnoreCase) == true).ToList();
            if (matches.Count == 1)
                await jobTitles.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", title["_id"]), Builders<BsonDocument>.Update.Set("DepartmentId", matches[0].GetValue("_id").AsString));
        }
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static string? EnglishLabel(BsonArray titles) => titles.OfType<BsonDocument>()
        .FirstOrDefault(x => x.GetValue("Language", "").AsString.Equals("en", StringComparison.OrdinalIgnoreCase))?
        .GetValue("Label", "").AsString;
}
