using Microsoft.AspNetCore.Http;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.Calendar;

public class CalendarRequestDto
{
    public string? OccasionId { get; set; }
    public string OccasionName { get; set; }
    public bool Recurring { get; set; }
    public CalendarItem OccasionType { get; set; }
    public DateTime Date { get; set; }
    public string Detail { get; set; }
    public IFormFile? OccasionImage { get; set; }
}
