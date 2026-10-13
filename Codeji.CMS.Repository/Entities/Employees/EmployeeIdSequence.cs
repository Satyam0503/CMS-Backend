using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

/// <summary>One atomic, tenant-owned counter for automatic employee IDs.</summary>
public sealed class EmployeeIdSequence : BaseClass
{
    [BsonId]
    public string SequenceId { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int NextNumber { get; set; }
}
