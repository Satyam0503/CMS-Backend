using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

namespace Codeji.CMS.Repository.Entities.Recruitments;

/// <summary>One-time or revocable action capability delivered by email. The raw token is never stored.</summary>
public sealed class RecruitmentExternalActionToken : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string ActionTokenId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string? VacancyId { get; set; }
    public string? InterviewId { get; set; }
    public string? OfferId { get; set; }
    public int? ResourceVersion { get; set; }
    public string? RecipientEmailHash { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public string? IssuanceKey { get; set; }
}

/// <summary>Durable recruitment email work item. The worker owns delivery and retry state.</summary>
public sealed class RecruitmentEmailOutbox : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string OutboxId { get; set; } = string.Empty;
    public string DedupeKey { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime AvailableAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? ProviderReference { get; set; }
    public string? LastError { get; set; }
}
