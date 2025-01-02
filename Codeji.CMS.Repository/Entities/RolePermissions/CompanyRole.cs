using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.RolePermissions
{
    public class CompanyRole : BaseClass
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string CompanyRoleId { get; set; }
        public string Titles { get; set; }
        public string Description { get; set; }
        public bool IsNotEditable { get; set; }
        public bool IsDefault { get; set; }
        public List<string> UserRoles { get; set; }
    }
}
