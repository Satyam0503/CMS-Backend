using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

public class User : BaseClass
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string UserId { get; set; }
    public required string FirstName { get; set; }
    public required string? LastName { get; set; }
    public required string Email { get; set; }
    public string Password { get; set; }
    public required string RoleId { get; set; }
}
