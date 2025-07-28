using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.RequestModels;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; }
    [Required]
    public string UserId { get; set; }
}
