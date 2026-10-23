using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentInterviewPlanCommand
{
    public string? PlanId { get; set; }
    public string? VacancyId { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    [Required, MinLength(1)] public List<RecruitmentInterviewPlanStageCommand> Stages { get; set; } = [];
    public RecruitmentRatingScaleConfig? RatingScale { get; set; }
    public List<RecruitmentCompetencyConfig>? Competencies { get; set; }
    [Range(0, 100)] public decimal HiringTargetScore { get; set; } = 70m;
    public string? MissingScorePolicy { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
}

public sealed class RecruitmentInterviewPlanStageCommand
{
    [Range(1, 20)] public int Order { get; set; }
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    [Range(1, 20)] public int RequiredPanelSize { get; set; } = 1;
    public bool ScorecardRequired { get; set; } = true;
    [Range(1, 720)] public int FeedbackDueHours { get; set; } = 24;
    [Range(0, 100)] public decimal? Weight { get; set; }
    [Range(0, 100)] public decimal? PassThreshold { get; set; }
    public bool IsMandatory { get; set; }
}

public sealed class RecruitmentRatingScaleConfig
{
    public string ScaleType { get; set; } = "Numeric1To5"; // "Numeric1To5", "Numeric1To10", "Numeric0To100", "CustomNumeric", "Qualitative"
    public decimal MinRating { get; set; } = 1m;
    public decimal MaxRating { get; set; } = 5m;
    public decimal Step { get; set; } = 1m;
    public string NormalizationMethod { get; set; } = "PercentageOfMax"; // "PercentageOfMax", "MinMaxPercentage"
    public List<RecruitmentQualitativeRatingLevel> Levels { get; set; } = [];
}

public sealed class RecruitmentQualitativeRatingLevel
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Description { get; set; }
}

public sealed class RecruitmentCompetencyConfig
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Weight { get; set; } = 0m;
    public bool IsMandatory { get; set; }
    public decimal? MinScore { get; set; }
}
