using Codeji.CMS.Utility.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities
{
    public class Notifications
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string NotificationId { get; set; }
        public string Title { get; set; }
        public EnumsHelper.NotificationTypes NotificationType { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string CreatedBy { get; set; }
    }
}