using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;
namespace Codeji.CMS.Repository.Entities.Recruitments;
public sealed class RecruitmentCandidateShare : BaseClass
{
    [BsonId(IdGenerator=typeof(UniqueIdGenerator))] public string ShareId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string SharedWithUserId { get; set; } = string.Empty;
    public string Scope { get; set; } = "View";
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? LastCommandIdempotencyKey { get; set; }
}
