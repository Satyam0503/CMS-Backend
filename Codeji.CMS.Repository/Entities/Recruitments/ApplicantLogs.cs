using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Recruitments
{
    public class ApplicantLogs : BaseClass
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string Id { get; set; }
        public string Description { get; set; }
        public string ApplicantId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string JobRole { get; set; }
        public int ActivityCategory { get; set; }
    }
}
