using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;
namespace Codeji.CMS.Repository.Entities.Recruitments;
public sealed class RecruitmentResumeExtractionReview : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string ReviewId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string VacancyId { get; set; } = string.Empty;
    public string? ResumeVersion { get; set; }
    public Dictionary<string, string> ExtractedFields { get; set; } = [];
    public string Decision { get; set; } = RecruitmentResumeExtractionDecision.Pending;
    public string? ReviewerUserId { get; set; }
    public string? Notes { get; set; }
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}
public static class RecruitmentResumeExtractionDecision
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
