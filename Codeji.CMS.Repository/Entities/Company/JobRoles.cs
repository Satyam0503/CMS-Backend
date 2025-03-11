using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Company
{
    public class JobRoles
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string JobRoleId { get; set; }
        public string JobTitle { get; set; }

        public string JobDescription { get; set; }

    }
}
