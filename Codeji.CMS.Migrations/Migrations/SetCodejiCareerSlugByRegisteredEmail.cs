using System.Text.RegularExpressions;
using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class SetCodejiCareerSlugByRegisteredEmail : IMigration
{
    public string Id => $"2026-07-23-03-{typeof(SetCodejiCareerSlugByRegisteredEmail).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var companies = db.GetCollection<BsonDocument>("Company");
        var employees = db.GetCollection<BsonDocument>("EmpUser");

        var registeredUser = await employees.Find(
                Builders<BsonDocument>.Filter.Regex(
                    "Email",
                    new BsonRegularExpression("^satyam@codeji\\.in$", "i")))
            .FirstOrDefaultAsync();

        if (registeredUser is not null)
        {
            string companyId = registeredUser.GetValue("CompanyId", string.Empty).ToString();
            if (!string.IsNullOrWhiteSpace(companyId))
            {
                var company = await companies.Find(
                        Builders<BsonDocument>.Filter.Eq("_id", companyId))
                    .FirstOrDefaultAsync();

                if (company is not null)
                {
                    string currentSlug = company.GetValue("CareerSlug", string.Empty).ToString();
                    string companyName = company.GetValue("CompanyName", "company").ToString();
                    string slug = string.IsNullOrWhiteSpace(currentSlug)
                        ? await BuildUniqueSlug(companies, Normalize(companyName), companyId)
                        : Normalize(currentSlug);

                    await companies.UpdateOneAsync(
                        Builders<BsonDocument>.Filter.Eq("_id", companyId),
                        Builders<BsonDocument>.Update
                            .Set("CareerSlug", slug)
                            .Set("CareerPortalEnabled", true)
                            .Set("UpdatedDate", DateTime.UtcNow));
                }
            }
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static async Task<string> BuildUniqueSlug(
        IMongoCollection<BsonDocument> companies,
        string baseSlug,
        string currentCompanyId)
    {
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "company";

        string slug = baseSlug;
        for (int suffix = 2; await companies.Find(
                Builders<BsonDocument>.Filter.Eq("CareerSlug", slug) &
                Builders<BsonDocument>.Filter.Ne("_id", currentCompanyId))
            .AnyAsync(); suffix++)
        {
            slug = $"{baseSlug}-{suffix}";
        }

        return slug;
    }

    private static string Normalize(string? value) =>
        Regex.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
}
