using Codeji.CMS.Repository.Entities;
using MongoDB.Bson.Serialization.Attributes;
namespace Codeji.CMS.Repository.Entities.Recruitments;
public sealed class RecruitmentOnboardingCase : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string CaseId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string EmployeeUserId { get; set; } = string.Empty;
    public string Status { get; set; } = RecruitmentOnboardingStatus.PendingActivation;
    public List<RecruitmentOnboardingTask> Tasks { get; set; } = [];
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}
public sealed class RecruitmentOnboardingTask
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? AssigneeUserId { get; set; }
}
public static class RecruitmentOnboardingStatus
{
    public const string PendingActivation = "PendingActivation";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
}
