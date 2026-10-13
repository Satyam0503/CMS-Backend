namespace Codeji.CMS.DTO.PayRoll;

public sealed class PayrollReportQueryDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Status { get; set; }
    public string? RunType { get; set; }
    public string? PayGroupId { get; set; }
    public string? EmployeeId { get; set; }
    public string? LineStatus { get; set; }
}

public sealed class PayrollReportStatusTotalDto
{
    public string Status { get; set; } = string.Empty;
    public int RunCount { get; set; }
    public int EmployeeCount { get; set; }
    public int BlockedCount { get; set; }
    public int SkippedCount { get; set; }
    public decimal GrossTotal { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal NetTotal { get; set; }
}

public sealed class PayrollReportSummaryDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int RunCount { get; set; }
    public int EmployeeCount { get; set; }
    public int BlockedCount { get; set; }
    public int SkippedCount { get; set; }
    public decimal GrossTotal { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal NetTotal { get; set; }
    public List<PayrollReportStatusTotalDto> StatusTotals { get; set; } = [];
}

public sealed class PayrollRegisterRowDto
{
    public string RunId { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime PayDate { get; set; }
    public string RunType { get; set; } = string.Empty;
    public string RunStatus { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string? PayGroupId { get; set; }
    public string LineStatus { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal NetAmount { get; set; }
}
public sealed class PayrollStatutoryRegisterRowDto
{
    public string RunId { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime PayDate { get; set; }
    public string RunType { get; set; } = string.Empty;
    public string RunStatus { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string? PolicyProfileId { get; set; }
    public int PolicyProfileVersion { get; set; }
    public decimal EmployeeEpf { get; set; }
    public decimal EmployerEpf { get; set; }
    public decimal EmployeeEsic { get; set; }
    public decimal EmployerEsic { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal ProfessionalTax { get; set; }
    public decimal TotalEmployeeStatutory { get; set; }
}
