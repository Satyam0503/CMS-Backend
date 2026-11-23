using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddCandidateAnonymizationIndexes : IMigration
{
    public string Id => $"2026-11-18-{nameof(AddCandidateAnonymizationIndexes)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        await CreateIndex(db, "Applicant", "ix_applicant_retention_anonymization", ["ConsentStatus", "RetentionUntilUtc", "AnonymizationCleanupPending", "AnonymizationCleanupCompletedAtUtc"]);
        await CreateIndex(db, "RecruitmentEmailOutbox", "ix_recruitment_email_company_related_applicant", ["CompanyId", "RelatedApplicantId"]);
        await CreateIndex(db, "RecruitmentJobApplication", "ix_recruitment_application_company_applicant", ["CompanyId", "ApplicantId"]);
        await CreateIndex(db, "ApplicantLogs", "ix_applicant_logs_company_applicant", ["CompanyId", "ApplicantId"]);
        await CreateIndex(db, "RecruitmentExternalActionToken", "ix_recruitment_action_token_company_applicant", ["CompanyId", "ApplicantId"]);
        await CreateIndex(db, "RecruitmentCandidateShare", "ix_recruitment_candidate_share_company_applicant", ["CompanyId", "ApplicantId"]);

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static Task CreateIndex(IMongoDatabase db, string collection, string name, string[] fields)
    {
        var keys = new BsonDocument(fields.Select(field => new BsonElement(field, 1)));
        return db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = name }));
    }
}
