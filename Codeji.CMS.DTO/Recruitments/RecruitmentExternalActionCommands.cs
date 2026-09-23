using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentExternalActionIssueCommand
{
    [Required] public string ApplicantId { get; set; } = string.Empty;
    public string? VacancyId { get; set; }
    public string? InterviewId { get; set; }
    public string? OfferId { get; set; }
    [Required] public string ActionType { get; set; } = string.Empty;
    [Required, EmailAddress] public string RecipientEmail { get; set; } = string.Empty;
    public int? ResourceVersion { get; set; }
    public int ExpiresInMinutes { get; set; } = 60;
    [Required] public string IssuanceKey { get; set; } = string.Empty;
}

public sealed class RecruitmentExternalActionTokenDto
{
    [JsonIgnore] public string CompanyId { get; set; } = string.Empty;
public string ActionTokenId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string? VacancyId { get; set; }
    public string? InterviewId { get; set; }
    public string? OfferId { get; set; }
    public int? ResourceVersion { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    [JsonIgnore] public string? RawToken { get; set; }
}

public sealed class RecruitmentExternalActionConsumeCommand
{
    [Required] public string Token { get; set; } = string.Empty;
    public bool Consume { get; set; }
}

public sealed class RecruitmentEmailQueueCommand
{
    [Required] public string DedupeKey { get; set; } = string.Empty;
    [Required] public string EventType { get; set; } = string.Empty;
    [Required, EmailAddress] public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientUserId { get; set; }
    [Required] public string Subject { get; set; } = string.Empty;
    [Required] public string HtmlBody { get; set; } = string.Empty;
}

public sealed class RecruitmentCandidateInterviewActionCommand
{
    [System.ComponentModel.DataAnnotations.Required] public string Token { get; set; } = string.Empty;
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public string? TimeZoneId { get; set; }
    public string? Location { get; set; }
    public string? MeetingUrl { get; set; }
    [System.ComponentModel.DataAnnotations.Required] public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class RecruitmentCandidateOfferActionCommand
{
    [System.ComponentModel.DataAnnotations.Required] public string Token { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required] public string Decision { get; set; } = string.Empty;
    public string? Message { get; set; }
    [System.ComponentModel.DataAnnotations.Required] public string IdempotencyKey { get; set; } = string.Empty;
}





