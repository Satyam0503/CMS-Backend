using System.Text.RegularExpressions;
using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddCompanyCareerSlugs : IMigration
{
    public string Id => $"2026-07-23-02-{typeof(AddCompanyCareerSlugs).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var companies = db.GetCollection<BsonDocument>("Company");
        var rows = await companies.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.OrderBy(x => x.GetValue("CreatedDate", BsonNull.Value).ToString()))
        {
            string existing = row.GetValue("CareerSlug", "").ToString();
            string companyName = row.GetValue("CompanyName", "company").ToString();
            string baseSlug = Normalize(existing.Length > 0 ? existing : companyName);

            if (string.IsNullOrEmpty(baseSlug)) baseSlug = "company";
            string slug = baseSlug;
            for (int suffix = 2; used.Contains(slug); suffix++) slug = $"{baseSlug}-{suffix}";
            used.Add(slug);

            await companies.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", row["_id"]),
                Builders<BsonDocument>.Update
                    .Set("CareerSlug", slug)
                    .Set("CareerPortalEnabled", true));
        }

        var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("CareerSlug");
        await companies.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            indexKeys,
            new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_company_career_slug",
                // Every company receives a slug above. A normal unique index is compatible
                // with older MongoDB servers, unlike a partial `$ne` expression.
                Unique = true
            }));

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static string Normalize(string value) =>
        Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
}
