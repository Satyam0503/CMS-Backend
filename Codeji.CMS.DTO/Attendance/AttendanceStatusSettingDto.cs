using System.ComponentModel.DataAnnotations;

public class AttendanceStatusSettingDto
{
    public string? Id { get; set; }
    [Required, RegularExpression("^[A-Z0-9+_-]{1,20}$")]
    public required string Code { get; set; }
    [Required, MaxLength(100)]
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }
    public bool RequiresTime { get; set; }
    [Range(0, 1)] public decimal PaidDayFraction { get; set; } = 1m;
    [Range(0, 1)] public decimal UnpaidDayFraction { get; set; }
}
