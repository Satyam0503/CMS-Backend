using System.ComponentModel.DataAnnotations;
namespace Codeji.CMS.DTO.Recruitments;
public sealed class RecruitmentApplicantMergeCommand { [Required] public string SourceApplicantId { get; set; } = string.Empty; [Required] public string TargetApplicantId { get; set; } = string.Empty; [Required] public string IdempotencyKey { get; set; } = string.Empty; public int? ExpectedSourceVersion { get; set; } public string? Reason { get; set; } }
public sealed class RecruitmentStageSlaPolicyCommand { [Required] public string Stage { get; set; } = string.Empty; public int TargetHours { get; set; } public bool Enabled { get; set; } = true; public string? Location { get; set; } public string IdempotencyKey { get; set; } = string.Empty; }
public sealed class RecruitmentEnterpriseConfigCommand { public List<string>? Locations { get; set; } public string? RecruitmentMode { get; set; } public bool? EnableSla { get; set; } public bool? EnableAutomation { get; set; } public bool ResetToDefaults { get; set; } public string? DefaultTimezone { get; set; } public string IdempotencyKey { get; set; } = string.Empty; public bool? ApplicationReview { get; set; } public bool? HiringManagerReview { get; set; } public bool? InterviewRounds { get; set; } public bool? ParallelPanels { get; set; } public bool? Scorecards { get; set; } public bool? OfferApproval { get; set; } public bool? OfferNegotiation { get; set; } public bool? CandidateEmailActions { get; set; } public bool? Requisitions { get; set; } public bool? SlaTracking { get; set; } public bool? Automation { get; set; } public bool? InternalMobility { get; set; } public bool? RehireLinks { get; set; } public bool? AutoAdvanceOnScreening { get; set; } public bool? RequireInterviewBeforeDecision { get; set; } public bool? RequirePreboardingBeforeHandoff { get; set; } public bool? AllowMultiVacancyApplications { get; set; } public bool? AllowJobWorkflowCustomization { get; set; } public List<string>? ScreeningCriteria { get; set; } public int? ScreeningPassScore { get; set; } public List<string>? PreboardingChecklist { get; set; } public string? CompensationSchemeName { get; set; } public string? CompensationCurrency { get; set; } public Dictionary<string, decimal>? CompensationComponents { get; set; } [Range(30, 3650)] public int? DefaultCandidateRetentionDays { get; set; } public string? OfferLetterSubjectTemplate { get; set; } public string? OfferLetterBodyTemplate { get; set; } }
public sealed class RecruitmentRelationshipLinkCommand { [Required] public string ApplicantId { get; set; } = string.Empty; [Required] public string RelationshipType { get; set; } = string.Empty; [Required] public string RelatedPersonId { get; set; } = string.Empty; public string? Notes { get; set; } public string IdempotencyKey { get; set; } = string.Empty; }
public sealed class RecruitmentAutomationEventCommand { [Required] public string EventType { get; set; } = string.Empty; public string? ApplicantId { get; set; } public Dictionary<string,string> Payload { get; set; } = []; public string IdempotencyKey { get; set; } = string.Empty; }
public sealed class RecruitmentAnalytics { public int TotalApplicants { get; set; } public int ActiveApplicants { get; set; } public int RejectedApplicants { get; set; } public int MergedApplicants { get; set; } public Dictionary<string,int> ByStage { get; set; } = []; public decimal? AverageTimeToHireDays { get; set; } public int TimeToHireSampleSize { get; set; } }

public sealed class RecruitmentFeedbackSlaItem { public string InterviewId { get; set; } = string.Empty; public string ApplicantId { get; set; } = string.Empty; public string VacancyId { get; set; } = string.Empty; public int RoundNumber { get; set; } public DateTime DueAtUtc { get; set; } public int AssignedInterviewers { get; set; } public int SubmittedScorecards { get; set; } public bool Overdue { get; set; } }
public sealed class RecruitmentFeedbackSlaReport { public int TotalOpenRounds { get; set; } public int OverdueRounds { get; set; } public List<RecruitmentFeedbackSlaItem> Items { get; set; } = []; }



public sealed class RecruitmentReadinessDto
{
    public string Mode { get; set; } = "Simple";
    public string Timezone { get; set; } = "UTC";
    public bool ReadyToPublishJobs { get; set; }
    public int CompletionPercent { get; set; }
    public List<RecruitmentReadinessItemDto> Required { get; set; } = [];
    public List<RecruitmentReadinessItemDto> Recommended { get; set; } = [];
}

public sealed class RecruitmentReadinessItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Complete { get; set; }
}
