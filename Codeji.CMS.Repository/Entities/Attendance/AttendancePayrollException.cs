using Codeji.CMS.Repository.Entities;
using MongoDB.Bson.Serialization.Attributes;

public class AttendancePayrollException : BaseClass
{
    [BsonId] public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string UserId { get; set; }
    public required string EmployeeId { get; set; }
    public required DateTime PayrollMonth { get; set; }
    public string ExceptionType { get; set; } = "LHD_ED_LIMIT_EXCEEDED";
    public DateTime? AttendanceDate { get; set; }
    public string Severity { get; set; } = "BLOCKING";
    public string? Resolution { get; set; }
    public List<DateTime> AffectedDates { get; set; } = [];
    public int LhdCount { get; set; }
    public int EdCount { get; set; }
    public int CombinedOccurrenceCount { get; set; }
    public int AllowedOccurrenceCount { get; set; }
    public int ExceededOccurrenceCount { get; set; }
    public List<string> AffectedAttendanceRecordIds { get; set; } = [];
    public string Status { get; set; } = "PENDING_REVIEW";
    public string? Decision { get; set; }
    public decimal DeductionDayFraction { get; set; }
    public decimal DeductionAmount { get; set; }
    public string? Reason { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string PolicyId { get; set; } = "";
    public int PolicyVersion { get; set; }
    public int Version { get; set; } = 1;
    public string? LeaveRequestId { get; set; }
    public string? ExistingAttendanceId { get; set; }
    public string? ExistingStatus { get; set; }
    public string? RequestedLeaveStatus { get; set; }
    public int SourceVersion { get; set; }
}
