using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Recruitments;

public sealed class RecruitmentOfferVersionCommand
{
    [Required] public string OfferId { get; set; } = string.Empty;
    [Range(0.01, double.MaxValue)] public decimal Salary { get; set; }
    [Required] public string Currency { get; set; } = string.Empty;
    public DateTime? StartDateUtc { get; set; }
    [Required] public string Status { get; set; } = "Draft";
    public string? VersionReason { get; set; }
    public string? ApprovalReference { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedBaseVersionNumber { get; set; }
    public int? ExpectedVersionNumber { get; set; }
}

public sealed class RecruitmentOfferCounterCommand
{
    [Required] public string OfferId { get; set; } = string.Empty;
    [Range(0.01, double.MaxValue)] public decimal RequestedSalary { get; set; }
    [Required] public string RequestedCurrency { get; set; } = string.Empty;
    public DateTime? RequestedStartDateUtc { get; set; }
    [MaxLength(2000)] public string? Message { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedBaseVersionNumber { get; set; }
}

public sealed class RecruitmentOfferCounterDecisionCommand
{
    [Required] public string CounterRequestId { get; set; } = string.Empty;
    [Required] public string Decision { get; set; } = string.Empty;
    [MaxLength(2000)] public string? ResolutionNote { get; set; }
    public decimal? ApprovedSalary { get; set; }
    public string? ApprovedCurrency { get; set; }
    public DateTime? ApprovedStartDateUtc { get; set; }
    [Required] public string IdempotencyKey { get; set; } = string.Empty;
    public int? ExpectedBaseVersionNumber { get; set; }
}



