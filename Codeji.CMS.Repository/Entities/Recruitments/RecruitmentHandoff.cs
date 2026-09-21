using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

namespace Codeji.CMS.Repository.Entities.Recruitments;

/// <summary>Idempotent audit record for a candidate-to-employee handoff.</summary>
public sealed class RecruitmentHandoff : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string HandoffId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string EmployeeUserId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Preboarding";
    public DateTime HandoffAtUtc { get; set; } = DateTime.UtcNow;
}
