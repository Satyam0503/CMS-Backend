namespace Codeji.CMS.DTO.Calendar;

public class CalendarResponseDto
{
    public string? OccasionId { get; set; }
    public string OccasionName { get; set; }
    public DateTime Date { get; set; }
    public string Detail { get; set; }
    public string? HolidayImageUrl { get; set; }
    public string? HolidayCoverImageUrl { get; set; }
    // {
    //     get
    //     {
    //         return !string.IsNullOrEmpty(HolidayImageUrl) ? Common.GetHolidayCoverImagePath(HolidayImageUrl) : null;
    //     }
    // }
}
