using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.RolePermissions
{
    public class Permission
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string _id { get; set; }
        public int PermissionId { get; set; }
        public string PermissionName { get; set; }
        public string PermissionConstant { get; set; }
    }
}
