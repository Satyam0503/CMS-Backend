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
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }



    public decimal? TotalHours { get; set; }

    [BsonRepresentation(BsonType.Int32)]
    public AttendanceStatus Status { get; set; } = AttendanceStatus.P;

    public string?Remarks { get; set; }

    public int LateCount { get; set; } = 0;

    public int EarlyExitCount { get; set; } = 0;

}
