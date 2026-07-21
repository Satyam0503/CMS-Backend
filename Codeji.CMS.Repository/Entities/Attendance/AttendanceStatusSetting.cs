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
    public decimal PaidDayFraction { get; set; } = 1m;
    public decimal UnpaidDayFraction { get; set; }
}
