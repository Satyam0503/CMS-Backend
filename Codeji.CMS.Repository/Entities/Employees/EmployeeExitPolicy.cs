using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

/// <summary>Company policy with department taking precedence over role, then company default.</summary>
public sealed class EmployeeExitPolicy : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EmployeeExitPolicyId { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string? RoleId { get; set; }
    public int MinimumNoticeDays { get; set; } = 30;
    public int Version { get; set; } = 1;
    public bool RequireManagerReview { get; set; } = true;
    public bool RequireHrReview { get; set; } = true;
    public bool AllowJointDiscussion { get; set; } = true;
    public bool RequireResignationEvidence { get; set; }
    public bool RequireEmployeeAcknowledgement { get; set; }
    public bool EnableFormerEmployeePortal { get; set; }
    public bool IsActive { get; set; } = true;
}
