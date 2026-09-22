using MongoDB.Bson.Serialization.Attributes;
using Codeji.CMS.Repository.Entities;

namespace Codeji.CMS.Repository.Entities.Recruitments;

/// <summary>Tenant-scoped headcount request. Approval remains represented by the shared approval reference.</summary>
public sealed class RecruitmentRequisition : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string RequisitionId { get; set; } = string.Empty;
    public string VacancyId { get; set; } = string.Empty;
    public string RequestedByUserId { get; set; } = string.Empty;
    public string PositionTitle { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string? Location { get; set; }
    public string? EmploymentType { get; set; }
    public string? HiringManagerUserId { get; set; }
    public string? CostCenter { get; set; }
    public DateTime? TargetStartDateUtc { get; set; }
    public string? Justification { get; set; }
    public int Headcount { get; set; }
    public int ApprovedHeadcount { get; set; }
    public string Status { get; set; } = RecruitmentRequisitionStatus.Draft;
    public string? ApprovalReference { get; set; }
    public string? LastCommandIdempotencyKey { get; set; }
    public int Version { get; set; } = 1;
}

public static class RecruitmentRequisitionStatus
{
    public const string Draft = "Draft";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Closed = "Closed";
}