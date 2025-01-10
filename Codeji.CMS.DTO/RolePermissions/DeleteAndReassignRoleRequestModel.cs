using System;
namespace Codeji.CMS.DTO.RolePermissions
{
    public class DeleteAndReassignRoleRequestModel
    {
        public string RoleId { get; set; }
        public string NewRole { get; set; }
    }
}

