
using System.ComponentModel.DataAnnotations;
using Codeji.CMS.Utility.Enums;

public class AttendanceUpdateDto
{
    public string?CheckInTime {get;set;}
    public string? CheckOutTime { get; set; }
    [Required]
    public string Status { get; set; } = "P";
    public string? Remarks { get; set; }
    public DateTime Date { get; set; }
}
