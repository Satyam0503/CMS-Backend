using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.DTO.Holiday;

public class HolidayRequestDto
{
    public string? HolidayId { get; set; }
    // [Required]
    public string HolidayName { get; set; }
    public DateTime Date { get; set; }
    public string Detail { get; set; }
    public EnumsHelper.HolidayTypes HolidayType { get; set; }
    public IFormFile? HolidayImage { get; set; }
}
