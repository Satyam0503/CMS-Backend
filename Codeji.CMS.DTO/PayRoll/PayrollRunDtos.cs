using System.ComponentModel.DataAnnotations;

namespace Codeji.CMS.DTO.PayRoll;

public sealed class CreatePayrollRunRequestDto
{
    [Required] public DateTime PeriodStart { get; set; }
    [Required] public DateTime PeriodEnd { get; set; }
    [Required] public DateTime PayDate { get; set; }
    public string RunType { get; set; } = "REGULAR";
    public string PopulationType { get; set; } = "ALL_ELIGIBLE";
    public string? DepartmentId { get; set; }
    public string? PayGroupId { get; set; }
    public List<string> EmployeeIds { get; set; } = [];
    public string? IdempotencyKey { get; set; }
}

public sealed class ExcludePayrollRunEmployeeRequestDto
{
    [Required, StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class ReopenPayrollRunRequestDto
{
    [Required, StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class CreateCorrectionRunRequestDto
{
    [Required, StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
    public DateTime? PayDate { get; set; }
}

public sealed class CancelPayrollRunRequestDto
{
    [Required, StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class PayrollRunDto
{
    public string Id { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime PayDate { get; set; }
    public string RunType { get; set; } = string.Empty;
    public string? ParentRunId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PopulationType { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string? PayGroupId { get; set; }
    public string? IdempotencyKey { get; set; }
    public int EmployeeCount { get; set; }
    public int ReadyCount { get; set; }
    public int BlockedCount { get; set; }
    public int SkippedCount { get; set; }
    public decimal GrossTotal { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal NetTotal { get; set; }
    public DateTime? LegacyPayrollRowsSynchronizedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class PayrollComponentSnapshotDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string CalculationType { get; set; } = string.Empty;
    public string? BaseComponentCode { get; set; }
    public decimal Value { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsRecurring { get; set; } = true;
    public bool IsStatutory { get; set; }
    public bool IsEmployerContribution { get; set; }
    public decimal? Percentage { get; set; }
    public string? TaxExemptionSection { get; set; }
    public string ProrationBasis { get; set; } = "ATTENDANCE";
    public string? GlAccountCode { get; set; }
    public bool IncludeInPfWage { get; set; }
    public bool IncludeInEsiWage { get; set; }
    public bool IncludeInPtWage { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PayrollEmployeeInputSnapshotDto
{
    public string? AssignmentId { get; set; }
    public int AssignmentVersion { get; set; }
    public string? SalaryStructureId { get; set; }
    public int SalaryStructureVersion { get; set; }
    public List<PayrollSalaryStructureSnapshotDto> SalaryStructures { get; set; } = [];
    public string? SalaryTemplateId { get; set; }
    public int SalaryTemplateVersion { get; set; }
    public string? PayGroupId { get; set; }
    public int PayGroupVersion { get; set; }
    public string? PolicyProfileId { get; set; }
    public int PolicyProfileVersion { get; set; }
    public string EpfMode { get; set; } = string.Empty;
    public decimal EpfWageCeiling { get; set; }
    public decimal EpfEmployeeRatePercent { get; set; }
    public decimal EpfEmployerRatePercent { get; set; }
    public string EsicMode { get; set; } = string.Empty;
    public decimal EsicWageCeiling { get; set; }
    public decimal EsicEmployeeRatePercent { get; set; }
    public decimal EsicEmployerRatePercent { get; set; }
    public string TdsMode { get; set; } = string.Empty;
    public bool? RequireAttendanceLock { get; set; }
    public List<PayrollComponentSnapshotDto> Components { get; set; } = [];
}

public sealed class PayrollSalaryStructureSnapshotDto
{
    public string SalaryStructureId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? PayGroupId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public List<PayrollComponentSnapshotDto> Components { get; set; } = [];
}

public sealed class PayrollStatutorySnapshotDto
{
    public string? PolicyProfileId { get; set; }
    public int PolicyProfileVersion { get; set; }
    public decimal EmployeeEpf { get; set; }
    public decimal EmployerEpf { get; set; }
    public decimal EmployeeEsic { get; set; }
    public decimal EmployerEsic { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal ProfessionalTax { get; set; }
}

public sealed class PayrollAttendanceSnapshotDto
{
    public string? SummaryId { get; set; }
    public int SummaryVersion { get; set; }
    public bool IsApproved { get; set; }
    public bool IsLocked { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }
    public int ExpectedWorkingDays { get; set; }
    public int EligibleWorkingDays { get; set; }
    public decimal PresentDays { get; set; }
    public decimal WfhDays { get; set; }
    public decimal PaidLeaveDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public decimal HalfDays { get; set; }
    public decimal AbsentDays { get; set; }
    public decimal PaidDays { get; set; }
    public decimal UnpaidDays { get; set; }
    public int LhdCount { get; set; }
    public int EdCount { get; set; }
    public int CombinedLhdEdCount { get; set; }
    public decimal LossOfPayDays { get; set; }
    public decimal LossOfPayAmount { get; set; }
    public decimal Divisor { get; set; }
    public string DivisorPolicy { get; set; } = "CALENDAR_DAYS";
}
public sealed class PayrollRunEmployeeDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? InclusionReason { get; set; }
    public string? ExclusionReason { get; set; }
    public string? ErrorMessage { get; set; }
    public PayrollEmployeeInputSnapshotDto InputSnapshot { get; set; } = new();
    public PayrollStatutorySnapshotDto StatutorySnapshot { get; set; } = new();
    public PayrollAttendanceSnapshotDto AttendanceSnapshot { get; set; } = new();
}

