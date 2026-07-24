namespace Codeji.CMS.DTO.PublicCareers;

public sealed class PublicJobSearchRequest
{
    public int PageNo { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? EmploymentType { get; set; }
    public string? WorkplaceType { get; set; }
    public int? JobType { get; set; }
    public decimal? ExperienceMin { get; set; }
    public decimal? ExperienceMax { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? Currency { get; set; }
    public string? Department { get; set; }
    public string? Industry { get; set; }
    public string? RoleCategory { get; set; }
    public string? Education { get; set; }
    public string? Skills { get; set; }
    public int? DatePostedDays { get; set; }
    public string? CompanySize { get; set; }
    public bool? Featured { get; set; }
    public bool? UrgentHiring { get; set; }
    public bool? WalkIn { get; set; }
    public string? ApplicationMode { get; set; }
    public string? Sort { get; set; } = "newest";
}

public class PublicJobSummaryDto
{
    public required string PublicJobId { get; init; }
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string ShortDescription { get; init; }
    public int Vacancies { get; init; }
    public int JobType { get; init; }
    public string? Location { get; init; }
    public string? WorkplaceType { get; init; }
    public string? EmploymentType { get; init; }
    public decimal? ExperienceMin { get; init; }
    public decimal? ExperienceMax { get; init; }
    public string? Currency { get; init; }
    public decimal? SalaryMin { get; init; }
    public decimal? SalaryMax { get; init; }
    public List<string> Skills { get; init; } = [];
    public DateTime? PublishedAt { get; init; }
    public DateTime? ApplicationDeadline { get; init; }
    public required string ApplicationMode { get; init; }
    public string? ExternalApplicationUrl { get; init; }
    public bool CanApply { get; init; }
    public string? ApplicationClosedReason { get; init; }
    public required string CompanyPublicCode { get; init; }
    public required string CompanyName { get; init; }
    public required string CompanySlug { get; init; }
    public string? CompanyLogo { get; init; }
    public string? Summary { get; init; }
    public string? Department { get; init; }
    public string? Industry { get; init; }
    public string? RoleCategory { get; init; }
    public string? EducationRequirement { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsUrgentHiring { get; init; }
    public bool IsWalkIn { get; init; }
    public bool ShowSalary { get; init; }
}

public sealed class PublicJobDetailsDto : PublicJobSummaryDto
{
    public required string Description { get; init; }
    public string? ReferenceCode { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? FunctionalArea { get; init; }
    public List<string> Responsibilities { get; init; } = [];
    public List<string> RequiredSkills { get; init; } = [];
    public List<string> PreferredSkills { get; init; } = [];
    public List<string> Benefits { get; init; } = [];
    public string? ShiftType { get; init; }
    public string? WorkingDays { get; init; }
    public string? TravelRequirement { get; init; }
    public DateTime? WalkInStartAt { get; init; }
    public DateTime? WalkInEndAt { get; init; }
    public string? WalkInAddress { get; init; }
    public int? NoticePeriodMaxDays { get; init; }
}

public sealed class PublicJobApplicationRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public string? State { get; init; }
    public decimal Experience { get; init; }
}

public sealed class PublicJobApplicationResponse
{
    public required string ApplicationReference { get; init; }
    public required string ResumeUploadToken { get; init; }
    public DateTime TokenExpiresAt { get; init; }
    public required string JobTitle { get; init; }
    public required string CompanyName { get; init; }
}

public sealed class PublicResumeUploadResult
{
    public required string ApplicationReference { get; init; }
}

public sealed class PublicResumeUploadRequest
{
    public required string Token { get; init; }
}
