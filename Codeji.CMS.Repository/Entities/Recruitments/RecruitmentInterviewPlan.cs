using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Recruitments;

public sealed class RecruitmentInterviewPlan : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string PlanId { get; set; } = string.Empty;
    public string? VacancyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
    public List<RecruitmentInterviewPlanStage> Stages { get; set; } = [];

    // Configurable competency & scoring engine settings
    public RecruitmentRatingScaleConfig RatingScale { get; set; } = new();
    public List<RecruitmentCompetencyConfig> Competencies { get; set; } = [];
    public decimal HiringTargetScore { get; set; } = 70m;
    public string MissingScorePolicy { get; set; } = "Exclude"; // "Exclude", "Zero", "Block"
}

public sealed class RecruitmentInterviewPlanStage
{
    public int Order { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int RequiredPanelSize { get; set; } = 1;
    public bool ScorecardRequired { get; set; } = true;
    public int FeedbackDueHours { get; set; } = 24;
    public decimal? Weight { get; set; }
    public decimal? PassThreshold { get; set; }
    public bool IsMandatory { get; set; }
}
