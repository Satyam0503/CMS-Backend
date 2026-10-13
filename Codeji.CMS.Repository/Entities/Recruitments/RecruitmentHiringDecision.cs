using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;
namespace Codeji.CMS.Repository.Entities.Recruitments;
public sealed class RecruitmentHiringDecision : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string DecisionId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string VacancyId { get; set; } = string.Empty;
    public string Decision { get; set; } = RecruitmentHiringDecisionStatus.Hold;
    public string? Notes { get; set; }
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}
public static class RecruitmentHiringDecisionStatus
{
    public const string Approve = "Approve";
    public const string Hold = "Hold";
    public const string Reject = "Reject";
}
