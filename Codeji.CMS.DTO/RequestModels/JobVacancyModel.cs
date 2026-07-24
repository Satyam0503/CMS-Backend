namespace Codeji.CMS.DTO.RequestModels
{
    public class JobVacancyModel
    {
        public string JobId { get; set; }
        public string Title { get; set; }
        public int Vacancies { get; set; }
        public int JobType { get; set; }
        public bool Status { get; set; }
        public string Description { get; set; }
        public string? PublicJobId { get; set; }
        public string? Slug { get; set; }
        public string? ReferenceCode { get; set; }
        public bool? PublishToCareerPortal { get; set; }
        public bool? PublishToMasterPortal { get; set; }
        public string? ApplicationMode { get; set; }
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
