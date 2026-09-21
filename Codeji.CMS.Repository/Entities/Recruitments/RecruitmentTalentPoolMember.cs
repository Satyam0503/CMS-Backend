using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

namespace Codeji.CMS.Repository.Entities.Recruitments;

public sealed class RecruitmentTalentPoolMember : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string MemberId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string PoolName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string? Notes { get; set; }
    public string Status { get; set; } = RecruitmentTalentPoolStatus.Active;
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}

public static class RecruitmentTalentPoolStatus
{
    public const string Active = "Active";
    public const string Archived = "Archived";
}