using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.DTO.PublicCareers;
using Codeji.CMS.Utility.Enums;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentLifecycleRulesTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, false, true)]
    public void Reapplication_cooldown_follows_company_multi_vacancy_setting(
        bool allowMultipleVacancies,
        bool sameVacancy,
        bool expected)
    {
        var now = new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, PublicCareerApplicationRules.IsWithinReapplicationCooldown(
            now.AddMonths(-1), now, allowMultipleVacancies, sameVacancy));
    }

    [Fact]
    public void Reapplication_is_allowed_after_the_cooldown_expires()
    {
        var now = new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(PublicCareerApplicationRules.IsWithinReapplicationCooldown(
            now.AddMonths(-6), now, allowMultiVacancyApplications: false, sameVacancy: false));
    }

    [Theory]
    [InlineData("Notice text", "policy-2", true)]
    [InlineData(" ", "policy-2", false)]
    [InlineData("Notice text", null, false)]
    public void Privacy_notice_must_have_both_text_and_version(string? text, string? version, bool expected)
    {
        Assert.Equal(expected, PublicCareerApplicationRules.IsPrivacyNoticeConfigured(text, version));
    }

    [Theory]
    [InlineData(true, "policy-2", "policy-2", true)]
    [InlineData(true, " policy-2 ", "policy-2", true)]
    [InlineData(true, "policy-1", "policy-2", false)]
    [InlineData(false, "policy-2", "policy-2", false)]
    [InlineData(true, null, "policy-2", false)]
    public void Application_acknowledgement_requires_the_current_notice_version(
        bool acknowledged, string? submitted, string? current, bool expected)
    {
        Assert.Equal(expected, PublicCareerApplicationRules.AcknowledgesCurrentPrivacyNotice(acknowledged, submitted, current));
    }

    [Fact]
    public void Privacy_notice_acknowledgement_does_not_grant_candidate_consent()
    {
        var applicant = new Applicant { ConsentStatus = "Unknown", Phone = "5550100" };
        var acknowledgedAtUtc = new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

        PublicCareerApplicationRules.RecordPrivacyNoticeAcknowledgement(
            applicant, "Candidate information is used to assess this application.", "policy-3", acknowledgedAtUtc);

        Assert.Equal("Unknown", applicant.ConsentStatus);
        Assert.Null(applicant.ConsentGrantedAtUtc);
        Assert.Equal(acknowledgedAtUtc, applicant.PrivacyNoticeAcknowledgedAtUtc);
        Assert.Equal("policy-3", applicant.PrivacyNoticeVersion);
        Assert.Equal("career-portal", applicant.PrivacyNoticeAcknowledgementSource);
    }

    [Fact]
    public void Anonymization_scrubs_application_identity_resume_metadata_and_privacy_snapshot()
    {
        var applicant = new Applicant
        {
            ApplicantId = "a-privacy",
            ConsentStatus = "Granted",
            ConsentGrantedAtUtc = DateTime.UtcNow,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Phone = "5550100",
            State = "Karnataka",
            Experience = 12,
            CurrentCompany = "Analytical Engines",
            CurrentJobTitle = "Engineer",
            NoticePeriodDays = 30,
            CoverLetter = "Private note",
            LinkedInUrl = "https://example.com/ada",
            PortfolioUrl = "https://example.com/work",
            CandidateReportedSource = "Employee referral",
            CandidateReportedSourceDetail = "Referral from colleague",
            AttributionSource = "linkedin",
            AttributionCampaign = "engineering-roles",
            ReferrerHost = "news.example",
            AttributionCapturedAtUtc = DateTime.UtcNow,
            ResumeUrl = "private:resume.pdf",
            ResumeQuarantineFileName = "0123456789abcdef0123456789abcdef.quarantine",
            ResumeQuarantinedAtUtc = DateTime.UtcNow,
            PrivacyNoticeVersion = "policy-1",
            PrivacyNoticeText = "Private policy snapshot",
            PrivacyNoticeAcknowledgementSource = "career-portal",
            ApplicationSubmissionIdempotencyKey = "opaque-submission-key",
            ApplicationSubmissionPayloadHash = "hashed-payload",
            PublicResumeUploadTokenId = "upload-token",
            PublicResumeUploadSha256 = "resume-hash"
        };
        var nowUtc = DateTime.UtcNow;

        PublicCareerApplicationRules.AnonymizeApplicant(applicant, nowUtc);

        Assert.Equal("Anonymized", applicant.ConsentStatus);
        Assert.Equal(nowUtc, applicant.AnonymizedAtUtc);
        Assert.DoesNotContain("@example.com", applicant.Email);
        Assert.Equal("0000000000", applicant.Phone);
        Assert.Empty(applicant.State);
        Assert.Equal(0, applicant.Experience);
        Assert.Null(applicant.CurrentCompany);
        Assert.Null(applicant.CurrentJobTitle);
        Assert.Null(applicant.NoticePeriodDays);
        Assert.Null(applicant.CoverLetter);
        Assert.Null(applicant.LinkedInUrl);
        Assert.Null(applicant.PortfolioUrl);
        Assert.Null(applicant.CandidateReportedSource);
        Assert.Null(applicant.CandidateReportedSourceDetail);
        Assert.Null(applicant.AttributionSource);
        Assert.Null(applicant.AttributionCampaign);
        Assert.Null(applicant.ReferrerHost);
        Assert.Null(applicant.AttributionCapturedAtUtc);
        Assert.Equal(string.Empty, applicant.ResumeUrl);
        Assert.Null(applicant.PrivacyNoticeText);
        Assert.Null(applicant.ApplicationSubmissionIdempotencyKey);
        Assert.Null(applicant.ApplicationSubmissionPayloadHash);
        Assert.Null(applicant.PublicResumeUploadTokenId);
        Assert.Null(applicant.PublicResumeUploadSha256);
        Assert.Null(applicant.ResumeQuarantineFileName);
        Assert.Null(applicant.ResumeQuarantinedAtUtc);
        Assert.Equal("0123456789abcdef0123456789abcdef.quarantine", applicant.AnonymizationCleanupResumeQuarantineFileName);
    }

    [Fact]
    public void Application_retry_requires_the_same_normalized_payload()
    {
        var original = new PublicJobApplicationRequest
        {
            IdempotencyKey = "submission-key-123",
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Phone = "5550100",
            State = "Karnataka",
            Experience = 2.5m,
            CurrentCompany = "Analytical Engines",
            CandidateReportedSource = "Employee referral",
            CandidateReportedSourceDetail = "From a colleague",
            AttributionSource = "linkedin",
            AttributionCampaign = "engineering-roles",
            ReferrerHost = "news.example",
            NoticePeriodDays = 30,
            PrivacyAcknowledged = true,
            PrivacyNoticeVersion = "policy-1"
        };
        var applicant = new Applicant
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Phone = "5550100",
            State = "Karnataka",
            Experience = 30,
            CurrentCompany = "Analytical Engines",
            CandidateReportedSource = "Employee referral",
            CandidateReportedSourceDetail = "From a colleague",
            AttributionSource = "linkedin",
            AttributionCampaign = "engineering-roles",
            ReferrerHost = "news.example",
            NoticePeriodDays = 30,
            PrivacyNoticeVersion = "policy-1",
            ApplicationSubmissionPayloadHash = PublicCareerApplicationRules.HashApplicationSubmission(original)
        };
        var normalizedRetry = new PublicJobApplicationRequest
        {
            IdempotencyKey = original.IdempotencyKey,
            FirstName = " Ada ",
            LastName = "Lovelace",
            Email = " ADA@example.com ",
            Phone = "5550100",
            State = "Karnataka",
            Experience = 2.5m,
            CurrentCompany = "Analytical Engines",
            CandidateReportedSource = "employee referral",
            CandidateReportedSourceDetail = "From a colleague",
            AttributionSource = "linkedin",
            AttributionCampaign = "engineering-roles",
            ReferrerHost = "NEWS.EXAMPLE",
            NoticePeriodDays = 30,
            PrivacyAcknowledged = true,
            PrivacyNoticeVersion = "policy-1"
        };

        Assert.True(PublicCareerApplicationRules.IsSameApplicationSubmission(applicant, normalizedRetry));
        Assert.False(PublicCareerApplicationRules.IsSameApplicationSubmission(applicant, new PublicJobApplicationRequest
        {
            IdempotencyKey = original.IdempotencyKey,
            FirstName = original.FirstName,
            LastName = original.LastName,
            Email = original.Email,
            Phone = original.Phone,
            State = original.State,
            Experience = original.Experience,
            CurrentCompany = original.CurrentCompany,
            CandidateReportedSource = "Job board",
            CandidateReportedSourceDetail = original.CandidateReportedSourceDetail,
            AttributionSource = original.AttributionSource,
            AttributionCampaign = original.AttributionCampaign,
            ReferrerHost = original.ReferrerHost,
            NoticePeriodDays = original.NoticePeriodDays,
            PrivacyAcknowledged = true,
            PrivacyNoticeVersion = original.PrivacyNoticeVersion
        }));
        Assert.False(PublicCareerApplicationRules.IsSameApplicationSubmission(applicant, new PublicJobApplicationRequest
        {
            IdempotencyKey = original.IdempotencyKey,
            FirstName = original.FirstName,
            LastName = original.LastName,
            Email = original.Email,
            Phone = "5550199",
            State = original.State,
            Experience = original.Experience,
            CurrentCompany = original.CurrentCompany,
            NoticePeriodDays = original.NoticePeriodDays,
            PrivacyAcknowledged = true,
            PrivacyNoticeVersion = original.PrivacyNoticeVersion
        }));
    }

    [Fact]
    public void Source_attribution_only_accepts_known_candidate_options_and_bounded_tags()
    {
        var request = new PublicJobApplicationRequest
        {
            IdempotencyKey = "submission-key-123", FirstName = "Ada", LastName = "Lovelace",
            Email = "ada@example.com", Phone = "5550100", CandidateReportedSource = "Employee referral",
            AttributionSource = "linkedin", AttributionCampaign = "engineering-roles", ReferrerHost = "jobs.example"
        };

        Assert.True(PublicCareerApplicationRules.IsValidSourceAttribution(request));
        var companySources = new[] { "Talent community", "Campus partner" };
        Assert.True(PublicCareerApplicationRules.IsValidSourceAttribution(
            new PublicJobApplicationRequest
            {
                IdempotencyKey = request.IdempotencyKey, FirstName = request.FirstName, LastName = request.LastName,
                Email = request.Email, Phone = request.Phone, CandidateReportedSource = "campus partner"
            }, companySources));
        Assert.Equal("Campus partner", PublicCareerApplicationRules.NormalizeCandidateReportedSource("campus partner", companySources));
        Assert.False(PublicCareerApplicationRules.IsValidSourceAttribution(request, companySources));
        Assert.False(PublicCareerApplicationRules.IsValidSourceAttribution(
            new PublicJobApplicationRequest
            {
                IdempotencyKey = request.IdempotencyKey, FirstName = request.FirstName, LastName = request.LastName,
                Email = request.Email, Phone = request.Phone, CandidateReportedSource = "Employee referral",
                CandidateReportedSourceDetail = "Entered before the source was changed"
            }));
        Assert.False(PublicCareerApplicationRules.IsValidSourceAttribution(new PublicJobApplicationRequest
        {
            IdempotencyKey = request.IdempotencyKey, FirstName = request.FirstName, LastName = request.LastName,
            Email = request.Email, Phone = request.Phone, CandidateReportedSource = "Unapproved channel"
        }));
        Assert.False(PublicCareerApplicationRules.IsValidSourceAttribution(new PublicJobApplicationRequest
        {
            IdempotencyKey = request.IdempotencyKey, FirstName = request.FirstName, LastName = request.LastName,
            Email = request.Email, Phone = request.Phone, AttributionCampaign = new string('x', 201)
        }));
        Assert.False(PublicCareerApplicationRules.IsValidSourceAttribution(new PublicJobApplicationRequest
        {
            IdempotencyKey = request.IdempotencyKey, FirstName = request.FirstName, LastName = request.LastName,
            Email = request.Email, Phone = request.Phone, ReferrerHost = "https://jobs.example/private"
        }));
    }

    [Fact]
    public void Legacy_application_retry_compares_all_saved_fields_when_hash_is_missing()
    {
        var request = new PublicJobApplicationRequest
        {
            IdempotencyKey = "submission-key-123",
            FirstName = "Grace",
            LastName = "Hopper",
            Email = "grace@example.com",
            Phone = "5550101",
            State = "Virginia",
            Experience = 10,
            PrivacyAcknowledged = true,
            PrivacyNoticeVersion = "policy-2"
        };
        var applicant = new Applicant
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            State = request.State,
            Experience = 120,
            PrivacyNoticeVersion = request.PrivacyNoticeVersion
        };

        Assert.True(PublicCareerApplicationRules.IsSameApplicationSubmission(applicant, request));
        Assert.False(PublicCareerApplicationRules.IsSameApplicationSubmission(applicant, new PublicJobApplicationRequest
        {
            IdempotencyKey = request.IdempotencyKey,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            State = request.State,
            Experience = request.Experience,
            CoverLetter = "Changed content",
            PrivacyAcknowledged = true,
            PrivacyNoticeVersion = request.PrivacyNoticeVersion
        }));
    }

    [Fact]
    public void Resume_upload_retry_is_idempotent_only_for_the_completed_same_file()
    {
        var token = new PublicApplicationToken
        {
            IsUsed = true,
            UploadCompletedAtUtc = DateTime.UtcNow,
            UploadSha256 = "AABB"
        };

        Assert.True(PublicCareerApplicationRules.IsCompletedResumeUploadReplay(token, "AABB"));
        Assert.False(PublicCareerApplicationRules.IsCompletedResumeUploadReplay(token, "CCDD"));

        token.UploadCompletedAtUtc = null;
        Assert.False(PublicCareerApplicationRules.IsCompletedResumeUploadReplay(token, "AABB"));
    }

    [Fact]
    public void Resume_upload_claim_becomes_reclaimable_after_its_lease()
    {
        var nowUtc = new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);
        var token = new PublicApplicationToken { IsProcessing = true, ProcessingStartedAtUtc = nowUtc.AddMinutes(-2) };

        Assert.True(PublicCareerApplicationRules.IsResumeUploadClaimActive(token, nowUtc));
        token.ProcessingStartedAtUtc = nowUtc.AddMinutes(-11);
        Assert.False(PublicCareerApplicationRules.IsResumeUploadClaimActive(token, nowUtc));
        Assert.Equal(nowUtc.AddMinutes(-10), PublicCareerApplicationRules.ResumeUploadClaimExpiry(nowUtc));
    }

    [Fact]
    public void Public_resume_upload_is_denied_for_anonymized_or_deleted_candidates()
    {
        Assert.False(PublicCareerApplicationRules.CanAcceptPublicResumeUpload(null));
        Assert.False(PublicCareerApplicationRules.CanAcceptPublicResumeUpload(new Applicant { ConsentStatus = "Anonymized", Phone = "000" }));
        Assert.False(PublicCareerApplicationRules.CanAcceptPublicResumeUpload(new Applicant { ConsentStatus = "Granted", IsDeleted = true, Phone = "000" }));
        Assert.True(PublicCareerApplicationRules.CanAcceptPublicResumeUpload(new Applicant { ConsentStatus = "Granted", Phone = "123" }));
    }

    [Theory]
    [InlineData("Completed", null, true)]
    [InlineData("Skipped", null, false)]
    [InlineData("Skipped", "Approved exception", true)]
    [InlineData("InProgress", null, false)]
    public void Preboarding_skip_requires_an_auditable_reason(string status, string? reason, bool expected)
    {
        Assert.Equal(expected, RecruitmentPreboardingTaskRules.IsResolved(new RecruitmentPreboardingTask { Code = "documents", Status = status, SkipReason = reason }));
    }

    [Theory]
    [InlineData("Simple", false, false)]
    [InlineData("Enterprise", true, true)]
    public void Preboarding_policy_respects_company_workflow(string mode, bool requiredByConfig, bool expected)
    {
        var config = new RecruitmentEnterpriseConfig
        {
            RecruitmentMode = mode,
            RequirePreboardingBeforeHandoff = requiredByConfig,
            AllowJobWorkflowCustomization = false,
        };

        Assert.Equal(expected, RecruitmentPreboardingPolicy.Requires(config, null));
    }

    [Fact]
    public void Direct_vacancy_explicitly_skips_preboarding_when_workflow_overrides_are_enabled()
    {
        var config = new RecruitmentEnterpriseConfig { RecruitmentMode = "Enterprise", AllowJobWorkflowCustomization = true };
        var vacancy = new JobVacancy { WorkflowMode = "Direct" };

        Assert.False(RecruitmentPreboardingPolicy.Requires(config, vacancy));
    }

    [Fact]
    public void Interview_schedule_requires_utc_end_after_start()
    {
        var start = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);
        Assert.True(RecruitmentInterviewRules.IsValidSchedule(start, start.AddMinutes(30)));
        Assert.False(RecruitmentInterviewRules.IsValidSchedule(start, start.AddMinutes(-1)));
        Assert.False(RecruitmentInterviewRules.IsValidSchedule(DateTime.SpecifyKind(start, DateTimeKind.Local), start.AddMinutes(30)));
    }

    [Fact]
    public void Interview_and_offer_transitions_are_server_authoritative()
    {
        Assert.True(RecruitmentInterviewRules.IsAllowedTransition(RecruitmentInterviewStatus.Draft, RecruitmentInterviewStatus.Scheduled));
        Assert.False(RecruitmentInterviewRules.IsAllowedTransition(RecruitmentInterviewStatus.Completed, RecruitmentInterviewStatus.Scheduled));
        Assert.True(RecruitmentOfferRules.IsAllowedTransition(RecruitmentOfferStatus.Approved, RecruitmentOfferStatus.Sent));
        Assert.False(RecruitmentOfferRules.IsAllowedTransition(RecruitmentOfferStatus.Draft, RecruitmentOfferStatus.Sent));
    }

    [Fact]
    public void Offer_compensation_rejects_missing_currency_or_negative_salary()
    {
        Assert.True(RecruitmentOfferRules.IsValidCompensation(100m, "INR"));
        Assert.False(RecruitmentOfferRules.IsValidCompensation(-1m, "INR"));
        Assert.False(RecruitmentOfferRules.IsValidCompensation(100m, ""));
    }

    [Fact]
    public void Legacy_pipeline_snapshot_is_stable_and_parses_without_empty_stages()
    {
        Assert.Equal("New|InProgress|OnHold|Shortlisted|Selected|Rejected|ReApply", RecruitmentWorkflowSnapshot.LegacyV1);
        var stages = RecruitmentPipelineRules.ParseSnapshot($"  {RecruitmentWorkflowSnapshot.LegacyV1} | ");
        Assert.Equal(new[] { "New", "InProgress", "OnHold", "Shortlisted", "Selected", "Rejected", "ReApply" }, stages);
    }

    [Fact]
    public void Pipeline_rules_reject_backward_moves_and_terminal_changes()
    {
        Assert.True(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.Shortlisted, ActivityType.OnHold, out var holdError));
        Assert.True(string.IsNullOrWhiteSpace(holdError));
        Assert.False(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.Shortlisted, ActivityType.InProgress, out var backwardError));
        Assert.Contains("back", backwardError, StringComparison.OrdinalIgnoreCase);
        Assert.False(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.Selected, ActivityType.Rejected, out var terminalError));
        Assert.Contains("cannot", terminalError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Shortlisted_candidate_can_transition_to_rejected_directly()
    {
        Assert.True(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.Shortlisted, ActivityType.Rejected, out var error));
        Assert.True(string.IsNullOrWhiteSpace(error));

        Assert.True(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.InProgress, ActivityType.Rejected, out var error2));
        Assert.True(string.IsNullOrWhiteSpace(error2));

        Assert.True(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.OnHold, ActivityType.Rejected, out var error3));
        Assert.True(string.IsNullOrWhiteSpace(error3));

        // Once rejected, terminal state cannot transition to any other stage
        Assert.False(RecruitmentPipelineRules.IsAllowedTransition(ActivityType.Rejected, ActivityType.Shortlisted, out var error4));
        Assert.Contains("cannot be changed after it is Rejected", error4, StringComparison.OrdinalIgnoreCase);
    }
}
