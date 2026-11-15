namespace Codeji.CMS.DTO.Dashboard;

public sealed class DashboardOverviewDto
{
    public int TotalEmployees { get; set; }
    public decimal? TotalEmployeesChangePercent { get; set; }
    public int PresentToday { get; set; }
    public int OnLeaveToday { get; set; }
    public int AbsentToday { get; set; }
    public bool IsWorkingDay { get; set; }
    public int OpenPositions { get; set; }
    public List<HeadcountPointDto> HeadcountTrend { get; set; } = [];
    public List<DashboardActivityDto> RecentActivities { get; set; } = [];
}

public sealed class HeadcountPointDto
{
    /// <summary>yyyy-MM</summary>
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class DashboardActivityDto
{
    /// <summary>joined | leaveApproved | salaryRevised | exitInitiated | jobPosted</summary>
    public string Type { get; set; } = string.Empty;
    public string? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime OccurredAt { get; set; }
}
