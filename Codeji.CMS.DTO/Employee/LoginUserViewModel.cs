using System;
namespace Codeji.CMS.DTO.Employee
{
	public class LoginUserViewModel
	{
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Fullname {
            get { return FirstName + " " + LastName; }
        }
        public string CompanyId { get; set; }
        public string Role { get; set; }

        public string[] Permissions { get; set; }



    }
}

