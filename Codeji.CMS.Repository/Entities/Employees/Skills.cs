using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees
{
    public class Skills : BaseClass
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string SkillId { get; set; }
        public string UserId { get; set; }
        public string TotalSkills { get; set; }
    }
}
