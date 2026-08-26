using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

/// <summary>Durable, retryable downstream work for an employee-exit transition.</summary>
public sealed class EmployeeExitOutbox : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EmployeeExitOutboxId { get; set; } = string.Empty;
    public string EmployeeExitId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string RecipientUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? LastError { get; set; }
}
