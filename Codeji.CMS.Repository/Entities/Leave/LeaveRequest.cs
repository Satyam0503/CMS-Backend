using Codeji.CMS.Utility.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Leave;

public class LeaveRequest : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string LeaveRequestId { get; set; }
    public string EmployeeId { get; set; }
    public bool IsHalfDay { get; set; }
    // FIRST_HALF or SECOND_HALF. Null is tolerated for legacy requests and is
    // interpreted as FIRST_HALF only when reconciliation needs a segment.
    public string? HalfDayPeriod { get; set; }
    public required string LeavePolicyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public required string Reason { get; set; }
    public string? ReviewedBy { get; set; }
    public string Comment { get; set; } = "";
    public EnumsHelper.LeaveRequestStatus Status { get; set; }
    public int Version { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Decision status and attendance reconciliation are deliberately separate.
    // An Accepted request is not proof that its attendance rows were created.
    public string ReconciliationStatus { get; set; } = "NotRequired";
    public string? ReconciliationErrorCode { get; set; }
    public string? ReconciliationErrorMessage { get; set; }
    public int ReconciliationAttempts { get; set; }
    public DateTime? LastReconciliationAttemptAtUtc { get; set; }
    public DateTime? NextReconciliationAttemptAtUtc { get; set; }
    public DateTime? ReconciledAtUtc { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    public string? ReconciliationJobId { get; set; }

    public string? AssignedReviewerUserId { get; set; }
    public string? AssignedReviewerSource { get; set; }
    public DateTime? AssignedAtUtc { get; set; }

}
