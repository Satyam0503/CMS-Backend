using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.Calendar;

public class CalendarResponseDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public DateTime? StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
    public string Description { get; set; }
    public CalendarResponseItem Type { get; set; }
    public string? ImageUrl { get; set; }
    public bool Recurring { get; set; }
    public string[]? TargetUserIds { get; set; }
    public bool RestrictToTargetUsersAndHrAdmin { get; set; }
}

