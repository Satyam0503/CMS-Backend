namespace Codeji.CMS.DTO.Attendance;

public sealed class EmployeeOfficeScheduleAssignmentDto
{
    public string UserId { get; set; } = string.Empty;
    public string? ScheduleId { get; set; }
    public string? ScheduleName { get; set; }
    public string Source { get; set; } = "CompanyDefault";
}

public sealed class DepartmentOfficeScheduleAssignmentDto
{
    public string DepartmentId { get; set; } = string.Empty;
    public string? ScheduleId { get; set; }
    public string? ScheduleName { get; set; }
}

public sealed class SetOfficeScheduleAssignmentRequest
{
    // Null removes the direct mapping: the employee then inherits the
    // department schedule, or the company default when no department mapping exists.
    public string? ScheduleId { get; set; }
}

/// <summary>
/// Resolves the shift that applies to employees on one business date.  Company
/// identity deliberately is not part of this contract; it is taken from the
/// authenticated request on the server.
/// </summary>
public sealed class EffectiveOfficeSchedulesRequest
{
    public List<string> UserIds { get; set; } = [];
    public DateTime AttendanceDate { get; set; }
}

public sealed class EffectiveOfficeScheduleDto
{
    public string UserId { get; set; } = string.Empty;
    public string? ScheduleId { get; set; }
    public string? ScheduleName { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? PunchInAllowedFrom { get; set; }
    public string? PunchInAllowedUntil { get; set; }
    public string? PunchOutAllowedFrom { get; set; }
    public string? PunchOutAllowedUntil { get; set; }
    public string Source { get; set; } = "CompanyDefault";
}
