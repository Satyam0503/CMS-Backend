using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

namespace Codeji.CMS.Repository.Entities.Attendance;

/// <summary>A self-service request only; it never changes the attendance source record.</summary>
public sealed class AttendanceCorrectionRequest : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewDueAt { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? Resolution { get; set; }
    public string? ResolutionNote { get; set; }
}

/// <summary>Append-only operational audit; attendance records are never used as the audit log.</summary>
public sealed class AttendanceAuditEvent : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EventId { get; set; } = string.Empty;
    public string AttendanceUserId { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public long AttendanceVersion { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Durable notification intent. A worker can retry failed deliveries without losing the source event.</summary>
public sealed class AttendanceNotificationOutbox : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string OutboxId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string RecipientUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime AvailableAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? LastError { get; set; }
}
