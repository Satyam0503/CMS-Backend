public class AttendanceResponseDto
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string EmployeeId {get;set;}
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public AttendanceStatus Status { get; set; }
    public string Remarks { get; set; }
    public int LateCount { get; set; }
    public int EarlyExitCount { get; set; }
}
