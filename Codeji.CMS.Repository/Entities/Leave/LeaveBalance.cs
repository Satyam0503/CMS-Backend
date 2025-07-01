using Codeji.CMS.DTO.Leave.LeaveBalance;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Leave;

public class LeaveBalance:BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public string EmployeeId { get; set; }
    public DateTime Year { get; set; }
    public List<LeaveTypeBalance> LeaveTypeBalances { get; set; }
}

