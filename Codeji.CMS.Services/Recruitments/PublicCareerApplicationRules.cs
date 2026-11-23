using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Codeji.CMS.DTO.PublicCareers;
using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Recruitments;

public static class PublicCareerApplicationRules
{
    private static readonly Regex CandidatePhoneFormat = new(@"^\+?[0-9\s().-]{7,25}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    public static readonly IReadOnlyList<string> CandidateSourceOptions =
    ["Career site", "Employee referral", "Job board", "Social media", "Search engine", "Recruitment agency", "Event", "Other", "Prefer not to say"];

    private static readonly TimeSpan ResumeUploadClaimLease = TimeSpan.FromMinutes(10);

    public static bool IsCompletedResumeUploadReplay(PublicApplicationToken token, string fileSha256) =>
        token.IsUsed && token.UploadCompletedAtUtc.HasValue &&
        string.Equals(token.UploadSha256, fileSha256, StringComparison.Ordinal);

    public static bool IsResumeUploadClaimActive(PublicApplicationToken token, DateTime nowUtc) =>
        token.IsProcessing && token.ProcessingStartedAtUtc is DateTime startedAtUtc &&
        startedAtUtc > nowUtc - ResumeUploadClaimLease;

    public static bool CanAcceptPublicResumeUpload(Applicant? applicant) =>
        applicant is not null && !applicant.IsDeleted &&
        !string.Equals(applicant.ConsentStatus, "Anonymized", StringComparison.OrdinalIgnoreCase);

    public static DateTime ResumeUploadClaimExpiry(DateTime nowUtc) => nowUtc - ResumeUploadClaimLease;

    public static bool IsValidCandidatePhone(string? phone)
    {
        var value = phone?.Trim();
        if (string.IsNullOrWhiteSpace(value) || !CandidatePhoneFormat.IsMatch(value)) return false;
        var digitCount = value.Count(char.IsDigit);
        return digitCount is >= 7 and <= 15;
    }

    public static bool IsValidCandidateExperience(decimal years) => years is >= 0m and <= 80m;

    public static bool IsValidCandidateNoticePeriod(int? days) => !days.HasValue || days.Value is >= 0 and <= 3650;

    /// <summary>
    /// Validates public candidate input at the service boundary as well as the API model-binding boundary.
    /// Keep the explicit limits here so direct service callers cannot bypass request annotations.
    /// </summary>
    public static string? ValidateApplicationRequest(PublicJobApplicationRequest? request)
    {
        if (request is null) return "Application details are required.";
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || !Regex.IsMatch(request.IdempotencyKey.Trim(), "^[A-Za-z0-9._:-]{12,100}$")) return "A valid application submission key is required.";
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName)) return "First and last name are required.";
        if (!Regex.IsMatch(request.FirstName.Trim(), "^[^=~<>;`%]{1,100}$") || !Regex.IsMatch(request.LastName.Trim(), "^[^=~<>;`%]{1,100}$")) return "Applicant name contains invalid characters.";
        if (request.Email is null || request.Email.Length is < 3 or > 320 || !new EmailAddressAttribute().IsValid(request.Email.Trim())) return "A valid email address is required.";
        if (!IsValidCandidatePhone(request.Phone)) return "Enter a valid phone number, including its country code when needed.";
        if (!IsValidCandidateExperience(request.Experience)) return "Experience must be between 0 and 80 years.";
        if (!IsValidCandidateNoticePeriod(request.NoticePeriodDays)) return "Notice period must be between 0 and 3650 days.";
        if (request.State?.Length > 120) return "One or more application fields exceed the allowed length.";
        if (!string.IsNullOrWhiteSpace(request.LinkedInUrl) && !IsSafeExternalUrl(request.LinkedInUrl)) return "LinkedIn URL must be a safe HTTPS URL no longer than 2048 characters.";
        if (!string.IsNullOrWhiteSpace(request.PortfolioUrl) && !IsSafeExternalUrl(request.PortfolioUrl)) return "Portfolio URL must be a safe HTTPS URL no longer than 2048 characters.";
        if (request.CurrentCompany?.Length > 200 || request.CurrentJobTitle?.Length > 200 || request.CoverLetter?.Length > 10000) return "One or more application fields exceed the allowed length.";
        if (request.CandidateReportedSource?.Length > 60 || request.CandidateReportedSourceDetail?.Length > 250 ||
            request.AttributionSource?.Length > 200 || request.AttributionMedium?.Length > 200 ||
            request.AttributionCampaign?.Length > 200 || request.AttributionContent?.Length > 200 ||
            request.AttributionTerm?.Length > 200 || request.ReferrerHost?.Length > 253 ||
            request.PrivacyNoticeVersion?.Length > 40) return "One or more application fields exceed the allowed length.";
        if (new[] { request.CandidateReportedSource, request.CandidateReportedSourceDetail, request.AttributionSource,
                request.AttributionMedium, request.AttributionCampaign, request.AttributionContent,
                request.AttributionTerm, request.ReferrerHost, request.PrivacyNoticeVersion }
            .Any(value => value?.Any(char.IsControl) == true)) return "One or more application fields contain invalid characters.";
        return null;
    }

    public static bool IsSafeExternalUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2048 || !Uri.TryCreate(value, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps && !uri.IsLoopback;
    }

    public static string? NormalizeCandidateReportedSource(string? value) =>
        CandidateSourceOptions.FirstOrDefault(option => string.Equals(option, value?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<string> EffectiveCandidateSourceOptions(IEnumerable<string>? configured) =>
        (configured ?? CandidateSourceOptions)
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Select(option => option.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray() is { Length: > 0 } options ? options : CandidateSourceOptions;

    public static string? NormalizeCandidateReportedSource(string? value, IEnumerable<string>? configured)
    {
        var options = EffectiveCandidateSourceOptions(configured);
        return options.FirstOrDefault(option => string.Equals(option, value?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsValidSourceAttribution(PublicJobApplicationRequest request, IEnumerable<string>? configuredSources = null)
    {
        var sourceOptions = EffectiveCandidateSourceOptions(configuredSources);
        if (!string.IsNullOrWhiteSpace(request.CandidateReportedSource) &&
            !sourceOptions.Contains(request.CandidateReportedSource.Trim(), StringComparer.OrdinalIgnoreCase)) return false;
        if (request.CandidateReportedSourceDetail?.Length > 250) return false;
        if (!string.IsNullOrWhiteSpace(request.CandidateReportedSourceDetail) &&
            !string.Equals(request.CandidateReportedSource?.Trim(), "Other", StringComparison.OrdinalIgnoreCase)) return false;
        if (request.AttributionSource?.Length > 200 || request.AttributionMedium?.Length > 200 ||
            request.AttributionCampaign?.Length > 200 || request.AttributionContent?.Length > 200 ||
            request.AttributionTerm?.Length > 200) return false;
        if (new[] { request.AttributionSource, request.AttributionMedium, request.AttributionCampaign, request.AttributionContent, request.AttributionTerm }
            .Any(value => value?.Any(char.IsControl) == true)) return false;
        if (string.IsNullOrWhiteSpace(request.ReferrerHost)) return true;
        var host = request.ReferrerHost.Trim();
        return host.Length <= 253 && Uri.TryCreate($"https://{host}", UriKind.Absolute, out var uri) &&
            string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase) && uri.AbsolutePath == "/" &&
            string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
    }

    public static bool IsPrivacyNoticeConfigured(string? text, string? version) =>
        !string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(version);

    public static bool CanAcceptApplications(
        bool postingIsOpen,
        string? applicationMode,
        string? privacyNoticeText,
        string? privacyNoticeVersion) =>
        postingIsOpen &&
        (string.Equals(applicationMode?.Trim(), "External", StringComparison.OrdinalIgnoreCase) ||
         IsPrivacyNoticeConfigured(privacyNoticeText, privacyNoticeVersion));

    public static void AnonymizeApplicant(Applicant applicant, DateTime nowUtc)
    {
        applicant.AnonymizationCleanupPending = true;
        applicant.AnonymizationCleanupEmail = applicant.Email;
        applicant.AnonymizationCleanupResumeQuarantineFileName = applicant.ResumeQuarantineFileName;
        applicant.AnonymizationCleanupCompletedAtUtc = null;
        applicant.ConsentStatus = "Anonymized";
        applicant.AnonymizedAtUtc = nowUtc;
        applicant.RetentionDisposition = "Anonymized";
        applicant.RetentionDispositionAtUtc = nowUtc;
        applicant.RetentionUntilUtc = null;
        applicant.ConsentGrantedAtUtc = null;
        applicant.Email = $"anonymized-{applicant.ApplicantId}@invalid.local";
        applicant.Phone = "0000000000";
        applicant.FirstName = "Anonymized";
        applicant.LastName = "Candidate";
        applicant.State = string.Empty;
        applicant.Experience = 0;
        applicant.CurrentCompany = null;
        applicant.CurrentJobTitle = null;
        applicant.NoticePeriodDays = null;
        applicant.CoverLetter = null;
        applicant.CandidateReportedSource = null;
        applicant.CandidateReportedSourceDetail = null;
        applicant.AttributionSource = null;
        applicant.AttributionMedium = null;
        applicant.AttributionCampaign = null;
        applicant.AttributionContent = null;
        applicant.AttributionTerm = null;
        applicant.ReferrerHost = null;
        applicant.AttributionCapturedAtUtc = null;
        applicant.LinkedInUrl = null;
        applicant.PortfolioUrl = null;
        applicant.PrivacyNoticeAcknowledgedAtUtc = null;
        applicant.PrivacyNoticeVersion = null;
        applicant.PrivacyNoticeText = null;
        applicant.PrivacyNoticeAcknowledgementSource = null;
        applicant.ApplicationSubmissionIdempotencyKey = null;
        applicant.ApplicationSubmissionPayloadHash = null;
        applicant.PublicResumeUploadTokenId = null;
        applicant.PublicResumeUploadSha256 = null;
        applicant.ResumeUrl = string.Empty;
        applicant.ResumeScanSha256 = null;
        applicant.ResumeScanVerdict = null;
        applicant.ResumeScannedAtUtc = null;
        applicant.ResumeQuarantineFileName = null;
        applicant.ResumeQuarantinedAtUtc = null;
    }

    public static bool AcknowledgesCurrentPrivacyNotice(bool acknowledged, string? submittedVersion, string? currentVersion) =>
        acknowledged && !string.IsNullOrWhiteSpace(currentVersion) &&
        string.Equals(submittedVersion?.Trim(), currentVersion.Trim(), StringComparison.Ordinal);

    public static void RecordPrivacyNoticeAcknowledgement(Applicant applicant, string noticeText, string noticeVersion, DateTime acknowledgedAtUtc)
    {
        applicant.PrivacyNoticeAcknowledgedAtUtc = acknowledgedAtUtc;
        applicant.PrivacyNoticeVersion = noticeVersion;
        applicant.PrivacyNoticeText = noticeText;
        applicant.PrivacyNoticeAcknowledgementSource = "career-portal";
    }

    public static bool IsWithinReapplicationCooldown(
        DateTime? existingApplicationCreatedAtUtc,
        DateTime nowUtc,
        bool allowMultiVacancyApplications,
        bool sameVacancy)
    {
        if (existingApplicationCreatedAtUtc is not DateTime createdAtUtc) return false;
        return createdAtUtc > nowUtc.AddMonths(-6) && (sameVacancy || !allowMultiVacancyApplications);
    }

    public static string HashApplicationSubmission(PublicJobApplicationRequest request, IEnumerable<string>? configuredSources = null)
    {
        var normalized = new SortedDictionary<string, string?>(StringComparer.Ordinal)
        {
            ["CoverLetter"] = Normalize(request.CoverLetter),
            ["CandidateReportedSource"] = Normalize(NormalizeCandidateReportedSource(request.CandidateReportedSource, configuredSources)),
            ["CandidateReportedSourceDetail"] = Normalize(request.CandidateReportedSourceDetail),
            ["AttributionSource"] = Normalize(request.AttributionSource),
            ["AttributionMedium"] = Normalize(request.AttributionMedium),
            ["AttributionCampaign"] = Normalize(request.AttributionCampaign),
            ["AttributionContent"] = Normalize(request.AttributionContent),
            ["AttributionTerm"] = Normalize(request.AttributionTerm),
            ["ReferrerHost"] = Normalize(request.ReferrerHost).ToLowerInvariant(),
            ["CurrentCompany"] = Normalize(request.CurrentCompany),
            ["CurrentJobTitle"] = Normalize(request.CurrentJobTitle),
            ["Email"] = Normalize(request.Email).ToLowerInvariant(),
            ["Experience"] = request.Experience.ToString("G29", CultureInfo.InvariantCulture),
            ["FirstName"] = Normalize(request.FirstName),
            ["LastName"] = Normalize(request.LastName),
            ["LinkedInUrl"] = Normalize(request.LinkedInUrl),
            ["NoticePeriodDays"] = request.NoticePeriodDays?.ToString(CultureInfo.InvariantCulture),
            ["Phone"] = Normalize(request.Phone),
            ["PortfolioUrl"] = Normalize(request.PortfolioUrl),
            ["PrivacyAcknowledged"] = request.PrivacyAcknowledged ? "true" : "false",
            ["PrivacyNoticeVersion"] = Normalize(request.PrivacyNoticeVersion),
            ["State"] = Normalize(request.State)
        };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalized))));
    }

    public static bool IsSameApplicationSubmission(Applicant applicant, PublicJobApplicationRequest request, IEnumerable<string>? configuredSources = null)
    {
        if (!string.IsNullOrWhiteSpace(applicant.ApplicationSubmissionPayloadHash))
            return string.Equals(applicant.ApplicationSubmissionPayloadHash, HashApplicationSubmission(request, configuredSources), StringComparison.Ordinal);

        // Older idempotency records predate payload hashes. Compare every persisted
        // application field before allowing a retry to reuse their submission key.
        return string.Equals(Normalize(applicant.FirstName), Normalize(request.FirstName), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.LastName), Normalize(request.LastName), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.Email).ToLowerInvariant(), Normalize(request.Email).ToLowerInvariant(), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.Phone), Normalize(request.Phone), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.State), Normalize(request.State), StringComparison.Ordinal) &&
            applicant.Experience == request.Experience * 12 &&
            string.Equals(Normalize(applicant.CurrentCompany), Normalize(request.CurrentCompany), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.CurrentJobTitle), Normalize(request.CurrentJobTitle), StringComparison.Ordinal) &&
            applicant.NoticePeriodDays == request.NoticePeriodDays &&
            string.Equals(Normalize(applicant.LinkedInUrl), Normalize(request.LinkedInUrl), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.PortfolioUrl), Normalize(request.PortfolioUrl), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.CoverLetter), Normalize(request.CoverLetter), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.CandidateReportedSource), Normalize(NormalizeCandidateReportedSource(request.CandidateReportedSource, configuredSources)), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.CandidateReportedSourceDetail), Normalize(request.CandidateReportedSourceDetail), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.AttributionSource), Normalize(request.AttributionSource), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.AttributionMedium), Normalize(request.AttributionMedium), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.AttributionCampaign), Normalize(request.AttributionCampaign), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.AttributionContent), Normalize(request.AttributionContent), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.AttributionTerm), Normalize(request.AttributionTerm), StringComparison.Ordinal) &&
            string.Equals(Normalize(applicant.ReferrerHost), Normalize(request.ReferrerHost).ToLowerInvariant(), StringComparison.Ordinal) &&
            request.PrivacyAcknowledged &&
            string.Equals(Normalize(applicant.PrivacyNoticeVersion), Normalize(request.PrivacyNoticeVersion), StringComparison.Ordinal);
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
