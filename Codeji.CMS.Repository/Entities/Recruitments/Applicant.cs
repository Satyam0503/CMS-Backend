using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Repository.Entities.Recruitments
{
    public class Applicant : BaseClass
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string ApplicantId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public required string Phone { get; set; }
        public string VacancyId { get; set; }
        /// <summary>
        /// ActivityType : InProgress,Shorlisted,OnHold,Rejected, 
        /// </summary>
        public ActivityType ActivityType { get; set; }
        public ActivityStatus Status { get; set; }
        public string State { get; set; }
        public decimal Experience { get; set; }
        public string? CurrentCompany { get; set; }
        public string? CurrentJobTitle { get; set; }
        public int? NoticePeriodDays { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? CoverLetter { get; set; }
        /// <summary>Source selected by the candidate; self-reported, not verified.</summary>
        public string? CandidateReportedSource { get; set; }
        public string? CandidateReportedSourceDetail { get; set; }
        /// <summary>Campaign tags supplied by the public browser; treat as untrusted analytics metadata.</summary>
        public string? AttributionSource { get; set; }
        public string? AttributionMedium { get; set; }
        public string? AttributionCampaign { get; set; }
        public string? AttributionContent { get; set; }
        public string? AttributionTerm { get; set; }
        public string? ReferrerHost { get; set; }
        public DateTime? AttributionCapturedAtUtc { get; set; }
        public string ResumeUrl { get; set; }
        /// <summary>SHA-256 of the exact resume bytes that received a clean malware verdict; never return this to clients.</summary>
        [BsonIgnoreIfNull]
        [JsonIgnore]
        public string? ResumeScanSha256 { get; set; }
        /// <summary>Stored scanner verdict for the exact resume digest, used to identify legacy files requiring rescan.</summary>
        [BsonIgnoreIfNull]
        [JsonIgnore]
        public string? ResumeScanVerdict { get; set; }
        [BsonIgnoreIfNull]
        [JsonIgnore]
        public DateTime? ResumeScannedAtUtc { get; set; }
        [BsonIgnoreIfNull]
        [JsonIgnore]
        public string? ResumeQuarantineFileName { get; set; }
        [BsonIgnoreIfNull]
        [JsonIgnore]
        public DateTime? ResumeQuarantinedAtUtc { get; set; }
        public int PipelineVersion { get; set; } = 1;
        public string WorkflowSnapshot { get; set; } = string.Empty;
        public string? LastCommandIdempotencyKey { get; set; }
        public string? LastPrivacyCommandIdempotencyKey { get; set; }
        public string? LastPrivacyCommandFingerprint { get; set; }
        public string? ApplicationSubmissionIdempotencyKey { get; set; }
        public string? ApplicationSubmissionPayloadHash { get; set; }
        /// <summary>Temporary durable flag while secondary recruitment records are being redacted.</summary>
        public bool AnonymizationCleanupPending { get; set; }
        /// <summary>Temporary recovery value; cleared immediately after related email copies are redacted.</summary>
        public string? AnonymizationCleanupEmail { get; set; }
        public DateTime? AnonymizationCleanupCompletedAtUtc { get; set; }
        /// <summary>Private quarantine object pending deletion during candidate anonymization.</summary>
        [BsonIgnoreIfNull]
        [JsonIgnore]
        public string? AnonymizationCleanupResumeQuarantineFileName { get; set; }
        public int Version { get; set; } = 1;
        public string? MergedIntoApplicantId { get; set; }
        public string? MergeStatus { get; set; }
        public string ConsentStatus { get; set; } = "Unknown";
        public DateTime? ConsentGrantedAtUtc { get; set; }
        public DateTime? PrivacyNoticeAcknowledgedAtUtc { get; set; }
        public string? PrivacyNoticeVersion { get; set; }
        public string? PrivacyNoticeText { get; set; }
        public string? PrivacyNoticeAcknowledgementSource { get; set; }
        public string? PublicResumeUploadTokenId { get; set; }
        public string? PublicResumeUploadSha256 { get; set; }
        public DateTime? RetentionUntilUtc { get; set; }
        /// <summary>Candidate retention is transferred to employee-data retention after a completed hiring handoff.</summary>
        public string RetentionDisposition { get; set; } = "Candidate";
        public DateTime? RetentionDispositionAtUtc { get; set; }
        public DateTime? AnonymizedAtUtc { get; set; }

        /// <summary>Recruitment final interview benchmark score (0–100%) stored at hiring to measure ongoing performance.</summary>
        public int? FinalInterviewScore { get; set; }
        /// <summary>Recruitment final interview rating (1.0–5.0 scale) aggregated across non-HR evaluation rounds.</summary>
        public decimal? FinalInterviewRating { get; set; }
        /// <summary>Consensus recommendation from evaluation rounds (Strong Hire, Hire, Hold, No Hire).</summary>
        public string? FinalInterviewRecommendation { get; set; }
        /// <summary>Number of substantive evaluation rounds rated.</summary>
        public int? EvaluatedRoundsCount { get; set; }
        public DateTime? FinalScoreCalculatedAtUtc { get; set; }
    }
}
