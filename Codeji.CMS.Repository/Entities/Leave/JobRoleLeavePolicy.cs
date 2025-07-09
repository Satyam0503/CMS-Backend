using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Leave;

public class JobRoleLeavePolicy
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public string JobRoleId { get; set; }
    public List<JobRoleLeaveType> JobRoleLeaveTypes { get; set; }
}

public class JobRoleLeaveType
{
    public string LeaveType { get; set; }
    public int MaximumLeave { get; set; }
}
