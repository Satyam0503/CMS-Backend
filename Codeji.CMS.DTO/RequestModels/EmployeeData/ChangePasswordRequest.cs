using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.RequestModels.EmployeeData
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(25, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", ErrorMessage = "Password must contain at least one letter, one number, one special character and be at least 8 characters long")]
        public string NewPassword { get; set; }
        public string OldPassword { get; set; }
    }
}
