using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.Attendance;

public sealed class AttendanceCorrectionRequestDto
{
    [Required] public DateTime AttendanceDate { get; set; }
    [Required, StringLength(1000)] public string Reason { get; set; } = string.Empty;
}

public sealed class AttendanceCorrectionReviewDto
{
    [Required, RegularExpression("^(Approved|Rejected|NeedsInformation)$")] public string Resolution { get; set; } = string.Empty;
    [StringLength(1000)] public string? Note { get; set; }
}
