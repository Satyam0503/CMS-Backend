using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Recruitments
{
    public class ProcessLogs
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string ProcessId { get; set; }
        public string ApplicantName { get; set; }
        public string CompanyId { get; set; }
        public string UserId { get; set; }
        public string JobRole { get; set; }
        public int ActionCategory { get; set; }
        public DateTime CommentedOn { get; set; }
        public string Comments { get; set; }
        public string CommentedBy { get; set; }


    }
}
