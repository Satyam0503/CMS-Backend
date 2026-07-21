using System.ComponentModel.DataAnnotations;

public class WeeklyOffSettingDto
{
    [MinLength(1, ErrorMessage = "At least one weekly off day is required.")]
    public List<int> OffDays { get; set; } = [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday];
}
