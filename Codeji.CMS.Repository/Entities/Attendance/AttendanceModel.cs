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

    public int LateCount { get; set; } = 0;

    public int EarlyExitCount { get; set; } = 0;

}
