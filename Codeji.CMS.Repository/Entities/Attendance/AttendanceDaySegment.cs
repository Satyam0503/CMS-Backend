using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Attendance;

/// <summary>
/// Additive ownership record for one half of an attendance business day.  Daily
/// AttendanceModel records remain the compatibility representation for full-day
/// workflows; a date that has segments is calculated from these records instead.
/// </summary>
public sealed class AttendanceDaySegment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AttendanceDaySegmentId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    // FIRST_HALF or SECOND_HALF. The database unique index enforces one owner.
    public string Segment { get; set; } = string.Empty;
    [BsonSerializer(typeof(AttendanceStatusCodeSerializer))]
    public string Status { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public int SourceVersion { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal? TotalHours { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
