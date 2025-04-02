using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Company
{
    public class Department : BaseClass
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }

        public string DepartmentDescription { get; set; }

    }
}
