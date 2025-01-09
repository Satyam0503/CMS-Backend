using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.RolePermissions
{
    public class CompanyRole
    {
        [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
        public string CompanyRoleId {  get; set; }
        public int RoleType { get; set; }
        public string CompanyId { get; set; }
        public string Titles { get; set; }
        public string Description { get; set; }
        public bool HasAppAccess { get; set; }
        public bool IsNotEditable { get; set; }
        public bool IsDefault { get; set; }
        public bool IsDeleted { get; set; }
        public List<string> UserRoles { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
