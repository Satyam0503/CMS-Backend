namespace Codeji.CMS.DTO.CareerPortal;

public sealed class PublicCompanyProfileDto
{
    public string PublicCompanyCode { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? AboutCompanyHtml { get; init; }
    public string? LogoUrl { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? Industry { get; init; }
    public string? CompanyType { get; init; }
    public string? CompanySize { get; init; }
    public int? FoundedYear { get; init; }
    public string? Headquarters { get; init; }
    public List<string> OfficeLocations { get; init; } = [];
    public string? Mission { get; init; }
    public string? Vision { get; init; }
    public List<string> Values { get; init; } = [];
    public List<string> Benefits { get; init; } = [];
    public List<string> Technologies { get; init; } = [];
    public Dictionary<string, string> SocialLinks { get; init; } = [];
    public string? WorkCultureHtml { get; init; }
    public string? HiringProcessHtml { get; init; }
    public string? DiversityStatementHtml { get; init; }
    public bool IsPublished { get; init; }
}

public sealed class UpsertPublicCompanyProfileRequest
{
    public string? DisplayName { get; init; }
    public string? ShortDescription { get; init; }
    public string? AboutCompanyHtml { get; init; }
    public string? LogoUrl { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? Industry { get; init; }
    public string? CompanyType { get; init; }
    public string? CompanySize { get; init; }
    public int? FoundedYear { get; init; }
    public string? Headquarters { get; init; }
    public List<string> OfficeLocations { get; init; } = [];
    public string? Mission { get; init; }
    public string? Vision { get; init; }
    public List<string> Values { get; init; } = [];
    public List<string> Benefits { get; init; } = [];
    public List<string> Technologies { get; init; } = [];
    public Dictionary<string, string> SocialLinks { get; init; } = [];
    public string? WorkCultureHtml { get; init; }
    public string? HiringProcessHtml { get; init; }
    public string? DiversityStatementHtml { get; init; }
    public bool IsPublished { get; init; }
}

public sealed class CareerSubscribeRequest
{
    public string Email { get; init; } = string.Empty;
    public string NotificationFrequency { get; init; } = "Instant";
    public string PreferredLanguage { get; init; } = "en";
    public string? ConsentSource { get; init; }
    public string? PrivacyPolicyVersion { get; init; }
}

public sealed class CareerTokenRequest { public string Token { get; init; } = string.Empty; }
public sealed class CareerSessionResponse { public string SessionToken { get; init; } = string.Empty; public DateTime ExpiresAt { get; init; } }
public sealed class CareerPreferencesDto { public string NotificationFrequency { get; init; } = "Instant"; public string PreferredLanguage { get; init; } = "en"; public bool IsActive { get; init; } }
public sealed class UpdateCareerPreferencesRequest { public string NotificationFrequency { get; init; } = "Instant"; public string PreferredLanguage { get; init; } = "en"; public bool IsActive { get; init; } = true; }

public sealed class SaveJobRequest { public string? VisitorToken { get; init; } public string? SubscriberSessionToken { get; init; } }
public sealed class MergeAnonymousSavesRequest { public string VisitorToken { get; init; } = string.Empty; public string SubscriberSessionToken { get; init; } = string.Empty; }
public sealed class FollowCompanyRequest { public string SubscriberSessionToken { get; init; } = string.Empty; public bool NotifyForNewJobs { get; init; } = true; public bool NotifyForJobUpdates { get; init; } }

public sealed class JobAlertRequest
{
    public string SubscriberSessionToken { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? SearchText { get; init; }
    public List<string> Locations { get; init; } = [];
    public List<string> EmploymentTypes { get; init; } = [];
    public List<string> WorkplaceTypes { get; init; } = [];
    public List<int> JobTypes { get; init; } = [];
    public List<string> Industries { get; init; } = [];
    public List<string> Departments { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public decimal? ExperienceMin { get; init; }
    public decimal? ExperienceMax { get; init; }
    public decimal? SalaryMin { get; init; }
    public string? Currency { get; init; }
    public string NotificationFrequency { get; init; } = "Instant";
}

public sealed class CareerAnalyticsRequest
{
    public string EventType { get; init; } = string.Empty;
    public string? VisitorToken { get; init; }
    public string? PublicJobId { get; init; }
    public string? PublicCompanyCode { get; init; }
}

public sealed class SavedCareerJobDto
{
    public string PublicJobId { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public List<string> Locations { get; init; } = [];
    public DateTime SavedAt { get; init; }
    public DateTime? ApplicationDeadline { get; init; }
}

public sealed class CompanyFollowStatusDto
{
    public bool IsFollowing { get; init; }
    public bool NotifyForNewJobs { get; init; }
    public bool NotifyForJobUpdates { get; init; }
}

public sealed class JobAlertDto
{
    public string JobAlertSubscriptionId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? SearchText { get; init; }
    public List<string> Locations { get; init; } = [];
    public List<string> EmploymentTypes { get; init; } = [];
    public List<string> WorkplaceTypes { get; init; } = [];
    public List<int> JobTypes { get; init; } = [];
    public List<string> Industries { get; init; } = [];
    public List<string> Departments { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public decimal? ExperienceMin { get; init; }
    public decimal? ExperienceMax { get; init; }
    public decimal? SalaryMin { get; init; }
    public string? Currency { get; init; }
    public string NotificationFrequency { get; init; } = "Instant";
    public bool IsActive { get; init; }
}

public sealed class VisitorPreferenceRequest
{
    public string VisitorToken { get; init; } = string.Empty;
    public bool DismissSubscribePopup { get; init; }
    public string? LastVisitedCompanyCode { get; init; }
    public int? SuppressDays { get; init; }
}

public sealed class VisitorPreferenceDto
{
    public bool SuppressSubscribePopup { get; init; }
    public DateTime? DoNotShowUntil { get; init; }
    public int PopupDisplayCount { get; init; }
}
