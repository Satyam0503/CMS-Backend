namespace Codeji.CMS.DTO.Employee;

public class BulkImportEmployeesResponseDto
{
    public int Total { get; set; }
    public int CreatedCount { get; set; }
    public int FailedCount { get; set; }
    public List<BulkImportEmployeeRowResultDto> Results { get; set; } = [];
}

public class BulkImportEmployeeRowResultDto
{
    public int RowNumber { get; set; }
    public string EmpId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = "Failed";
    public string Message { get; set; } = string.Empty;
    public string? CreatedUserId { get; set; }
}
