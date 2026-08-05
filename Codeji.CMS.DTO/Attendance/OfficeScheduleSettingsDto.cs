using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Attendance;

public sealed class OfficeScheduleSettingsDto
{
    public string? ScheduleId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "Standard Office Schedule";
    [Required] public string StandardCheckIn { get; set; } = "09:00";
    [Required] public string StandardCheckOut { get; set; } = "18:00";
    [Required] public string CheckInAllowedFrom { get; set; } = "08:30";
    [Required] public string CheckInAllowedUntil { get; set; } = "10:00";
    [Required] public string CheckOutAllowedFrom { get; set; } = "17:30";
    [Required] public string CheckOutAllowedUntil { get; set; } = "19:00";
    [Range(0, 180)] public int BreakMinutes { get; set; } = 60;
    [Range(1, 24 * 60)] public int RequiredWorkingMinutes { get; set; } = 480;
}
