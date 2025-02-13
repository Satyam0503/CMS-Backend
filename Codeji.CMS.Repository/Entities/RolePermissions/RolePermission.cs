using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.RolePermissions
{
    public class RolePermission : BaseClass
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string RolePermissionId { get; set; }
        public string RoleId { get; set; }
        public int ModulePermissionId { get; set; }
        public bool HasAccess { get; set; }
        //public bool IsDefaultProfile { get; set; }
        public bool IsAccessible { get; set; }
    }
}
