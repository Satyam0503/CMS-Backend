using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.Calendar;

public class CalendarResponseDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public CalendarResponseItem Type { get; set; }
    public string? ImageUrl { get; set; }
}
