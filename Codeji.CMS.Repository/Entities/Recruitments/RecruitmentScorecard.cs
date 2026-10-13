using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

namespace Codeji.CMS.Repository.Entities.Recruitments;

public sealed class RecruitmentScorecard : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string ScorecardId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string VacancyId { get; set; } = string.Empty;
    public string? InterviewId { get; set; }
    public int RoundNumber { get; set; } = 1;
    public DateTime? FeedbackDueAtUtc { get; set; }
    public string? Recommendation { get; set; }
    public string ReviewerUserId { get; set; } = string.Empty;
    public List<RecruitmentScorecardCriterion> Criteria { get; set; } = [];
    public string Status { get; set; } = RecruitmentScorecardStatus.Draft;
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}

public sealed class RecruitmentScorecardCriterion
{
    public string Name { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Notes { get; set; }
}

public static class RecruitmentScorecardStatus
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string Locked = "Locked";
}

