namespace Codeji.CMS.DTO.Employee;

public class OrgChartNodeDto
{
    public string UserId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfileUrl { get; set; }
    public string JobRoleId { get; set; } = string.Empty;
    public string JobRoleTitle { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? ReportingManagerUserId { get; set; }
    public string? ReportingManagerName { get; set; }
    public int DirectReportsCount { get; set; }
    public int TotalTeamCount { get; set; }
    public List<OrgChartNodeDto> DirectReports { get; set; } = new();
}

public class OrgChartResponseDto
{
    public List<OrgChartNodeDto> Roots { get; set; } = new();
    public int TotalEmployeesCount { get; set; }
    public int TotalDepartmentsCount { get; set; }
    public List<OrgChartDepartmentSummaryDto> Departments { get; set; } = new();
}

public class OrgChartDepartmentSummaryDto
{
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
}

