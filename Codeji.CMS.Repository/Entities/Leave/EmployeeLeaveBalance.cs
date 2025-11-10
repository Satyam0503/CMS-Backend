using Codeji.CMS.DTO.LeaveManagement.LeaveBalance;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Leave;

public class EmployeeLeaveBalance : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public required string UserId { get; set; }
    public decimal Balance { get; set; }
    public decimal UsedBalance { get; set; }
    public DateTime? LastAccrual { get; set; }
}