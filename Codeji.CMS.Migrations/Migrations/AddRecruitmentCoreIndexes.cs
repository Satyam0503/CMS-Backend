using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddRecruitmentCoreIndexes : IMigration
{
    public string Id => $"2026-09-18-{typeof(AddRecruitmentCoreIndexes).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;
        await CreateUnique(db, "RecruitmentHandoff", "ux_recruitment_handoff_company_applicant_idempotency", "CompanyId", "ApplicantId", "IdempotencyKey");
        await CreateUniqueNonEmpty(db, "RecruitmentInterview", "ux_recruitment_interview_company_applicant_idempotency", "CompanyId", "ApplicantId", "LastCommandIdempotencyKey");
        await CreateUniqueNonEmpty(db, "RecruitmentOffer", "ux_recruitment_offer_company_applicant_idempotency", "CompanyId", "ApplicantId", "LastCommandIdempotencyKey");
        await CreateUniqueNonEmpty(db, "RecruitmentRequisition", "ux_recruitment_requisition_company_idempotency", "CompanyId", "LastCommandIdempotencyKey");
        await CreateIndex(db, "RecruitmentRequisition", "ix_recruitment_requisition_company_status", "CompanyId", "Status", "UpdatedDate");
        await CreateUniqueNonEmpty(db, "RecruitmentScorecard", "ux_recruitment_scorecard_company_applicant_reviewer_idempotency", "CompanyId", "ApplicantId", "ReviewerUserId", "LastCommandIdempotencyKey");
        await CreateIndex(db, "RecruitmentScorecard", "ix_recruitment_scorecard_company_applicant", "CompanyId", "ApplicantId", "Status");
        await CreateUnique(db, "RecruitmentTalentPoolMember", "ux_recruitment_talent_pool_company_applicant_pool", "CompanyId", "ApplicantId", "PoolName");
        await CreateUniqueNonEmpty(db, "RecruitmentTalentPoolMember", "ux_recruitment_talent_pool_company_applicant_pool_idempotency", "CompanyId", "ApplicantId", "PoolName", "LastCommandIdempotencyKey");
        await CreateIndex(db, "RecruitmentTalentPoolMember", "ix_recruitment_talent_pool_company_status", "CompanyId", "Status", "UpdatedDate");
        await CreateIndex(db, "RecruitmentInterview", "ix_recruitment_interview_company_applicant_start", "CompanyId", "ApplicantId", "StartsAtUtc");
        await CreateIndex(db, "RecruitmentOffer", "ix_recruitment_offer_company_applicant_status", "CompanyId", "ApplicantId", "Status");
        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static Task CreateIndex(IMongoDatabase db, string collection, string name, params string[] fields) =>
        db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(fields.Select(x => new BsonElement(x, 1))), new CreateIndexOptions { Name = name }));

    private static async Task CreateUniqueNonEmpty(IMongoDatabase db, string collection, string name, params string[] fields)
    {
        try
        {
            var filter = new BsonDocument("LastCommandIdempotencyKey", new BsonDocument { { "$type", "string" }, { "$ne", "" } });
            await db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(fields.Select(x => new BsonElement(x, 1))), new CreateIndexOptions<BsonDocument> { Name = name, Unique = true, PartialFilterExpression = filter }));
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            Console.WriteLine($"Skipped {name}: {collection} contains legacy duplicate keys.");
        }
    }
    private static async Task CreateUnique(IMongoDatabase db, string collection, string name, params string[] fields)
    {
        try
        {
            await db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(fields.Select(x => new BsonElement(x, 1))), new CreateIndexOptions { Name = name, Unique = true }));
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            Console.WriteLine($"Skipped {name}: {collection} contains legacy duplicate keys.");
        }
    }
}



