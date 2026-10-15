using System;
using System.Collections.Generic;

namespace Codeji.CMS.DTO.Attendance;

public class AttendanceImportTemplateRequestDto
{
    public int Year { get; set; }
    public int Month { get; set; }
}

public class AttendanceImportFileRequestDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public bool PreserveApprovedLeaves { get; set; } = true;
}

public class AttendanceImportIssueDto
{
    public int RowNumber { get; set; }
    public string? ColumnHeader { get; set; }
    public string? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? Date { get; set; }
    public string Severity { get; set; } = "Error"; // "Error" or "Warning"
    public string Message { get; set; } = string.Empty;
}

public class AttendanceImportValidationResultDto
{
    public bool IsValid { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalDaysInMonth { get; set; }
    public int TotalCellsProcessed { get; set; }
    public int ValidEntriesCount { get; set; }
    public int PreservedLeavesCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public List<AttendanceImportIssueDto> Issues { get; set; } = [];
}

public class AttendanceImportCommitResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int EmployeesAffected { get; set; }
    public int RecordsUpserted { get; set; }
    public int PreservedLeavesCount { get; set; }
    public List<AttendanceImportIssueDto> Issues { get; set; } = [];
}

