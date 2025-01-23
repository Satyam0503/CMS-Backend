using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees
{
    public class EmployeeSkills
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string Id { get; set; }
        public string UserId { get; set; }
        public string CompanyId { get; set; }
        public List<string> Skills { get; set; }
    }
}
