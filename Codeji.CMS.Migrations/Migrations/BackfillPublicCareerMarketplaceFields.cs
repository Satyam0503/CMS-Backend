using System.Text.RegularExpressions;
using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class BackfillPublicCareerMarketplaceFields : IMigration
{
    // New id intentionally reruns the backfill for databases that received the
    // earlier alphanumeric public-code implementation.
    public string Id => $"2026-07-24-05-{typeof(BackfillPublicCareerMarketplaceFields).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var companies = db.GetCollection<BsonDocument>("Company");
        var jobs = db.GetCollection<BsonDocument>("JobVacancy");

        await BackfillCompanies(companies);
        await BackfillJobs(jobs);
        await CreateIndexes(companies, jobs);

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static async Task BackfillCompanies(IMongoCollection<BsonDocument> companies)
    {
        var rows = await companies.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var usedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var company in rows.OrderBy(x => x.GetValue("CreatedDate", BsonNull.Value).ToString()))
        {
            string id = company.GetValue("_id", string.Empty).ToString();
            string name = company.GetValue("CompanyName", "company").ToString();
            string slug = Unique(Normalize(company.GetValue("CareerSlug", string.Empty).ToString()), Normalize(name), usedSlugs);
            string code = UniqueCode(company.GetValue("PublicCompanyCode", string.Empty).ToString(), name, usedCodes);

            await companies.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", id),
                Builders<BsonDocument>.Update
                    .Set("CareerSlug", slug)
                    .Set("CareerPortalEnabled", company.GetValue("CareerPortalEnabled", true).ToBoolean())
                    .Set("PublicCompanyCode", code)
                    .Set("PublishJobsToMasterPortal", company.GetValue("PublishJobsToMasterPortal", true).ToBoolean()));
        }
    }

    private static async Task BackfillJobs(IMongoCollection<BsonDocument> jobs)
    {
        var rows = await jobs.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var usedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedPublicIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var job in rows.OrderBy(x => x.GetValue("CreatedDate", BsonNull.Value).ToString()))
        {
            string id = job.GetValue("_id", string.Empty).ToString();
            string title = job.GetValue("Title", "job").ToString();
            string publicId = job.GetValue("PublicJobId", string.Empty).ToString();
            if (string.IsNullOrWhiteSpace(publicId) || usedPublicIds.Contains(publicId))
            {
                publicId = $"JOB-{Guid.NewGuid():N}";
            }
            usedPublicIds.Add(publicId);

            string slug = Unique(Normalize(job.GetValue("Slug", string.Empty).ToString()), Normalize($"{title}-{publicId[..8]}"), usedSlugs);

            await jobs.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", id),
                Builders<BsonDocument>.Update
                    .Set("PublicJobId", publicId)
                    .Set("Slug", slug)
                    .Set("PublishToCareerPortal", job.GetValue("PublishToCareerPortal", true).ToBoolean())
                    .Set("PublishToMasterPortal", job.GetValue("PublishToMasterPortal", true).ToBoolean())
                    .Set("ApplicationMode", string.IsNullOrWhiteSpace(job.GetValue("ApplicationMode", string.Empty).ToString()) ? "Internal" : job.GetValue("ApplicationMode").ToString())
                    .Set("PublishedAt", job.GetValue("PublishedAt", job.GetValue("CreatedDate", DateTime.UtcNow))));
        }
    }

    private static async Task CreateIndexes(IMongoCollection<BsonDocument> companies, IMongoCollection<BsonDocument> jobs)
    {
        await companies.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("PublicCompanyCode"),
            new CreateIndexOptions<BsonDocument> { Name = "ux_company_public_code", Unique = true }));

        await jobs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("PublicJobId"),
            new CreateIndexOptions<BsonDocument> { Name = "ux_job_public_id", Unique = true }));

        await jobs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("Slug"),
            new CreateIndexOptions<BsonDocument> { Name = "ix_job_public_slug" }));

        await jobs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("CompanyId").Ascending("PublishToCareerPortal").Ascending("PublishToMasterPortal"),
            new CreateIndexOptions<BsonDocument> { Name = "ix_public_career_jobs" }));
    }

    private static string Unique(string preferred, string fallback, HashSet<string> used)
    {
        string baseValue = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
        if (string.IsNullOrWhiteSpace(baseValue)) baseValue = "item";
        string value = baseValue;
        for (int suffix = 2; used.Contains(value); suffix++) value = $"{baseValue}-{suffix}";
        used.Add(value);
        return value;
    }

    private static string UniqueCode(string preferred, string companyName, HashSet<string> used)
    {
        string normalized = Regex.Replace(preferred ?? string.Empty, @"\D", string.Empty);
        string code = normalized.Length == 6 ? normalized : CreateSixDigitCode(companyName, used);
        if (used.Contains(code)) code = CreateSixDigitCode(companyName, used);
        used.Add(code);
        return code;
    }

    private static string CreateSixDigitCode(string seed, HashSet<string> used)
    {
        int candidate = (int)(2166136261u ^ (uint)StringComparer.OrdinalIgnoreCase.GetHashCode(seed ?? string.Empty));
        candidate = 100000 + Math.Abs(candidate % 900000);
        while (used.Contains(candidate.ToString()))
            candidate = candidate == 999999 ? 100000 : candidate + 1;
        return candidate.ToString();
    }

    private static string Normalize(string value) =>
        Regex.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
}
