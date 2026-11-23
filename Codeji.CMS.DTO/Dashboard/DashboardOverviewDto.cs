namespace Codeji.CMS.DTO.Dashboard;

public sealed class DashboardOverviewDto
{
    public const string CurrentDefinitionVersion = "dashboard-overview.v4";
    public const string TimeZoneId = "Asia/Kolkata";

    public string DefinitionVersion { get; set; } = CurrentDefinitionVersion;
    public DateOnly AsOfDate { get; set; }
    public string TimeZone { get; set; } = TimeZoneId;
    public DateTime GeneratedAtUtc { get; set; }
    public DashboardSourceAvailabilityDto? SourceAvailability { get; set; }
    public int TotalEmployees { get; set; }
    public decimal? TotalEmployeesChangePercent { get; set; }
    public int PresentToday { get; set; }
    public int OnLeaveToday { get; set; }
    public int AbsentToday { get; set; }
    public AttendanceBreakdownDto? AttendanceBreakdown { get; set; } = new();
    public bool IsWorkingDay { get; set; }
    public int OpenPositions { get; set; }
    public List<HeadcountPointDto> HeadcountTrend { get; set; } = [];
    public List<WorkforceMovementPointDto> WorkforceMovementTrend { get; set; } = [];
    public List<LeaveRequestCohortPointDto> LeaveRequestCohortTrend { get; set; } = [];
    public List<DashboardActivityDto> RecentActivities { get; set; } = [];
}

/// <summary>
/// Company-level source module switches for dashboard metrics. These flags do not grant
/// user access; the endpoint's authentication and per-user permission checks still apply.
/// </summary>
public sealed class DashboardSourceAvailabilityDto
{
    public bool Employees { get; set; }
    public bool Attendance { get; set; }
    public bool LeaveManagement { get; set; }
    public bool Jobs { get; set; }
    public bool Offboarding { get; set; }
}

/// <summary>
/// Mutually exclusive employee-day buckets for the attendance visualization.
/// Every expected employee belongs to exactly one bucket on a working day.
/// </summary>
public sealed class AttendanceBreakdownDto
{
    public const string CurrentDefinitionVersion = "attendance-breakdown.v1";

    public string DefinitionVersion { get; set; } = CurrentDefinitionVersion;
    public int ExpectedEmployees { get; set; }
    public int PresentOnly { get; set; }
    public int LeaveOnly { get; set; }
    public int PresentAndOnLeave { get; set; }
    public int ExplicitlyAbsent { get; set; }
    public int OtherStatus { get; set; }
    public int Unreported { get; set; }
}

public sealed class HeadcountPointDto
{
    /// <summary>yyyy-MM</summary>
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>Monthly hire and completed departure events by their effective business date.</summary>
public sealed class WorkforceMovementPointDto
{
    /// <summary>yyyy-MM</summary>
    public string Month { get; set; } = string.Empty;
    public int Hires { get; set; }
    public int Exits { get; set; }
    public int NetChange => Hires - Exits;
}

/// <summary>Request submission cohorts grouped by their current status at query time.</summary>
public sealed class LeaveRequestCohortPointDto
{
    /// <summary>yyyy-MM</summary>
    public string Month { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Withdrawn { get; set; }
    public int OtherStatus { get; set; }
}

public sealed class DashboardActivityDto
{
    /// <summary>joined | leaveApproved | exitInitiated | jobPosted</summary>
    public string Type { get; set; } = string.Empty;
    public string? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime OccurredAt { get; set; }
}
