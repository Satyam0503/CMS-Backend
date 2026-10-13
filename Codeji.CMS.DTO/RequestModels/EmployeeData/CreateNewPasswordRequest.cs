using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.RequestModels.EmployeeData;

public class CreateNewPasswordRequest
{
    [Required]
    [StringLength(25, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$",
        ErrorMessage = "Password must contain an uppercase letter, a lowercase letter, a number, and a special character.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}
