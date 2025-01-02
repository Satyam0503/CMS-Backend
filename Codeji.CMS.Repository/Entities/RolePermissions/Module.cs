using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.RolePermissions
{
    public class Module
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string _id { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string ModuleConstant { get; set; }
        public int SortOrder { get; set; }
    }
}
