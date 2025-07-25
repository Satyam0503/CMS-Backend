using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities;

public class RefreshToken
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; }
    public required string Token { get; set; }
    public required string UserId { get; set; }
    public required DateTime ExpireAt { get; set; }
    public required DateTime CreatedAt { get; set; }
}
