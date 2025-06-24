using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.Holiday;

public class HolidayFilter
{
    public string? HolidayName { get; set; }
    public EnumsHelper.HolidayTypes[]? HolidayType { get; set; }
    public DateTime? Date { get; set; }
    public int PageNo { get; set; }
    public int PageSize { get; set; }
}
