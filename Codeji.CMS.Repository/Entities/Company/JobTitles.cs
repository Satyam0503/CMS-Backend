using Codeji.CMS.DTO;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Company
{
    public class JobTitles : BaseClass
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string JobTitleId { get; set; }
        // A job role belongs to one department.  It is nullable only for
        // historical records created before this relationship was introduced.
        public string? DepartmentId { get; set; }
        public bool IsActive { get; set; }
        public List<MultilingualModel> Titles { get; set; }
    }
}
