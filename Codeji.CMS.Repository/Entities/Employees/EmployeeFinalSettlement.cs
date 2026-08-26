using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public sealed class EmployeeFinalSettlement : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EmployeeFinalSettlementId { get; set; } = string.Empty;
    public string EmployeeExitId { get; set; } = string.Empty;
    public string EmployeeUserId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedByUserId { get; set; }
}
