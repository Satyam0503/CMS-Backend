using MongoDB.Bson.Serialization.Attributes;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.Repository.Entities
{
    public class UserSecurityToken
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string Id { get; set; }
        public required string UserId { get; set; }
        public required string TokenHash { get; set; }
        public required DateTime Expiry { get; set; }
        public required bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }
        public required SecurityTokenType Type { get; set; }
    }
}