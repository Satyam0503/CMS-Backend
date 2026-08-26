using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public sealed class EmployeeExitAttendanceCutoff : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EmployeeExitAttendanceCutoffId { get; set; } = string.Empty;
    public string EmployeeExitId { get; set; } = string.Empty;
    public string EmployeeUserId { get; set; } = string.Empty;
    public DateTime LastWorkingDate { get; set; }
    public DateTime LockedAtUtc { get; set; }
    public string LockedByUserId { get; set; } = string.Empty;
}
