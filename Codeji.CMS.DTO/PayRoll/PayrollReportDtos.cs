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

public sealed class Form16CertificateDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Pan { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public string AssessmentYear { get; set; } = string.Empty;
    public string Regime { get; set; } = "NEW_115BAC";
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }

    // Section 17 Salary Break-up
    public decimal GrossSalarySection17_1 { get; set; }
    public decimal ValueOfPerquisitesSection17_2 { get; set; }
    public decimal ProfitsInLieuSection17_3 { get; set; }
    public decimal TotalGrossSalary { get; set; }

    // Section 10 Exemptions
    public decimal ExemptionHraSection10_13A { get; set; }
    public decimal TotalExemptionsSection10 { get; set; }
    public decimal TotalSalaryAfterSection10 { get; set; }

    // Section 16 Deductions
    public decimal StandardDeductionSection16_ia { get; set; }
    public decimal ProfessionalTaxSection16_iii { get; set; }
    public decimal TotalDeductionsSection16 { get; set; }
    public decimal IncomeChargeableUnderSalaries { get; set; }

    // Other Income reported
    public decimal OtherIncomeReported { get; set; }
    public decimal GrossTotalIncome { get; set; }

    // Chapter VI-A Deductions
    public decimal Section80C { get; set; }
    public decimal Section80Ccd1B { get; set; }
    public decimal Section80D { get; set; }
    public decimal Section24HomeLoanInterest { get; set; }
    public decimal Section80E { get; set; }
    public decimal OtherDeductionsChapterVIA { get; set; }
    public decimal TotalChapterVIADeductions { get; set; }

    // Computation of Tax
    public decimal TotalTaxableIncome { get; set; }
    public decimal TaxOnTotalIncome { get; set; }
    public decimal RebateSection87A { get; set; }
    public decimal Surcharge { get; set; }
    public decimal HealthAndEducationCess { get; set; }
    public decimal TotalTaxPayable { get; set; }
    public decimal ReliefSection89 { get; set; }
    public decimal NetTaxPayable { get; set; }
    public decimal TotalTdsDeducted { get; set; }
    public decimal TotalTdsDeposited { get; set; }
}
