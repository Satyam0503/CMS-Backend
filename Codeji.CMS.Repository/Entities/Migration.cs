using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities;

[BsonIgnoreExtraElements]
public class Migration
{
    [BsonId]
    public required string Id { get; set; }
    public required DateTime ExecutedAt { get; set; }
}
