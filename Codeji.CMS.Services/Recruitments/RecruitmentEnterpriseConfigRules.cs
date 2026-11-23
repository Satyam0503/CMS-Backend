using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities.Recruitments;
using System.Security.Cryptography;
using System.Text.Json;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>Keeps historical recruitment switches aligned with their company capability flags.</summary>
public static class RecruitmentEnterpriseConfigRules
{
    public const int RecentCommandReceiptLimit = 100;

    public static RecruitmentEnterpriseConfig CreateDefaults() => new()
        {
            Locations = [],
            RecruitmentMode = "Simple",
            EnableSla = false,
            SlaTracking = false,
            EnableAutomation = false,
            Automation = false,
            DefaultTimezone = "UTC",
            ApplicationReview = true,
            HiringManagerReview = false,
            InterviewRounds = false,
            ParallelPanels = false,
            Scorecards = false,
            OfferApproval = false,
            OfferNegotiation = false,
            CandidateEmailActions = true,
            Requisitions = false,
            InternalMobility = false,
            RehireLinks = false,
            AutoAdvanceOnScreening = true,
            RequireInterviewBeforeDecision = false,
            RequirePreboardingBeforeHandoff = false,
            AllowMultiVacancyApplications = true,
            AllowJobWorkflowCustomization = false,
            ScreeningCriteria = ["Relevant experience", "Required skills", "Communication"],
            ScreeningPassScore = 60,
            PreboardingChecklist = ["candidate-details", "documents", "verification"],
            CandidateSourceOptions = PublicCareerApplicationRules.CandidateSourceOptions.ToList(),
            CompensationSchemeName = "Standard",
            CompensationCurrency = "INR",
            CompensationComponents = [],
            DefaultCandidateRetentionDays = 730,
            CandidatePrivacyNoticeText = null,
            CandidatePrivacyNoticeVersion = null,
            OfferLetterSubjectTemplate = "Formal Offer of Employment: [JobTitle] - [CandidateName]",
            OfferLetterBodyTemplate = "<p>Dear [CandidateName],</p><p>We are pleased to extend this formal offer of employment for the position of <strong>[JobTitle]</strong>.</p><p><strong>Compensation (Total CTC):</strong> [Currency] [TotalCtc]</p><p>Please review the terms and submit your response using the secure candidate link in this email.</p><p>Sincerely,<br/><strong>Human Resources & Talent Acquisition</strong></p>"
        };

