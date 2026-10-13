using Codeji.CMS.Repository.Entities;
using MongoDB.Bson.Serialization.Attributes;
namespace Codeji.CMS.Repository.Entities.Recruitments;
public sealed class RecruitmentInterview : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string InterviewId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string VacancyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int RoundNumber { get; set; } = 1;
    public string? RoundType { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public string Status { get; set; } = RecruitmentInterviewStatus.Draft;
    public string? Location { get; set; }
    public string? MeetingUrl { get; set; }
    public string? CalendarItemId { get; set; }
    public string CalendarSyncStatus { get; set; } = "NotScheduled";
    public string[] PanelistUserIds { get; set; } = [];
    public int QuorumRequired { get; set; }
    public string DebriefStatus { get; set; } = RecruitmentDebriefStatus.Pending;
    public string? DebriefDecision { get; set; }
    public string? DebriefedByUserId { get; set; }
    public DateTime? DebriefedAtUtc { get; set; }
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}
public static class RecruitmentInterviewStatus
{
    public const string Draft = "Draft";
    public const string Scheduled = "Scheduled";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
}
public static class RecruitmentDebriefStatus
{
    public const string Pending = "Pending";
    public const string Complete = "Complete";
}
