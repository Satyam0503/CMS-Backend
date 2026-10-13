using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

public class AttendanceModel
{
   [BsonId]
[BsonRepresentation(BsonType.ObjectId)]
public string? AttendanceId { get; set; }


    public string? UserId { get; set; }

    public string? EmployeeId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public string? SourceId { get; set; }
    public int SourceVersion { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }



    public decimal? TotalHours { get; set; }

    [BsonSerializer(typeof(AttendanceStatusCodeSerializer))]
    public string Status { get; set; } = "P";

    public string?Remarks { get; set; }
    // Structured remarks are additive. Remarks remains the compatibility field consumed by
    // existing calendar, payroll and API clients.
    public string? RemarkCode { get; set; }
    public string? RemarkText { get; set; }
    public string? SystemRemark { get; set; }
    public string? OperatorRemark { get; set; }
    public int BreakMinutes { get; set; }
    public string? ScheduleId { get; set; }
    public long? ScheduleVersion { get; set; }
    public string? MarkedByUserId { get; set; }
    public DateTime? MarkedAtUtc { get; set; }
    public string? ModifiedByUserId { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public long Version { get; set; }

    public int LateCount { get; set; } = 0;

    public int EarlyExitCount { get; set; } = 0;

}
