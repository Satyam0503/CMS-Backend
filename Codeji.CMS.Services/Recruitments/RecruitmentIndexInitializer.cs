using Codeji.CMS.Repository.Entities.Recruitments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>
/// Creates the unique index that makes anonymous application submissions safe
/// against concurrent retries across API instances. Startup fails if MongoDB
/// cannot enforce this invariant; the API must not accept applications without it.
/// </summary>
public sealed class RecruitmentIndexInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<RecruitmentIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var applicants = database.GetCollection<Applicant>(nameof(Applicant));
        await applicants.Indexes.CreateOneAsync(
            CreateSubmissionIdempotencyIndex(),
            cancellationToken: cancellationToken);
        await applicants.Indexes.CreateOneAsync(
            CreateResumeScanQueueIndex(),
            cancellationToken: cancellationToken);
        await applicants.Indexes.CreateOneAsync(
            CreateResumeScanHealthIndex(),
            cancellationToken: cancellationToken);
        var applications = database.GetCollection<RecruitmentJobApplication>(nameof(RecruitmentJobApplication));
        await applications.Indexes.CreateOneAsync(
            CreatePublicApplicationReplayIndex(),
            cancellationToken: cancellationToken);
        var screeningReviews = database.GetCollection<RecruitmentScreeningReview>(nameof(RecruitmentScreeningReview));
        await screeningReviews.Indexes.CreateOneAsync(
            CreatePendingScreeningStageSyncIndex(),
            cancellationToken: cancellationToken);
        var hiringDecisions = database.GetCollection<RecruitmentHiringDecision>(nameof(RecruitmentHiringDecision));
        await hiringDecisions.Indexes.CreateOneAsync(
            CreatePendingHiringDecisionStageSyncIndex(),
            cancellationToken: cancellationToken);
        var applicantLogs = database.GetCollection<ApplicantLogs>(nameof(ApplicantLogs));
        await applicantLogs.Indexes.CreateOneAsync(
            CreatePublicApplicationActivityIndex(),
            cancellationToken: cancellationToken);
        var applicationTokens = database.GetCollection<PublicApplicationToken>(nameof(PublicApplicationToken));
        await applicationTokens.Indexes.CreateOneAsync(
            CreateApplicationTokenExpiryIndex(),
            cancellationToken: cancellationToken);
        logger.LogInformation("Recruitment idempotency index is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public static CreateIndexModel<Applicant> CreateSubmissionIdempotencyIndex()
    {
        var keys = Builders<Applicant>.IndexKeys
            .Ascending(x => x.CompanyId)
            .Ascending(x => x.ApplicationSubmissionIdempotencyKey);
        var options = new CreateIndexOptions<Applicant>
        {
            Name = "ux_applicant_company_submission_key",
            Unique = true,
            PartialFilterExpression = Builders<Applicant>.Filter.Type(
                x => x.ApplicationSubmissionIdempotencyKey,
                BsonType.String)
        };
        return new CreateIndexModel<Applicant>(keys, options);
    }

    public static CreateIndexModel<Applicant> CreateResumeScanQueueIndex()
    {
        var keys = Builders<Applicant>.IndexKeys
            .Ascending(x => x.IsDeleted)
            .Ascending(x => x.ResumeScannedAtUtc)
            .Ascending(x => x.ApplicantId);
        return new CreateIndexModel<Applicant>(keys, new CreateIndexOptions<Applicant>
        {
            Name = "ix_ats_resume_scan_migration",
            PartialFilterExpression = Builders<Applicant>.Filter.Type(x => x.ResumeUrl, BsonType.String)
        });
    }

    public static CreateIndexModel<Applicant> CreateResumeScanHealthIndex()
    {
        var keys = Builders<Applicant>.IndexKeys
            .Ascending(x => x.CompanyId)
            .Ascending(x => x.IsDeleted)
            .Ascending(x => x.ResumeScanVerdict)
            .Ascending(x => x.ResumeScannedAtUtc);
        return new CreateIndexModel<Applicant>(keys, new CreateIndexOptions<Applicant>
        {
            Name = "ix_ats_resume_scan_health_by_company",
            PartialFilterExpression = Builders<Applicant>.Filter.Type(x => x.ResumeUrl, BsonType.String)
        });
    }

    public static CreateIndexModel<PublicApplicationToken> CreateApplicationTokenExpiryIndex()
    {
        var keys = Builders<PublicApplicationToken>.IndexKeys.Ascending(x => x.ExpiresAt);
        var options = new CreateIndexOptions<PublicApplicationToken>
        {
            Name = "ttl_public_application_token_expiry",
            ExpireAfter = TimeSpan.Zero
        };
        return new CreateIndexModel<PublicApplicationToken>(keys, options);
    }

    public static CreateIndexModel<RecruitmentJobApplication> CreatePublicApplicationReplayIndex()
    {
        var keys = Builders<RecruitmentJobApplication>.IndexKeys
            .Ascending(x => x.CompanyId)
            .Ascending(x => x.PublicSubmissionIdempotencyKey);
        var options = new CreateIndexOptions<RecruitmentJobApplication>
        {
            Name = "ux_public_application_replay_company_key",
            Unique = true,
            PartialFilterExpression = Builders<RecruitmentJobApplication>.Filter.Type(
                x => x.PublicSubmissionIdempotencyKey, BsonType.String)
                & Builders<RecruitmentJobApplication>.Filter.Gt(
                    x => x.PublicSubmissionIdempotencyKey, string.Empty)
        };
        return new CreateIndexModel<RecruitmentJobApplication>(keys, options);
    }

    public static CreateIndexModel<RecruitmentScreeningReview> CreatePendingScreeningStageSyncIndex()
    {
        var keys = Builders<RecruitmentScreeningReview>.IndexKeys
            .Ascending(x => x.StageSyncStatus)
            .Ascending(x => x.CreatedDate);
        return new CreateIndexModel<RecruitmentScreeningReview>(keys, new CreateIndexOptions<RecruitmentScreeningReview>
        {
            Name = "ix_screening_stage_sync_pending",
            PartialFilterExpression = Builders<RecruitmentScreeningReview>.Filter.Eq(x => x.StageSyncStatus, "Pending")
        });
    }

    public static CreateIndexModel<RecruitmentHiringDecision> CreatePendingHiringDecisionStageSyncIndex()
    {
        var keys = Builders<RecruitmentHiringDecision>.IndexKeys
            .Ascending(x => x.StageSyncStatus)
            .Ascending(x => x.UpdatedDate);
        return new CreateIndexModel<RecruitmentHiringDecision>(keys, new CreateIndexOptions<RecruitmentHiringDecision>
        {
            Name = "ix_hiring_decision_stage_sync_pending",
            PartialFilterExpression = Builders<RecruitmentHiringDecision>.Filter.Eq(x => x.StageSyncStatus, "Pending")
        });
    }

    public static CreateIndexModel<ApplicantLogs> CreatePublicApplicationActivityIndex()
    {
        var keys = Builders<ApplicantLogs>.IndexKeys
            .Ascending(x => x.CompanyId)
            .Ascending(x => x.IdempotencyKey);
        var options = new CreateIndexOptions<ApplicantLogs>
        {
            Name = "ux_applicant_log_company_idempotency_key",
            Unique = true,
            PartialFilterExpression = Builders<ApplicantLogs>.Filter.Type(
                x => x.IdempotencyKey, BsonType.String)
                & Builders<ApplicantLogs>.Filter.Gt(x => x.IdempotencyKey, string.Empty)
        };
        return new CreateIndexModel<ApplicantLogs>(keys, options);
    }
}
