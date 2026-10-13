using MongoDB.Bson.Serialization.Attributes;

public class AttendanceStatusSetting
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }
    public bool RequiresTime { get; set; }
    /// <summary>Allows this no-time attendance code to be selected by leave policies.</summary>
    public bool IsAvailableForLeaveManagement { get; set; }
    /// <summary>Theme-safe hexadecimal display color used by attendance surfaces.</summary>
    public string ColorHex { get; set; } = "#607D8B";
    public decimal PaidDayFraction { get; set; } = 1m;
    public decimal UnpaidDayFraction { get; set; }
}
