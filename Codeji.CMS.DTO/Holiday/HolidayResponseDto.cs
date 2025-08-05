using System.ComponentModel.DataAnnotations;
using Codeji.CMS.Utility;
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
    public string? HolidayImageUrl { get; set; }
    public string? HolidayCoverImageUrl
    {
        get
        {
            return !string.IsNullOrEmpty(HolidayImageUrl) ? Common.GetHolidayCoverImagePath(HolidayImageUrl) : null;
        }
    }
}
