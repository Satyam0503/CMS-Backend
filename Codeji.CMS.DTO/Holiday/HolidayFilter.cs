using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.Holiday;

public class HolidayFilter
{
    public EnumsHelper.HolidayTypes[]? HolidayType { get; set; }
    public DateTime? Date { get; set; }
}
