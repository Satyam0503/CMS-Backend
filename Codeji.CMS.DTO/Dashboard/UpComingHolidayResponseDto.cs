using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.DTO.Dashboard
{
    public class UpComingHolidayEventResponseDto
    {
        public List<CalendarItemDto> Holiday { get; set; }
        public List<CalendarItemDto> Event { get; set; }
    }

    public class CalendarItemDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public CalendarItem Type { get; set; }
        public string? ImageUrl { get; set; }
    }
}
