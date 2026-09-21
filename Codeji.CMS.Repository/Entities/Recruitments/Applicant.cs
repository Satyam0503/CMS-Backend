using MongoDB.Bson.Serialization.Attributes;
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
        public string ResumeUrl { get; set; }
        public int PipelineVersion { get; set; } = 1;
        public string WorkflowSnapshot { get; set; } = string.Empty;
        public string? LastCommandIdempotencyKey { get; set; }
        public int Version { get; set; } = 1;
        public string? MergedIntoApplicantId { get; set; }
        public string? MergeStatus { get; set; }
        public string ConsentStatus { get; set; } = "Unknown";
        public DateTime? ConsentGrantedAtUtc { get; set; }
        public DateTime? RetentionUntilUtc { get; set; }
        public DateTime? AnonymizedAtUtc { get; set; }

    }
}




