using System;
namespace Codeji.CMS.DTO.Employee
{
	public class LoginUserViewModel
	{
        public string UserId { get; set; }
        public string CompanyId { get; set; }
        public List<string> Roles { get; set; }
        public string RoleId { get; set; }

	}
}

