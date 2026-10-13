using Codeji.CMS.Repository.Entities;
using MongoDB.Bson.Serialization.Attributes;
namespace Codeji.CMS.Repository.Entities.Recruitments;
public sealed class RecruitmentJobApplication : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string ApplicationId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string VacancyId { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = "New";
    public int PipelineVersion { get; set; } = 1;
    public string PipelineSnapshot { get; set; } = string.Empty;
    public List<RecruitmentApplicationStageEvent> StageHistory { get; set; } = [];
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}
public sealed class RecruitmentApplicationStageEvent
{
    public string FromStage { get; set; } = string.Empty;
    public string ToStage { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
