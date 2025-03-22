using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees
{
    public class EmpSkills
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string Id { get; set; }
        public string CompanyId { get; set; }
        public string UserId { get; set; }
        public string Skills { get; set; }
    }
}
