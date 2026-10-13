using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

namespace Codeji.CMS.Repository.Entities.Recruitments;

public sealed class RecruitmentOfferVersion : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string OfferVersionId { get; set; } = string.Empty;
    public string OfferId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public decimal Salary { get; set; } public Dictionary<string, decimal> CompensationComponents { get; set; } = []; public decimal TotalCtc { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? StartDateUtc { get; set; }
    public string Status { get; set; } = "Draft";
    public string VersionReason { get; set; } = "Initial";
    public string? IdempotencyKey { get; set; }
    public string? ApprovalReference { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

public sealed class RecruitmentOfferCounterRequest : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))] public string CounterRequestId { get; set; } = string.Empty;
    public string OfferId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public int BaseVersionNumber { get; set; }
    public decimal RequestedSalary { get; set; }
    public string RequestedCurrency { get; set; } = string.Empty;
    public DateTime? RequestedStartDateUtc { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = "Pending";
    public int NegotiationRound { get; set; } = 1;
    public string? ResolutionNote { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? DecisionIdempotencyKey { get; set; }
    public string? IdempotencyKey { get; set; }
}




