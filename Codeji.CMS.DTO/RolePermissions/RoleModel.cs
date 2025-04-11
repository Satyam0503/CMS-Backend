using System;
namespace Codeji.CMS.DTO.RolePermissions
{
    public class RoleModel
    {
        public string RolesId { get; set; }
        public string Titles { get; set; }
        public string Description { get; set; }
        public bool IsNotEditable { get; set; }
        public bool IsDefault { get; set; }
        public bool HasAppAccess {get;set;}
        public List<string> UserRoles { get; set; }
    }
}

