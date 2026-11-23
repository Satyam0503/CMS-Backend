using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Prevents multiple active recruitment configurations for one tenant. The
/// migration stops without changing data when existing active duplicates need
/// an explicit company-by-company resolution.
/// </summary>
public sealed class AddRecruitmentConfigCompanyUniqueIndex : IMigration
{
    public string Id => $"2026-11-19-{nameof(AddRecruitmentConfigCompanyUniqueIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var configs = db.GetCollection<BsonDocument>("RecruitmentEnterpriseConfig");
        var activeConfigs = await configs.Find(new BsonDocument("IsDeleted", false))
            .Project(Builders<BsonDocument>.Projection.Include("CompanyId").Exclude("_id"))
            .ToListAsync();
        var issues = FindPreflightIssues(activeConfigs);
        if (issues.Count > 0)
            throw new InvalidOperationException(
                $"Cannot enforce one active recruitment configuration per company until existing data is resolved: {string.Join(", ", issues)}. No configuration records were changed.");

        var plan = CreateIndexPlan();
        await configs.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            plan.Keys,
            new CreateIndexOptions<BsonDocument>
            {
                Name = plan.Name,
                Unique = plan.Unique,
                PartialFilterExpression = plan.PartialFilter,
            }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    public static RecruitmentConfigUniqueIndexPlan CreateIndexPlan() => new(
        "ux_ats_config_active_company",
        new BsonDocument("CompanyId", 1),
        new BsonDocument("IsDeleted", false),
        Unique: true);

    public static IReadOnlyList<string> FindPreflightIssues(IEnumerable<BsonDocument> activeConfigs)
    {
        var companyIds = activeConfigs
            .Select(document => document.TryGetValue("CompanyId", out var value) && value.IsString
                ? value.AsString.Trim()
                : string.Empty)
            .ToList();
        var issues = new List<string>();
        var missingCompanyIds = companyIds.Count(string.IsNullOrWhiteSpace);
        if (missingCompanyIds > 0) issues.Add($"missing CompanyId ({missingCompanyIds} active record(s))");
        issues.AddRange(companyIds
            .Where(companyId => !string.IsNullOrWhiteSpace(companyId))
            .GroupBy(companyId => companyId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => $"{group.Key} ({group.Count()} active records)"));
        return issues;
    }
}

public sealed record RecruitmentConfigUniqueIndexPlan(string Name, BsonDocument Keys, BsonDocument PartialFilter, bool Unique);
