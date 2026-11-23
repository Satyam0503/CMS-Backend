using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddRecruitmentResumeScanHealthIndex : IMigration
{
    public string Id => $"2026-11-19-{nameof(AddRecruitmentResumeScanHealthIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var plan = CreateIndexPlan();
        await db.GetCollection<BsonDocument>(plan.CollectionName).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(plan.Keys, new CreateIndexOptions<BsonDocument>
            {
                Name = plan.Name,
                PartialFilterExpression = plan.PartialFilter
            }));
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    public static RecruitmentResumeScanHealthIndexPlan CreateIndexPlan() => new(
        "Applicant",
        "ix_ats_resume_scan_health_by_company",
        new BsonDocument
        {
            { "CompanyId", 1 },
            { "IsDeleted", 1 },
            { "ResumeScanVerdict", 1 },
            { "ResumeScannedAtUtc", 1 }
        },
        new BsonDocument("ResumeUrl", new BsonDocument("$type", "string")));
}

public sealed record RecruitmentResumeScanHealthIndexPlan(
    string CollectionName,
    string Name,
    BsonDocument Keys,
    BsonDocument PartialFilter);
