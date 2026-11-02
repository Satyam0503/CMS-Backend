using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.PayRoll;

public sealed class PayrollSalaryComponentDto
{
    public string? Id { get; set; }
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "EARNING";
    public string CalculationType { get; set; } = "FIXED";
    public string? BaseComponentCode { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsStatutory { get; set; }
    public bool IsEmployerContribution { get; set; }
    public bool IsRecurring { get; set; } = true;
    public string? TaxExemptionSection { get; set; }
    public string ProrationBasis { get; set; } = "ATTENDANCE";
    public string? GlAccountCode { get; set; }
    public bool IncludeInPfWage { get; set; }
    public bool IncludeInEsiWage { get; set; }
    public bool IncludeInPtWage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}

public sealed class PayrollSalaryTemplateComponentDto
{
    public string? ComponentId { get; set; }
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "EARNING";
    public string CalculationType { get; set; } = "FIXED";
    public string? BaseComponentCode { get; set; }
    public decimal Value { get; set; }
    public decimal? Percentage { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsStatutory { get; set; }
    public bool IsEmployerContribution { get; set; }
    public bool IsRecurring { get; set; } = true;
    public string? TaxExemptionSection { get; set; }
    public string ProrationBasis { get; set; } = "ATTENDANCE";
    public string? GlAccountCode { get; set; }
    public bool IncludeInPfWage { get; set; }
    public bool IncludeInEsiWage { get; set; }
    public bool IncludeInPtWage { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PayrollPayGroupDto
{
    public string? Id { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string Schedule { get; set; } = "MONTHLY";
    public string? LegalEntityId { get; set; }
    public string? PolicyProfileId { get; set; }
    public string? WorkLocationState { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; }
}

public sealed class EmployeePayrollAssignmentDto
{
    public string? Id { get; set; }
    public int Version { get; set; }
    [Required] public string EmployeeId { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public bool IsRejected { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? PayGroupId { get; set; }
    public string? SalaryTemplateId { get; set; }
    public string? PolicyProfileId { get; set; }
    public string? WorkLocationState { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public sealed class PayrollAssignmentRejectionDto
{
    [Required] public string EmployeeId { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    [Required, MaxLength(500)] public string Reason { get; set; } = string.Empty;
}
public sealed class PayrollPolicyProfileDto
{
    public string? Id { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string DivisorPolicy { get; set; } = "CALENDAR_DAYS";
    public string Scope { get; set; } = "COMPANY_DEFAULT";
    public bool RequireAttendanceLock { get; set; } = true;
    public string EpfMode { get; set; } = "CAPPED_CEILING";
    public decimal EpfWageCeiling { get; set; } = 15000m;
    public decimal EpfEmployeeRatePercent { get; set; } = 12m;
    public decimal EpfEmployerRatePercent { get; set; } = 12m;
    public string EsicMode { get; set; } = "STATUTORY";
    public decimal EsicWageCeiling { get; set; } = 21000m;
    public decimal EsicEmployeeRatePercent { get; set; } = 0.75m;
    public decimal EsicEmployerRatePercent { get; set; } = 3.25m;
    public string TdsMode { get; set; } = "AUTO_NEW_REGIME";
    public string PtMode { get; set; } = "STATUTORY";
    public string StatutoryJurisdiction { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public string StatutoryRuleVersion { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public bool IsRejected { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SalaryTemplateDto
{
    public string? Id { get; set; }
    public string? SourceTemplateId { get; set; }
    public int? SourceTemplateVersion { get; set; }
    public bool IsRetired { get; set; }
    public string? RetiredBy { get; set; }
    public DateTime? RetiredAtUtc { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string? PayGroupId { get; set; }
    public string? DepartmentId { get; set; }
    public string? EmployeeType { get; set; }
    public string? GradeId { get; set; }
    public decimal? BasicPay { get; set; }
    public decimal? Hra { get; set; }
    public decimal? Lta { get; set; }
    public decimal? OtherAllowance { get; set; }
    public decimal? Bonus { get; set; }
    public List<PayrollSalaryTemplateComponentDto> Components { get; set; } = [];
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
}
public sealed class SalaryTemplatePreviewDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal MonthlyGross { get; set; }
    public decimal AnnualGross { get; set; }
    public decimal MonthlyEmployeeDeductions { get; set; }
    public decimal AnnualEmployeeDeductions { get; set; }
    public decimal MonthlyEmployerContributions { get; set; }
    public decimal AnnualEmployerContributions { get; set; }
    public decimal MonthlyTotalCtc { get; set; }
    public decimal AnnualTotalCtc { get; set; }
    public decimal IndicativeMonthlyTakeHome { get; set; }
    public decimal IndicativeAnnualTakeHome { get; set; }
    public Dictionary<string, decimal> ComponentBreakdown { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string ExplanatoryNote { get; set; } = "Indicative take-home excludes attendance loss of pay, tax declarations, and state statutory changes which are determined dynamically at payroll run execution time.";
}
