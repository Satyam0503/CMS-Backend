using Microsoft.AspNetCore.Http;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.Calendar;

public class CalendarRequestDto
{
    public string? Id { get; set; }
    public string Name { get; set; }
    public bool Recurring { get; set; }
    public CalendarItem Type { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public IFormFile? Image { get; set; }
}
