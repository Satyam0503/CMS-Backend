using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public sealed class EmployeeExitClearanceTemplate : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string EmployeeExitClearanceTemplateId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? OwnerRole { get; set; }
    public bool IsMandatory { get; set; } = true;
    public int DueDaysBeforeLastWorkingDate { get; set; }
    public bool IsActive { get; set; } = true;
}
