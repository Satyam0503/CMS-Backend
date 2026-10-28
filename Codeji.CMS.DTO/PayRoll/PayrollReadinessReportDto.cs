namespace Codeji.CMS.DTO.PayRoll;

public sealed class PayrollReadinessReportDto
{
    public bool IsReady { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime TargetPeriod { get; set; }
    public List<string> BlockingIssues { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public int TotalActiveEmployees { get; set; }
    public int ConfiguredEmployeesCount { get; set; }
    public int MissingAssignmentCount { get; set; }
    public int MissingSalaryStructureCount { get; set; }
    public bool IsAttendanceLocked { get; set; }
    public List<PayrollSetupStepStatusDto> Steps { get; set; } = [];
}

public sealed class PayrollSetupStepStatusDto
{
    public string StepKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "READY"; // "READY", "WARNING", "BLOCKED"
    public string Details { get; set; } = string.Empty;
    public bool IsBlocking { get; set; }
}
