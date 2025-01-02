using System;
using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Employee
{
	public class LoginModel
	{
		public LoginModel()
		{
		}
		[Required]
		[EmailAddress]
		public string Email { get; set; }
        [Required]
        public string Password { get; set; }
	}
}

