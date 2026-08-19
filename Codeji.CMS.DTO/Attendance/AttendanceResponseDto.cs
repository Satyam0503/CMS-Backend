public class AttendanceResponseDto
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string EmployeeId {get;set;}
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = "P";
    public string Remarks { get; set; }
    public string? SourceType { get; set; }
    public string? SourceId { get; set; }
    // Snapshot of the tenant-scoped effective shift used when this attendance
    // row was marked. It supports historical shift filtering without resolving
    // a later employee/department reassignment against past attendance.
    public string? ScheduleId { get; set; }
    // A half-day leave/WFH is stored as a segment so the other half can retain
    // its attendance.  Calendar clients use this flag to give that segment
    // precedence over the compatibility daily AttendanceModel row.
    public bool IsDaySegment { get; set; }
    public string? Segment { get; set; }
    public int LateCount { get; set; }
    public int EarlyExitCount { get; set; }
}
