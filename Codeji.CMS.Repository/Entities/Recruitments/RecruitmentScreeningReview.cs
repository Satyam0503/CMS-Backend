using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

namespace Codeji.CMS.Repository.Entities.Recruitments;

public sealed class RecruitmentScreeningReview : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string ReviewId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string VacancyId { get; set; } = string.Empty;
    public string ReviewerUserId { get; set; } = string.Empty;
    public string? ResumeVersion { get; set; }
    public int Score { get; set; }
    public string Decision { get; set; } = RecruitmentScreeningDecision.Pending;
    public string? RejectionReason { get; set; }
    public string? Evidence { get; set; }
    public List<RecruitmentScreeningCriterion> Criteria { get; set; } = [];
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}

public sealed class RecruitmentScreeningCriterion
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Notes { get; set; }
}

public static class RecruitmentScreeningDecision
{
    public const string Pending = "Pending";
    public const string Pass = "Pass";
    public const string Hold = "Hold";
    public const string Reject = "Reject";
}