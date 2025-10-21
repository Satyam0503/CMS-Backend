namespace Codeji.CMS.DTO.Dashboard
{
    public class UpcomingCelebrations
    {
        public List<CalebrationItemDto> Birthday { get; set; }
        public List<CalebrationItemDto> WorkAnniversary { get; set; }
    }

    public class CalebrationItemDto
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string? ProfileUrl { get; set; }
        public Dictionary<string, string>? JobRole { get; set; } = null;
        public DateTime Date { get; set; }
        public int Ordinal { get; set; }
    }
}
