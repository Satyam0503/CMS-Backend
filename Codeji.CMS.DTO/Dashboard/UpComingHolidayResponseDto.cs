using Codeji.CMS.Utility;

namespace Codeji.CMS.DTO.Dashboard
{
    public class UpComingHolidayResponseDto
    {
        public string? HolidayId { get; set; }
        public string HolidayName { get; set; }
        public DateTime Date { get; set; }
        public string? HolidayCoverImageUrl { get; set; }
    }
}
