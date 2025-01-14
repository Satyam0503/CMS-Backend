using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.RolePermissions
{

    public class ModulePermission
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string _id { get; set; }
        public int ModulePermissionId { get; set; }
        public int ModuleId { get; set; }
        public int PermissionId { get; set; }
        public bool HasModuleAccess { get; set; }
    }
}
