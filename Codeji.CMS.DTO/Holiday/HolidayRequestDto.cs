using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.Holiday;

public class HolidayRequestDto
{

    public string HolidayId { get; set; }
    public string HolidayName { get; set; }
    public DateTime Date { get; set; }
    public string Detail { get; set; }
    public EnumsHelper.HolidayTypes HolidayType { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; }
}
