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
        public string? Summary { get; set; }
        public string? Department { get; set; }
        public string? FunctionalArea { get; set; }
        public string? Industry { get; set; }
        public string? RoleCategory { get; set; }
        public string? EducationRequirement { get; set; }
        public List<string> Responsibilities { get; set; } = [];
        public List<string> RequiredSkills { get; set; } = [];
        public List<string> PreferredSkills { get; set; } = [];
        public List<string> Benefits { get; set; } = [];
        public List<string> Keywords { get; set; } = [];
        public string? ShiftType { get; set; }
        public string? WorkingDays { get; set; }
        public string? TravelRequirement { get; set; }
        public bool? IsFeatured { get; set; }
        public bool? IsUrgentHiring { get; set; }
        public bool? IsWalkIn { get; set; }
        public DateTime? WalkInStartAt { get; set; }
        public DateTime? WalkInEndAt { get; set; }
        public string? WalkInAddress { get; set; }
        public string? RecruiterContactEmail { get; set; }
        public int? NoticePeriodMaxDays { get; set; }
        public bool? ShowSalary { get; set; }
    }
}
