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
public sealed class PayrollRunEmployeeDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? InclusionReason { get; set; }
    public string? ExclusionReason { get; set; }
    public string? ErrorMessage { get; set; }
    public PayrollEmployeeInputSnapshotDto InputSnapshot { get; set; } = new();
    public PayrollStatutorySnapshotDto StatutorySnapshot { get; set; } = new();
}

