using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Recruitments
{
    public class JobVacancy : BaseClass
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string JobId { get; set; }
        public string PublicJobId { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ReferenceCode { get; set; }
        public string Title { get; set; }
        public int Vacancies { get; set; }
        public int JobType { get; set; }
        public bool Status { get; set; }
        public string Description { get; set; }
        public bool PublishToCareerPortal { get; set; } = true;
        public bool PublishToMasterPortal { get; set; } = false;
        public string ApplicationMode { get; set; } = "Internal";
        public string? ExternalApplicationUrl { get; set; }
        public string? Location { get; set; }
        public string? WorkplaceType { get; set; }
        public string? EmploymentType { get; set; }
        public decimal? ExperienceMin { get; set; }
        public decimal? ExperienceMax { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? Currency { get; set; }
        public List<string> Skills { get; set; } = [];
        public DateTime? PublishedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
    }
}
