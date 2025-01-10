using System;
using Codeji.CMS.Utility;

namespace Codeji.CMS.DTO.RolePermissions
{
    public class ModuleWithPermissionsModel
    {
        public string ModuleName { get; set; }
        public string ModuleConstant { get; set; }
        public List<ModuleRolePermissionsModel> Permissions { get; set; }
    }
    public class ModuleRolePermissionsModel
    {
        public string PermissionName { get; set; }
        public string RolePermissionId { get; set; }
        public int ModulePermissionId { get; set; }
        public string CompanyRoleId { get; set; }
        public bool HasAccess { get; set; }
        public string PermissionConstant { get; set; }
    }
    public class RoleWithModuleAndPermissions
    {
        public string RoleId { get; set; }
        public string RoleTitle { get; set; }
        [Sanitize]
        public string Description { get; set; }
        public List<ModuleRolePermissionsModel> RolePermissions { get; set; }
        public List<string> UserRoles { get; set; }
    }
}

