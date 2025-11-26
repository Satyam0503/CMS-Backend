namespace Codeji.CMS.DTO.Calendar;

public class HolidayResponseDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
}
public class HolidayFilter
{
    public required DateTime FromDate { get; set; }
    public required DateTime ToDate { get; set; }
}