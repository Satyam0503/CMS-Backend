using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Company;

public class PolicyVersion : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public required string PolicyId { get; set; }
    public string? DocUrl { get; set; }
    public required string VersionName { get; set; }
    public bool IsCurrent { get; set; }
}
