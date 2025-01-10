using System;
namespace Codeji.CMS.DTO.RolePermissions
{
    public class RoleModel
    {
        public string CompanyRoleId { get; set; }
        public string Titles { get; set; }
        public string Description { get; set; }
        public bool IsNotEditable { get; set; }
        public bool IsDefault { get; set; }
        public List<string> UserRoles { get; set; }
    }
}

