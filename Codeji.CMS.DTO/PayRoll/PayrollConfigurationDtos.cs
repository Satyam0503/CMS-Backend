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
    public bool IsTaxable { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PayrollPayGroupDto
{
    public string? Id { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string Schedule { get; set; } = "MONTHLY";
    public string? LegalEntityId { get; set; }
    public string? PolicyProfileId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; }
}

public sealed class EmployeePayrollAssignmentDto
{
    public string? Id { get; set; }
    [Required] public string EmployeeId { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? PayGroupId { get; set; }
    public string? SalaryTemplateId { get; set; }
    public string? PolicyProfileId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
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
    public string EsicMode { get; set; } = "STATUTORY";
    public decimal EsicWageCeiling { get; set; } = 21000m;
    public string TdsMode { get; set; } = "AUTO_NEW_REGIME";
    public string StatutoryJurisdiction { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public string StatutoryRuleVersion { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SalaryTemplateDto
{
    public string? Id { get; set; }
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

