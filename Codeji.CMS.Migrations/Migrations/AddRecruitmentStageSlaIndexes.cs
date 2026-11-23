using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddRecruitmentStageSlaIndexes : IMigration
{
    public string Id => $"2026-11-18-{nameof(AddRecruitmentStageSlaIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        foreach (var plan in CreateIndexPlans())
        {
            await db.GetCollection<BsonDocument>(plan.CollectionName).Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(plan.Keys, new CreateIndexOptions<BsonDocument> { Name = plan.Name }));
        }

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    public static IReadOnlyList<(string CollectionName, string Name, BsonDocument Keys)> CreateIndexPlans() =>
    [
        ("RecruitmentJobApplication", "ix_ats_stage_sla_company_active_page", new BsonDocument { { "CompanyId", 1 }, { "IsDeleted", 1 }, { "ApplicationId", 1 } }),
        ("JobVacancy", "ix_ats_stage_sla_company_job", new BsonDocument { { "CompanyId", 1 }, { "IsDeleted", 1 }, { "JobId", 1 } }),
        ("RecruitmentStageSlaPolicy", "ix_ats_stage_sla_policy_company_stage_location", new BsonDocument { { "CompanyId", 1 }, { "IsDeleted", 1 }, { "Stage", 1 }, { "Location", 1 } })
    ];
}
