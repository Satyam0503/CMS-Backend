using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentIndexInitializerTests
{
    [Fact]
    public void SubmissionKeyIndexIsUniqueAndNamedForTenantScopedIdempotency()
    {
        var index = RecruitmentIndexInitializer.CreateSubmissionIdempotencyIndex();

        Assert.Equal("ux_applicant_company_submission_key", index.Options.Name);
        Assert.True(index.Options.Unique);
        Assert.NotNull(index.Options.PartialFilterExpression);
    }

    [Fact]
    public void ApplicationTokenIndexExpiresUploadCredentialsAfterTheirExpiry()
    {
        var index = RecruitmentIndexInitializer.CreateApplicationTokenExpiryIndex();

        Assert.Equal("ttl_public_application_token_expiry", index.Options.Name);
        Assert.Equal(TimeSpan.Zero, index.Options.ExpireAfter);
    }

    [Fact]
    public void PublicApplicationArtifactsHaveUniqueTenantScopedReplayKeys()
    {
        var applicationIndex = RecruitmentIndexInitializer.CreatePublicApplicationReplayIndex();
        var activityIndex = RecruitmentIndexInitializer.CreatePublicApplicationActivityIndex();

        Assert.Equal("ux_public_application_replay_company_key", applicationIndex.Options.Name);
        Assert.True(applicationIndex.Options.Unique);
        Assert.NotNull(applicationIndex.Options.PartialFilterExpression);
        Assert.Equal("ux_applicant_log_company_idempotency_key", activityIndex.Options.Name);
        Assert.True(activityIndex.Options.Unique);
        Assert.NotNull(activityIndex.Options.PartialFilterExpression);
    }

    [Fact]
    public void ScreeningStageRecoveryHasPendingQueueIndex()
    {
        var index = RecruitmentIndexInitializer.CreatePendingScreeningStageSyncIndex();

        Assert.Equal("ix_screening_stage_sync_pending", index.Options.Name);
    }

    [Fact]
    public void HiringDecisionStageRecoveryHasPendingQueueIndex()
    {
        var index = RecruitmentIndexInitializer.CreatePendingHiringDecisionStageSyncIndex();

        Assert.Equal("ix_hiring_decision_stage_sync_pending", index.Options.Name);
        Assert.NotNull(index.Options.PartialFilterExpression);
    }

    [Fact]
    public void LegacyResumeScanHasOrderedQueueIndexBeforeBackgroundWorkerRuns()
    {
        var index = RecruitmentIndexInitializer.CreateResumeScanQueueIndex();

        Assert.Equal("ix_ats_resume_scan_migration", index.Options.Name);
        Assert.NotNull(index.Options.PartialFilterExpression);
    }

    [Fact]
    public void ResumeScanHealthHasTenantScopedIndexForOperationsCounts()
    {
        var index = RecruitmentIndexInitializer.CreateResumeScanHealthIndex();

        Assert.Equal("ix_ats_resume_scan_health_by_company", index.Options.Name);
        Assert.NotNull(index.Options.PartialFilterExpression);
    }
}
