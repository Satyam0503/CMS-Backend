using Codeji.CMS.Repository.Entities;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Recruitments;

/// <summary>Tenant-scoped offer record. Approval remains delegated to shared approval infrastructure.</summary>
public sealed class RecruitmentOffer : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string OfferId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string VacancyId { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public Dictionary<string, decimal> CompensationComponents { get; set; } = [];
    public decimal TotalCtc { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public string Status { get; set; } = RecruitmentOfferStatus.Draft;
    public string? ApprovalReference { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public DateTime? ViewedAtUtc { get; set; }
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}

public static class RecruitmentOfferStatus
{
    public const string Draft = "Draft";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Sent = "Sent";
    public const string ChangeRequested = "ChangeRequested";
    public const string Accepted = "Accepted";
    public const string Declined = "Declined";
    public const string Expired = "Expired";
    public const string Withdrawn = "Withdrawn";
}



