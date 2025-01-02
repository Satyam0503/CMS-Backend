using System;
using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Recruitments
{
	public class ApplicantRegisterModel
	{
        public string ApplicantId { get; set; }
        public string CompnyId { get; set; }
        [RegularExpression("^[^=~<>;`%]*$", ErrorMessage = "One or more invalid characters")]
        public string FirstName { get; set; }
        [Required]
        [RegularExpression("^[^=~<>;`%]*$", ErrorMessage = "One or more invalid characters")]
        public required string LastName { get; set; }
        [EmailAddress]
        public required string Email { get; set; }

        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian phone number.")]
        public required string Phone { get; set; }
        public string VacanyId { get; set; }
        public decimal Exprience  { get; set; }

    }
}

