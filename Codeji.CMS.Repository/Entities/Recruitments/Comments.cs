using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Recruitments
{
    public class Comments
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string CommentId { get; set; }
        public string Description { get; set; }
        public string CompanyId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ApplicantId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int ActivityCategory { get; set; }
    }
}
