namespace Codeji.CMS.DTO.PayRoll;

public sealed class EmployeeSalaryImportRowDto
{
    public int RowNumber { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string Currency { get; set; } = "INR";
    public string PaymentFrequency { get; set; } = "MONTHLY";
    public decimal? AnnualCtc { get; set; }
    public string? AnnualCtcSource { get; set; }
    public decimal? MonthlyGross { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? SalaryTemplateId { get; set; }
    public string? PayGroupId { get; set; }
    public string? EmployeeType { get; set; }
    public List<EmployeeSalaryComponentValueDto> Components { get; set; } = [];
}

public sealed class EmployeeSalaryImportPreviewRowDto
{
    public int RowNumber { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
}

public sealed class EmployeeSalaryImportBatchDto
{
    public string BatchId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SkippedRows { get; set; }
    public int? FailedRowNumber { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
public sealed class EmployeeSalaryPdfImportParseResult
{
    public EmployeeSalaryImportRowDto? Row { get; set; }
    public List<string> Errors { get; set; } = [];
}
