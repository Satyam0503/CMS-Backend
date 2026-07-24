using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.CareerPortal;

public sealed class PublicCompanyProfile : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string PublicCompanyProfileId { get; set; } = string.Empty;
    public string PublicCompanyCode { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ShortDescription { get; set; }
    public string? AboutCompanyHtml { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Industry { get; set; }
    public string? CompanyType { get; set; }
    public string? CompanySize { get; set; }
    public int? FoundedYear { get; set; }
    public string? Headquarters { get; set; }
    public List<string> OfficeLocations { get; set; } = [];
    public string? Mission { get; set; }
    public string? Vision { get; set; }
    public List<string> Values { get; set; } = [];
    public List<string> Benefits { get; set; } = [];
    public List<string> Technologies { get; set; } = [];
    public Dictionary<string, string> SocialLinks { get; set; } = [];
    public string? WorkCultureHtml { get; set; }
    public string? HiringProcessHtml { get; set; }
    public string? DiversityStatementHtml { get; set; }
    public bool IsPublished { get; set; }
}

public sealed class CareerSubscriber
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string SubscriberId { get; set; } = string.Empty;
    public string EmailNormalized { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; }
    public string? VerificationTokenHash { get; set; }
    public DateTime? VerificationExpiresAt { get; set; }
    public string UnsubscribeTokenHash { get; set; } = string.Empty;
    public string? SessionTokenHash { get; set; }
    public DateTime? SessionExpiresAt { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string NotificationFrequency { get; set; } = "Instant";
    public string? ConsentSource { get; set; }
    public DateTime ConsentTimestamp { get; set; }
    public string? PrivacyPolicyVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
    public DateTime? LastNotificationAt { get; set; }
}

public sealed class JobAlertSubscription
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string JobAlertSubscriptionId { get; set; } = string.Empty;
    public string SubscriberId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? SearchText { get; set; }
    public List<string> Locations { get; set; } = [];
    public List<string> EmploymentTypes { get; set; } = [];
    public List<string> WorkplaceTypes { get; set; } = [];
    public List<int> JobTypes { get; set; } = [];
    public List<string> Industries { get; set; } = [];
    public List<string> Departments { get; set; } = [];
    public List<string> Skills { get; set; } = [];
    public decimal? ExperienceMin { get; set; }
    public decimal? ExperienceMax { get; set; }
    public decimal? SalaryMin { get; set; }
    public string? Currency { get; set; }
    public string NotificationFrequency { get; set; } = "Instant";
    public bool IsActive { get; set; } = true;
    public DateTime? LastMatchedAt { get; set; }
    public DateTime? LastNotificationAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class CompanyFollower
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CompanyFollowerId { get; set; } = string.Empty;
    public string SubscriberId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string PublicCompanyCode { get; set; } = string.Empty;
    public bool NotifyForNewJobs { get; set; } = true;
    public bool NotifyForJobUpdates { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime FollowedAt { get; set; }
    public DateTime? UnfollowedAt { get; set; }
}

public sealed class SavedJob
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SavedJobId { get; set; } = string.Empty;
    public string? SubscriberId { get; set; }
    public string? AnonymousVisitorIdHash { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string PublicJobId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CareerVisitorPreference
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string VisitorPreferenceId { get; set; } = string.Empty;
    public string VisitorIdHash { get; set; } = string.Empty;
    public DateTime? SubscribePopupDismissedAt { get; set; }
    public DateTime? DoNotShowUntil { get; set; }
    public int PopupDisplayCount { get; set; }
    public string? LastVisitedCompanyCode { get; set; }
    public DateTime? LastVisitedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class CareerNotificationOutbox
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string NotificationId { get; set; } = string.Empty;
    public string SubscriberId { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string? CompanyId { get; set; }
    public string? JobId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime AvailableAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class CareerNotificationDelivery
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string DeliveryId { get; set; } = string.Empty;
    public string SubscriberId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string NotificationReason { get; set; } = string.Empty;
    public string Channel { get; set; } = "Email";
    public DateTime SentAt { get; set; }
    public string? ProviderReference { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class CareerAnalyticsEvent
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? VisitorIdHash { get; set; }
    public string? SubscriberId { get; set; }
    public string? PublicJobId { get; set; }
    public string? PublicCompanyCode { get; set; }
    public DateTime CreatedAt { get; set; }
}