    public static string HashCommand(RecruitmentEnterpriseConfigCommand command)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(command));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCanonical(document.RootElement, writer);
        }
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }

    private static void WriteCanonical(JsonElement value, Utf8JsonWriter writer)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray()) WriteCanonical(item, writer);
                writer.WriteEndArray();
                break;
            default:
                value.WriteTo(writer);
                break;
        }
    }

    public static List<string> GetOverriddenFields(RecruitmentEnterpriseConfig config)
    {
        var defaults = CreateDefaults();

        var changed = new List<string>();
        void AddIf(bool differs, string name)
        {
            if (differs) changed.Add(name);
        }

        AddIf(!config.Locations.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).SequenceEqual(defaults.Locations, StringComparer.OrdinalIgnoreCase), nameof(config.Locations));
        AddIf(!string.Equals(config.RecruitmentMode, defaults.RecruitmentMode, StringComparison.Ordinal), nameof(config.RecruitmentMode));
        AddIf(config.SlaTracking != defaults.SlaTracking || config.EnableSla != defaults.EnableSla, "SlaTracking");
        AddIf(config.Automation != defaults.Automation || config.EnableAutomation != defaults.EnableAutomation, "Automation");
        AddIf(!string.Equals(config.DefaultTimezone ?? "UTC", defaults.DefaultTimezone, StringComparison.Ordinal), nameof(config.DefaultTimezone));
        AddIf(config.ApplicationReview != defaults.ApplicationReview, nameof(config.ApplicationReview));
        AddIf(config.HiringManagerReview != defaults.HiringManagerReview, nameof(config.HiringManagerReview));
        AddIf(config.InterviewRounds != defaults.InterviewRounds, nameof(config.InterviewRounds));
        AddIf(config.ParallelPanels != defaults.ParallelPanels, nameof(config.ParallelPanels));
        AddIf(config.Scorecards != defaults.Scorecards, nameof(config.Scorecards));
        AddIf(config.OfferApproval != defaults.OfferApproval, nameof(config.OfferApproval));
        AddIf(config.OfferNegotiation != defaults.OfferNegotiation, nameof(config.OfferNegotiation));
        AddIf(config.CandidateEmailActions != defaults.CandidateEmailActions, nameof(config.CandidateEmailActions));
        AddIf(config.Requisitions != defaults.Requisitions, nameof(config.Requisitions));
        AddIf(config.InternalMobility != defaults.InternalMobility, nameof(config.InternalMobility));
        AddIf(config.RehireLinks != defaults.RehireLinks, nameof(config.RehireLinks));
        AddIf(config.AutoAdvanceOnScreening != defaults.AutoAdvanceOnScreening, nameof(config.AutoAdvanceOnScreening));
        AddIf(config.RequireInterviewBeforeDecision != defaults.RequireInterviewBeforeDecision, nameof(config.RequireInterviewBeforeDecision));
        AddIf(config.RequirePreboardingBeforeHandoff != defaults.RequirePreboardingBeforeHandoff, nameof(config.RequirePreboardingBeforeHandoff));
        AddIf(config.AllowMultiVacancyApplications != defaults.AllowMultiVacancyApplications, nameof(config.AllowMultiVacancyApplications));
        AddIf(config.AllowJobWorkflowCustomization != defaults.AllowJobWorkflowCustomization, nameof(config.AllowJobWorkflowCustomization));
        AddIf(!config.ScreeningCriteria.SequenceEqual(defaults.ScreeningCriteria, StringComparer.OrdinalIgnoreCase), nameof(config.ScreeningCriteria));
        AddIf(config.ScreeningPassScore != defaults.ScreeningPassScore, nameof(config.ScreeningPassScore));
        AddIf(!config.PreboardingChecklist.SequenceEqual(defaults.PreboardingChecklist, StringComparer.OrdinalIgnoreCase), nameof(config.PreboardingChecklist));
        AddIf(!config.CandidateSourceOptions.SequenceEqual(defaults.CandidateSourceOptions, StringComparer.OrdinalIgnoreCase), nameof(config.CandidateSourceOptions));
        AddIf(!string.Equals(config.CompensationSchemeName, defaults.CompensationSchemeName, StringComparison.Ordinal), nameof(config.CompensationSchemeName));
        AddIf(!string.Equals(config.CompensationCurrency, defaults.CompensationCurrency, StringComparison.Ordinal), nameof(config.CompensationCurrency));
        AddIf(config.CompensationComponents.Count != 0, nameof(config.CompensationComponents));
        AddIf(config.DefaultCandidateRetentionDays != defaults.DefaultCandidateRetentionDays, nameof(config.DefaultCandidateRetentionDays));
        AddIf(!string.IsNullOrWhiteSpace(config.CandidatePrivacyNoticeText), nameof(config.CandidatePrivacyNoticeText));
        AddIf(!string.IsNullOrWhiteSpace(config.CandidatePrivacyNoticeVersion), nameof(config.CandidatePrivacyNoticeVersion));
        AddIf(!string.Equals(config.OfferLetterSubjectTemplate, defaults.OfferLetterSubjectTemplate, StringComparison.Ordinal), nameof(config.OfferLetterSubjectTemplate));
        AddIf(!string.Equals(config.OfferLetterBodyTemplate, defaults.OfferLetterBodyTemplate, StringComparison.Ordinal), nameof(config.OfferLetterBodyTemplate));
        return changed;
    }

    public static void ApplyDefaults(RecruitmentEnterpriseConfig config)
    {
        var defaults = CreateDefaults();
        config.Locations = defaults.Locations;
        config.RecruitmentMode = defaults.RecruitmentMode;
        config.EnableSla = defaults.EnableSla;
        config.SlaTracking = defaults.SlaTracking;
        config.EnableAutomation = defaults.EnableAutomation;
        config.Automation = defaults.Automation;
        config.DefaultTimezone = defaults.DefaultTimezone;
        config.ApplicationReview = defaults.ApplicationReview;
        config.HiringManagerReview = defaults.HiringManagerReview;
        config.InterviewRounds = defaults.InterviewRounds;
        config.ParallelPanels = defaults.ParallelPanels;
        config.Scorecards = defaults.Scorecards;
        config.OfferApproval = defaults.OfferApproval;
        config.OfferNegotiation = defaults.OfferNegotiation;
        config.CandidateEmailActions = defaults.CandidateEmailActions;
        config.Requisitions = defaults.Requisitions;
        config.InternalMobility = defaults.InternalMobility;
        config.RehireLinks = defaults.RehireLinks;
        config.AutoAdvanceOnScreening = defaults.AutoAdvanceOnScreening;
        config.RequireInterviewBeforeDecision = defaults.RequireInterviewBeforeDecision;
        config.RequirePreboardingBeforeHandoff = defaults.RequirePreboardingBeforeHandoff;
        config.AllowMultiVacancyApplications = defaults.AllowMultiVacancyApplications;
        config.AllowJobWorkflowCustomization = defaults.AllowJobWorkflowCustomization;
        config.ScreeningCriteria = defaults.ScreeningCriteria;
        config.ScreeningPassScore = defaults.ScreeningPassScore;
        config.PreboardingChecklist = defaults.PreboardingChecklist;
        config.CandidateSourceOptions = defaults.CandidateSourceOptions;
        config.CompensationSchemeName = defaults.CompensationSchemeName;
        config.CompensationCurrency = defaults.CompensationCurrency;
        config.CompensationComponents = defaults.CompensationComponents;
        config.DefaultCandidateRetentionDays = defaults.DefaultCandidateRetentionDays;
        config.CandidatePrivacyNoticeText = defaults.CandidatePrivacyNoticeText;
        config.CandidatePrivacyNoticeVersion = defaults.CandidatePrivacyNoticeVersion;
        config.OfferLetterSubjectTemplate = defaults.OfferLetterSubjectTemplate;
        config.OfferLetterBodyTemplate = defaults.OfferLetterBodyTemplate;
    }

    public static RecruitmentConfigCommandReceipt? FindCommandReceipt(RecruitmentEnterpriseConfig config, string idempotencyKey) =>
        config.RecentCommandReceipts?.FirstOrDefault(receipt =>
            string.Equals(receipt.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));

    public static void RecordCommandReceipt(
        RecruitmentEnterpriseConfig config,
        string idempotencyKey,
        string payloadHash,
        DateTime createdAtUtc)
    {
        config.RecentCommandReceipts ??= [];
        config.RecentCommandReceipts.RemoveAll(receipt =>
            string.Equals(receipt.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));
        config.RecentCommandReceipts.Add(new RecruitmentConfigCommandReceipt
        {
            IdempotencyKey = idempotencyKey,
            PayloadHash = payloadHash,
            CreatedAtUtc = createdAtUtc,
        });
        if (config.RecentCommandReceipts.Count > RecentCommandReceiptLimit)
            config.RecentCommandReceipts.RemoveRange(0, config.RecentCommandReceipts.Count - RecentCommandReceiptLimit);
    }

    public static FilterDefinition<RecruitmentEnterpriseConfig> BuildVersionFilter(
        string companyId,
        string configId,
        int expectedVersion)
    {
        var identity = Builders<RecruitmentEnterpriseConfig>.Filter.Eq(config => config.CompanyId, companyId) &
            Builders<RecruitmentEnterpriseConfig>.Filter.Eq(config => config.ConfigId, configId);
        var version = Builders<RecruitmentEnterpriseConfig>.Filter.Eq(config => config.DefaultVersion, expectedVersion);
        if (expectedVersion <= 1)
        {
            version |= Builders<RecruitmentEnterpriseConfig>.Filter.Eq(config => config.DefaultVersion, 0) |
                Builders<RecruitmentEnterpriseConfig>.Filter.Exists(config => config.DefaultVersion, false);
        }
        return identity & version;
    }

    public static bool ResolveSlaEnabled(RecruitmentEnterpriseConfigCommand command, RecruitmentEnterpriseConfig? current) =>
        command.SlaTracking ?? command.EnableSla ?? (current?.SlaTracking == true || current?.EnableSla == true);

    public static bool ResolveAutomationEnabled(RecruitmentEnterpriseConfigCommand command, RecruitmentEnterpriseConfig? current) =>
        command.Automation ?? command.EnableAutomation ?? (current?.Automation == true || current?.EnableAutomation == true);

    public static RecruitmentEnterpriseConfig NormalizeHistoricalSwitches(RecruitmentEnterpriseConfig config)
    {
        config.SlaTracking = config.SlaTracking || config.EnableSla;
        config.EnableSla = config.SlaTracking;
        config.Automation = config.Automation || config.EnableAutomation;
        config.EnableAutomation = config.Automation;
        return config;
    }
}
