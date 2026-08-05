using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.RequestModels;

public class EmailVerificationRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}
