using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddRecruitmentAnalyticsIndexes : IMigration
{
    public string Id => $"2026-11-18-{nameof(AddRecruitmentAnalyticsIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        foreach (var plan in CreateIndexPlans())
        {
            var options = new CreateIndexOptions<BsonDocument> { Name = plan.Name };
            if (plan.PartialFilter is not null) options.PartialFilterExpression = plan.PartialFilter;
            await db.GetCollection<BsonDocument>(plan.CollectionName).Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(plan.Keys, options));
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    public static IReadOnlyList<RecruitmentAnalyticsIndexPlan> CreateIndexPlans() =>
    [
        new("Applicant", "ix_ats_analytics_company_active_page",
            new BsonDocument { { "CompanyId", 1 }, { "IsDeleted", 1 }, { "ApplicantId", 1 } }),
        new("Applicant", "ix_ats_analytics_company_vacancy_active_page",
            new BsonDocument { { "CompanyId", 1 }, { "VacancyId", 1 }, { "IsDeleted", 1 }, { "ApplicantId", 1 } }),
        new("Applicant", "ix_ats_analytics_company_merged_count",
            new BsonDocument { { "CompanyId", 1 }, { "MergeStatus", 1 }, { "VacancyId", 1 } },
            new BsonDocument("MergeStatus", "Merged")),
        new("RecruitmentJobApplication", "ix_ats_analytics_application_batch",
            new BsonDocument { { "CompanyId", 1 }, { "ApplicantId", 1 }, { "IsDeleted", 1 }, { "UpdatedDate", -1 } }),
        new("RecruitmentJobApplication", "ix_ats_analytics_application_vacancy_batch",
            new BsonDocument { { "CompanyId", 1 }, { "ApplicantId", 1 }, { "IsDeleted", 1 }, { "VacancyId", 1 }, { "UpdatedDate", -1 } }),
        new("RecruitmentOffer", "ix_ats_analytics_offer_batch",
            new BsonDocument { { "CompanyId", 1 }, { "ApplicantId", 1 }, { "Status", 1 }, { "IsDeleted", 1 }, { "OfferId", 1 } }),
        new("RecruitmentOffer", "ix_ats_analytics_offer_vacancy_batch",
            new BsonDocument { { "CompanyId", 1 }, { "ApplicantId", 1 }, { "Status", 1 }, { "IsDeleted", 1 }, { "VacancyId", 1 }, { "OfferId", 1 } })
    ];
}

public sealed record RecruitmentAnalyticsIndexPlan(
    string CollectionName,
    string Name,
    BsonDocument Keys,
    BsonDocument? PartialFilter = null);
