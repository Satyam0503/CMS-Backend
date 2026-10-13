using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentInterviewCommand
{
    public string? InterviewId { get; set; }
    [Required] public string ApplicantId { get; set; } = string.Empty;
    [Required] public string VacancyId { get; set; } = string.Empty;
    [Required] public string Title { get; set; } = string.Empty;
    [Range(1, 20)] public int RoundNumber { get; set; } = 1;
    public string? RoundType { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
    public string? Location { get; set; }
    public string? MeetingUrl { get; set; }
    public string[] PanelistUserIds { get; set; } = [];
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
    public string? Status { get; set; }
}

public sealed class RecruitmentOfferCommand
{
    public string? OfferId { get; set; }
    [Required] public string ApplicantId { get; set; } = string.Empty;
    [Required] public string VacancyId { get; set; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal Salary { get; set; }
    [Required] public string Currency { get; set; } = string.Empty;
    public DateTime? StartDateUtc { get; set; }
    public Dictionary<string, decimal> CompensationComponents { get; set; } = [];
    public DateTime? ExpiresAtUtc { get; set; }
    public string? ApprovalReference { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedVersion { get; set; }
    [Required] public string Status { get; set; } = "Draft";
}




