using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities
{
    public class PasswordResetTokens
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string Id { get; set; }
        public required string UserId { get; set; }
        public required string TokenHash { get; set; }
        public required DateTime Expiry { get; set; }
        public required bool IsUsed { get; set; }
    }
}