using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Company;

public class Policy : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public required string PolicyId { get; set; }
    public required string PolicyName { get; set; }
    public string Description { get; set; }

    // List of department IDs that this policy applies to. if empty, applies to all departments.
    public List<string> Departments { get; set; } = [];

    // List of roles that have access to this policy (e.g., Admin, HR, Employee). if empty, accessible to all roles.
    public List<string> Roles { get; set; } = [];
    public bool IsActive { get; set; } = true;

}
