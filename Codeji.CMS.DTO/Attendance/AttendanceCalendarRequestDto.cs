public class AttendanceCalendarRequestDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string[]? UserIds { get; set; } 
}
