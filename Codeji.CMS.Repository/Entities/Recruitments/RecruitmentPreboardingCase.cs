using Codeji.CMS.Repository.Entities;
using MongoDB.Bson.Serialization.Attributes;
namespace Codeji.CMS.Repository.Entities.Recruitments;
public sealed class RecruitmentPreboardingCase : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string CaseId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string OfferId { get; set; } = string.Empty;
    public string Status { get; set; } = RecruitmentPreboardingStatus.PendingCandidateDetails;
    public string OwnerUserId { get; set; } = string.Empty;
    public DateTime? TargetJoiningDateUtc { get; set; }
    public List<RecruitmentPreboardingTask> Tasks { get; set; } = [];
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}
public sealed class RecruitmentPreboardingTask
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? AssigneeUserId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
public static class RecruitmentPreboardingStatus
{
    public const string PendingCandidateDetails = "PendingCandidateDetails";
    public const string InProgress = "InProgress";
    public const string ReadyForOnboarding = "ReadyForOnboarding";
    public const string Completed = "Completed";
}
