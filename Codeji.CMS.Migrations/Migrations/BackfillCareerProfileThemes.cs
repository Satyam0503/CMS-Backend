using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>Ensures every existing public profile has an explicit company-owned theme.</summary>
public sealed class BackfillCareerProfileThemes : IMigration
{
    public string Id => $"2026-08-24-01-{nameof(BackfillCareerProfileThemes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var profiles = db.GetCollection<BsonDocument>("PublicCompanyProfile");
        var missingTheme = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists("CareerTheme", false),
            Builders<BsonDocument>.Filter.Eq("CareerTheme", BsonNull.Value),
            Builders<BsonDocument>.Filter.Eq("CareerTheme", string.Empty));
        await profiles.UpdateManyAsync(missingTheme,
            Builders<BsonDocument>.Update.Set("CareerTheme", "glass"));

        var companies = db.GetCollection<BsonDocument>("Company");
        var profileRows = await profiles.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var profileThemes = profileRows
            .Where(row => row.TryGetValue("CompanyId", out _))
            .GroupBy(row => row["CompanyId"].AsString)
            .ToDictionary(
                group => group.Key,
                group => group.Last().TryGetValue("CareerTheme", out var theme) ? theme.ToString() : "glass");
        var companyRows = await companies.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        foreach (var company in companyRows)
        {
            if (company.TryGetValue("AppTheme", out var theme) && !theme.IsBsonNull && !string.IsNullOrWhiteSpace(theme.ToString())) continue;
            var companyId = company.GetValue("_id", BsonNull.Value).ToString();
            var inherited = profileThemes.GetValueOrDefault(companyId, "glass").Trim().ToLowerInvariant();
            if (inherited is not ("glass" or "slate" or "dawn" or "blush")) inherited = "glass";
            await companies.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", company["_id"]),
                Builders<BsonDocument>.Update.Set("AppTheme", inherited));
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }
}
