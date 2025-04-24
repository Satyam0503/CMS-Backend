using Codeji.CMS.DTO;
using MongoDB.Bson.Serialization.Attributes;
namespace Codeji.CMS.Repository.Entities.Company
{
    public class Department : BaseClass
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string DepartmentId { get; set; }
        public bool IsActive {get;set;}
        public List<MultilingualModel> Titles {get;set;}
    }
}
