using Codeji.CMS.DTO.RolePermissions;

namespace Codeji.CMS.DTO.Employee
{
    public class LoginUserViewModel
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName
        {
            get { return FirstName + " " + LastName; }
        }
        public string? ProfileImage { get; set; }
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string Role { get; set; }
        public string RoleId { get; set; }
        public List<ModuleWithPermissionsModel> modulePermission { get; set; }
        public string[] Permissions { get; set; }



    }
}

