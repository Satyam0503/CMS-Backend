using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public class EmpWorkHistory : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string WorkHistoryId { get; set; }
    public required string UserId { get; set; }
    public required string OrganisationName { get; set; }
    public required DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public required string JobRole { get; set; }
    public string? JobLocation { get; set; }
}