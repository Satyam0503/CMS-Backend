using System.Linq.Expressions;
using System.Security.Claims;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentEnterpriseConfigResetTests
{
    [Fact]
    public void Override_metadata_tracks_nondefault_settings_without_marking_disabled_defaults()
    {
        var defaults = RecruitmentEnterpriseConfigRules.CreateDefaults();
        Assert.Empty(RecruitmentEnterpriseConfigRules.GetOverriddenFields(defaults));

        var customized = new RecruitmentEnterpriseConfig
        {
            RequireInterviewBeforeDecision = false,
            RequirePreboardingBeforeHandoff = false,
            RecruitmentMode = "Enterprise",
            Locations = ["London"],
            DefaultTimezone = "Europe/London",
            Scorecards = true,
            SlaTracking = true,
            EnableSla = true,
            CandidatePrivacyNoticeText = "Candidate notice",
            CandidatePrivacyNoticeVersion = "v2",
        };
        var overridden = RecruitmentEnterpriseConfigRules.GetOverriddenFields(customized);
        Assert.Contains(nameof(customized.RecruitmentMode), overridden);
        Assert.Contains(nameof(customized.Locations), overridden);
        Assert.Contains(nameof(customized.DefaultTimezone), overridden);
        Assert.Contains(nameof(customized.Scorecards), overridden);
        Assert.Contains("SlaTracking", overridden);
        Assert.Contains(nameof(customized.CandidatePrivacyNoticeText), overridden);
        Assert.Contains(nameof(customized.CandidatePrivacyNoticeVersion), overridden);
        Assert.DoesNotContain("Automation", overridden);
    }

    [Fact]
    public void Config_command_fingerprint_is_stable_when_dictionary_order_changes()
    {
        var first = new RecruitmentEnterpriseConfigCommand
        {
            IdempotencyKey = "config-command-fingerprint-01",
            CompensationComponents = new Dictionary<string, decimal> { ["Base"] = 1000, ["Bonus"] = 200 },
        };
        var reordered = new RecruitmentEnterpriseConfigCommand
        {
            IdempotencyKey = "config-command-fingerprint-01",
            CompensationComponents = new Dictionary<string, decimal> { ["Bonus"] = 200, ["Base"] = 1000 },
        };

        Assert.Equal(
            RecruitmentEnterpriseConfigRules.HashCommand(first),
            RecruitmentEnterpriseConfigRules.HashCommand(reordered));
    }

    [Fact]
    public void Config_version_filter_scopes_tenant_record_and_expected_version()
    {
        var filter = RecruitmentEnterpriseConfigRules.BuildVersionFilter("tenant-a", "config-a", 9);
        var rendered = filter.Render(new RenderArgs<RecruitmentEnterpriseConfig>(
            BsonSerializer.SerializerRegistry.GetSerializer<RecruitmentEnterpriseConfig>(),
            BsonSerializer.SerializerRegistry));

        Assert.Contains("tenant-a", rendered.ToJson());
        Assert.Contains("config-a", rendered.ToJson());
        Assert.Contains("DefaultVersion", rendered.ToJson());
        Assert.Contains("9", rendered.ToJson());
    }

    [Fact]
    public void Legacy_sla_and_automation_switches_normalize_to_one_capability_value()
    {
        var legacy = new RecruitmentEnterpriseConfig { EnableSla = true, SlaTracking = false, EnableAutomation = true, Automation = false };
        RecruitmentEnterpriseConfigRules.NormalizeHistoricalSwitches(legacy);

        Assert.True(legacy.EnableSla);
        Assert.True(legacy.SlaTracking);
        Assert.True(legacy.EnableAutomation);
        Assert.True(legacy.Automation);
        Assert.False(RecruitmentEnterpriseConfigRules.ResolveSlaEnabled(new RecruitmentEnterpriseConfigCommand { SlaTracking = false }, legacy));
        Assert.True(RecruitmentEnterpriseConfigRules.ResolveSlaEnabled(new RecruitmentEnterpriseConfigCommand { EnableSla = true }, null));
        Assert.True(RecruitmentEnterpriseConfigRules.ResolveAutomationEnabled(new RecruitmentEnterpriseConfigCommand { Automation = true }, null));
    }

    [Fact]
    public async Task ResetToDefaults_resets_all_capabilities_and_company_configuration_values()
    {
        const string companyId = "tenant-reset";
        var existing = new RecruitmentEnterpriseConfig
        {
            ConfigId = "config-1",
            CompanyId = companyId,
            RecruitmentMode = "Enterprise",
            EnableSla = true,
            EnableAutomation = true,
            ApplicationReview = false,
            HiringManagerReview = true,
            InterviewRounds = true,
            ParallelPanels = true,
            Scorecards = true,
            OfferApproval = true,
            OfferNegotiation = true,
            CandidateEmailActions = false,
            Requisitions = true,
            SlaTracking = true,
            Automation = true,
            InternalMobility = true,
            RehireLinks = true,
            AutoAdvanceOnScreening = false,
            RequireInterviewBeforeDecision = true,
            RequirePreboardingBeforeHandoff = true,
            AllowMultiVacancyApplications = false,
            AllowJobWorkflowCustomization = true,
            ScreeningCriteria = ["Custom rubric"],
            ScreeningPassScore = 92,
            PreboardingChecklist = ["custom-task"],
            CandidateSourceOptions = ["Recruitment consultant", "University partner"],
            Locations = ["London"],
            DefaultTimezone = "Europe/London",
            DefaultCandidateRetentionDays = 180,
            CandidatePrivacyNoticeText = "Custom privacy notice",
            CandidatePrivacyNoticeVersion = "v3",
            CompensationSchemeName = "Executive",
            CompensationCurrency = "GBP",
            CompensationComponents = new Dictionary<string, decimal> { ["Equity"] = 5000 },
            OfferLetterSubjectTemplate = "Custom subject",
            OfferLetterBodyTemplate = "<p>Custom body</p>",
            ConfigSource = "CompanyOverride",
            OverriddenFields = ["RecruitmentMode", "Scorecards"],
        };

        RecruitmentEnterpriseConfig? persisted = null;
        var configRepository = new Mock<IMongoDbRepository<RecruitmentEnterpriseConfig>>();
        configRepository
            .Setup(repository => repository.FirstOrDefault(
                It.IsAny<Expression<Func<RecruitmentEnterpriseConfig, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(existing);
        configRepository
            .Setup(repository => repository.Update(
                It.IsAny<FilterDefinition<RecruitmentEnterpriseConfig>>(),
                It.IsAny<RecruitmentEnterpriseConfig>()))
            .ReturnsAsync((FilterDefinition<RecruitmentEnterpriseConfig> _, RecruitmentEnterpriseConfig value) =>
            {
                persisted = value;
                return new Result { Success = true };
            });

        var service = new RecruitmentEnterpriseService(
            new Mock<IMongoDbRepository<Applicant>>().Object,
            new Mock<IMongoDbRepository<ApplicantLogs>>().Object,
            new Mock<IMongoDbRepository<RecruitmentInterview>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOffer>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOfferVersion>>().Object,
            new Mock<IMongoDbRepository<RecruitmentOfferCounterRequest>>().Object,
            new Mock<IMongoDbRepository<RecruitmentScorecard>>().Object,
            new Mock<IMongoDbRepository<RecruitmentTalentPoolMember>>().Object,
            new Mock<IMongoDbRepository<RecruitmentHandoff>>().Object,
            new Mock<IMongoDbRepository<RecruitmentExternalActionToken>>().Object,
            new Mock<IMongoDbRepository<RecruitmentStageSlaPolicy>>().Object,
            configRepository.Object,
            new Mock<IMongoDbRepository<RecruitmentAutomationOutboxEvent>>().Object,
            new Mock<IMongoDbRepository<RecruitmentRelationshipLink>>().Object,
            TenantContext(companyId));

        var result = await service.UpsertConfig(
            new RecruitmentEnterpriseConfigCommand
            {
                ResetToDefaults = true,
                IdempotencyKey = "reset-enterprise-config-001",
            },
            "admin-1");

        Assert.True(result.Success);
        Assert.NotNull(persisted);
        Assert.Equal("Simple", persisted!.RecruitmentMode);
        Assert.False(persisted.EnableSla);
        Assert.False(persisted.EnableAutomation);
        Assert.True(persisted.ApplicationReview);
        Assert.False(persisted.HiringManagerReview);
        Assert.False(persisted.InterviewRounds);
        Assert.False(persisted.ParallelPanels);
        Assert.False(persisted.Scorecards);
        Assert.False(persisted.OfferApproval);
        Assert.False(persisted.OfferNegotiation);
        Assert.True(persisted.CandidateEmailActions);
        Assert.False(persisted.Requisitions);
        Assert.False(persisted.SlaTracking);
        Assert.False(persisted.Automation);
        Assert.False(persisted.InternalMobility);
        Assert.False(persisted.RehireLinks);
        Assert.True(persisted.AutoAdvanceOnScreening);
        Assert.False(persisted.RequireInterviewBeforeDecision);
        Assert.False(persisted.RequirePreboardingBeforeHandoff);
        Assert.True(persisted.AllowMultiVacancyApplications);
        Assert.False(persisted.AllowJobWorkflowCustomization);
        Assert.Equal(["Relevant experience", "Required skills", "Communication"], persisted.ScreeningCriteria);
        Assert.Equal(60, persisted.ScreeningPassScore);
        Assert.Equal(["candidate-details", "documents", "verification"], persisted.PreboardingChecklist);
        Assert.Equal(PublicCareerApplicationRules.CandidateSourceOptions, persisted.CandidateSourceOptions);
        Assert.Empty(persisted.Locations);
        Assert.Equal("UTC", persisted.DefaultTimezone);
        Assert.Equal(730, persisted.DefaultCandidateRetentionDays);
        Assert.Null(persisted.CandidatePrivacyNoticeText);
        Assert.Null(persisted.CandidatePrivacyNoticeVersion);
        Assert.Equal("Standard", persisted.CompensationSchemeName);
        Assert.Equal("INR", persisted.CompensationCurrency);
        Assert.Empty(persisted.CompensationComponents);
        Assert.Equal("Formal Offer of Employment: [JobTitle] - [CandidateName]", persisted.OfferLetterSubjectTemplate);
        Assert.Equal("SystemDefault", persisted.ConfigSource);
        Assert.Empty(persisted.OverriddenFields);

        var customizationResult = await service.UpsertConfig(
            new RecruitmentEnterpriseConfigCommand
            {
                Scorecards = true,
                IdempotencyKey = "enable-scorecard-feature-0001",
            },
            "admin-1");

        Assert.True(customizationResult.Success);
        Assert.NotNull(persisted);
        Assert.Equal("CompanyOverride", persisted!.ConfigSource);
        Assert.Equal(["Scorecards"], persisted.OverriddenFields);
        var appliedVersion = persisted.DefaultVersion;
        var nextCommandResult = await service.UpsertConfig(
            new RecruitmentEnterpriseConfigCommand
            {
                DefaultTimezone = "Europe/Berlin",
                IdempotencyKey = "set-recruitment-timezone-0001",
            },
            "admin-1");
        Assert.True(nextCommandResult.Success);
        Assert.Equal("Europe/Berlin", persisted.DefaultTimezone);
        var versionAfterNextCommand = persisted.DefaultVersion;
        Assert.True(versionAfterNextCommand > appliedVersion);

        var replayResult = await service.UpsertConfig(
            new RecruitmentEnterpriseConfigCommand
            {
                Scorecards = true,
                IdempotencyKey = "enable-scorecard-feature-0001",
            },
            "admin-1");

        Assert.True(replayResult.Success);
        Assert.Equal(versionAfterNextCommand, persisted.DefaultVersion);
        Assert.Equal("Europe/Berlin", persisted.DefaultTimezone);

        var keyReuseResult = await service.UpsertConfig(
            new RecruitmentEnterpriseConfigCommand
            {
                Scorecards = false,
                IdempotencyKey = "enable-scorecard-feature-0001",
            },
            "admin-1");

        Assert.False(keyReuseResult.Success);
        Assert.Equal(409, keyReuseResult.StatusCode);
        Assert.True(persisted.Scorecards);
    }

    private static IHttpContextAccessor TenantContext(string companyId) => new HttpContextAccessor
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("company_id", companyId), new Claim("user_id", "admin-1")],
                "TestAuth")),
        },
    };
}
