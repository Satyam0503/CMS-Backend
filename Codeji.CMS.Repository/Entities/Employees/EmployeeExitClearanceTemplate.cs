using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public sealed class EmployeeExitClearanceTemplate : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EmployeeExitClearanceTemplateId { get; set; } = string.Empty;
    /// <summary>Persisted tenant-local identity combining template key and scope.</summary>
    public string ScopeKey { get; set; } = string.Empty;
    // Stable logical identity. A department template replaces only the global template with this key.
    public string TemplateKey { get; set; } = string.Empty;
    public string Scope { get; set; } = "Global"; // Global, Department
    public string? DepartmentId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OwnerRole { get; set; }
    /// <summary>Operation permission responsible for the task; replaces free-text role matching.</summary>
    public string? OwnerPermission { get; set; }
    // The policy completion gate that enables this template on a newly approved exit.
    // General templates are only created when at least one clearance gate is required.
    public string RequiredPolicyGate { get; set; } = "General";
    public bool IsMandatory { get; set; } = true;
    public int DueDaysBeforeLastWorkingDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public int Version { get; set; } = 1;
}
