using System.ComponentModel.DataAnnotations;
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.Holiday;

public class HolidayResponseDto
{
    public string? HolidayId { get; set; }
    // [Required]
    public string HolidayName { get; set; }
    public DateTime Date { get; set; }
    public string Detail { get; set; }
    public EnumsHelper.HolidayTypes HolidayType { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
