using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

[BsonIgnoreExtraElements]
public sealed class PayrollPayGroup
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;
    public required string CompanyId { get; set; }
    public required string Name { get; set; }
    public string Schedule { get; set; } = "MONTHLY";
    public string? LegalEntityId { get; set; }
    public string? PolicyProfileId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Version { get; set; } = 1;
}

[BsonIgnoreExtraElements]
public sealed class PayrollPolicyProfile
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;
    public required string CompanyId { get; set; }
    public required string Name { get; set; }
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

[BsonIgnoreExtraElements]
public sealed class SalaryTemplate
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;
    public required string CompanyId { get; set; }
    public required string Name { get; set; }
    public string? PayGroupId { get; set; }
    public string? DepartmentId { get; set; }
    public string? EmployeeType { get; set; }
    public string? GradeId { get; set; }
    public decimal? BasicPay { get; set; }
    public decimal? Hra { get; set; }
    public decimal? Lta { get; set; }
    public decimal? OtherAllowance { get; set; }
    public decimal? Bonus { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public List<PayrollSalaryTemplateComponent> Components { get; set; } = [];
}

[BsonIgnoreExtraElements]
public sealed class EmployeePayrollAssignment
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;
    public required string CompanyId { get; set; }
    public required string EmployeeId { get; set; }
    public string? PayGroupId { get; set; }
    public string? SalaryTemplateId { get; set; }
    public string? PolicyProfileId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
}
