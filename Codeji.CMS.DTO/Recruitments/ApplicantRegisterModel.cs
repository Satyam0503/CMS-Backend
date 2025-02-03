using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Recruitments
{
    public class ApplicantRegisterModel
    {
        public string ApplicantId { get; set; }
        public string CompanyId { get; set; }
        [RegularExpression("^[^=~<>;`%]*$", ErrorMessage = "One or more invalid characters")]
        public string FirstName { get; set; }

        [RegularExpression("^[^=~<>;`%]*$", ErrorMessage = "One or more invalid characters")]
        public required string LastName { get; set; }
        [EmailAddress]
        public required string Email { get; set; }

        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Indian phone number.")]
        public required string Phone { get; set; }
        public string VacancyId { get; set; }
        public string VacancyName { get; set; }
        public decimal Experience { get; set; }
        public string? ResumeUrl { get; set; }

    }
}

