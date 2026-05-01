using System.ComponentModel.DataAnnotations;
using Codeji.CMS.Utility.Enums;

public class AdminAttendanceCreateDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string? EmployeeId { get; set; }  
    [Required]
    public DateTime Date { get; set; }

    public string? CheckInTime { get; set; }
    public string? CheckOutTime { get; set; }

    [Required]
    public AttendanceStatus Status { get; set; }

    public string? Remarks { get; set; }
}


