using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

namespace Codeji.CMS.Repository.Entities.Recruitments;

/// <summary>Idempotent audit record for a candidate-to-employee handoff.</summary>
public sealed class RecruitmentHandoff : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string HandoffId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string EmployeeUserId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Preboarding";
    public DateTime HandoffAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Recruitment final interview benchmark score (0–100%) stored at hiring to measure ongoing performance.</summary>
    public int? HiringBenchmarkScore { get; set; }
    /// <summary>Recruitment final interview rating (1.0–5.0 scale) aggregated across non-HR evaluation rounds.</summary>
    public decimal? HiringBenchmarkRating { get; set; }
    /// <summary>Consensus recommendation from evaluation rounds (Strong Hire, Hire, Hold, No Hire).</summary>
    public string? HiringRecommendation { get; set; }
    /// <summary>Number of substantive evaluation rounds rated.</summary>
    public int? EvaluatedRoundsCount { get; set; }
}
