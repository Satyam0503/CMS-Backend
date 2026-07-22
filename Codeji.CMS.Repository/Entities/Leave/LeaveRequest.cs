using Codeji.CMS.Utility.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Leave;

public class LeaveRequest : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string LeaveRequestId { get; set; }
    public string EmployeeId { get; set; }
    public bool IsHalfDay { get; set; }
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

}
