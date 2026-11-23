using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;
namespace Codeji.CMS.Repository.Entities.Recruitments;
public sealed class RecruitmentHiringDecision : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string DecisionId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string VacancyId { get; set; } = string.Empty;
    public string Decision { get; set; } = RecruitmentHiringDecisionStatus.Hold;
    public string? HiringManagerUserId { get; set; }
    public string? ManagerReviewStatus { get; set; }
    public string? ManagerReviewRequestedByUserId { get; set; }
    public DateTime? ManagerReviewRequestedAtUtc { get; set; }
    public string? ManagerReviewNotes { get; set; }
    public string? ManagerReviewedByUserId { get; set; }
    public DateTime? ManagerReviewedAtUtc { get; set; }
    public string? Notes { get; set; }
    public string? LastCommandIdempotencyKey { get; set; }
    /// <summary>Durable stage projection work; older decisions default to not requiring recovery.</summary>
    public string StageSyncStatus { get; set; } = "NotRequired";
    public string? StageSyncFromStage { get; set; }
    public string? StageSyncTargetStage { get; set; }
    public int Version { get; set; } = 1;
}
public static class RecruitmentHiringDecisionStatus
{
    public const string PendingManagerReview = "PendingManagerReview";
    public const string Approve = "Approve";
    public const string Hold = "Hold";
    public const string Reject = "Reject";
}

public static class RecruitmentManagerReviewStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
