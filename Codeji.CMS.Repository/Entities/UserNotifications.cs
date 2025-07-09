using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities
{

    public class UserNotifications
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string UserNotificationId { get; set; }
        public string UserId { get; set; }
        public string NotificationId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}