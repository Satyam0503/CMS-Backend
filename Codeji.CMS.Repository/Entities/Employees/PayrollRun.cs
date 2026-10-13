using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

[BsonIgnoreExtraElements]
public sealed class PayrollRun
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;
    public required string CompanyId { get; set; }
    public required DateTime PeriodStart { get; set; }
    public required DateTime PeriodEnd { get; set; }
    public required DateTime PayDate { get; set; }
    public string RunType { get; set; } = "REGULAR";
    public string Status { get; set; } = "DRAFT";
    public bool IsActive { get; set; } = true;
    public string PopulationType { get; set; } = "ALL_ELIGIBLE";
    public string? DepartmentId { get; set; }
    public string? PayGroupId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? RequestFingerprint { get; set; }
    public int Version { get; set; } = 1;
    public PayrollConfigurationSnapshot ConfigurationSnapshot { get; set; } = new();
    public int EmployeeCount { get; set; }
    public int ReadyCount { get; set; }
    public int BlockedCount { get; set; }
    public int SkippedCount { get; set; }
    public decimal GrossTotal { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal NetTotal { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewComment { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public List<PayrollRunAuditEvent> AuditHistory { get; set; } = [];
}

[BsonIgnoreExtraElements]
public sealed class PayrollConfigurationSnapshot
{
    public string? DivisorPolicyId { get; set; }
    public int DivisorPolicyVersion { get; set; }
    public string DivisorPolicy { get; set; } = "CALENDAR_DAYS";
    public string LopCalculationBase { get; set; } = "FIXED_GROSS";
    public string EpfMode { get; set; } = "CAPPED_CEILING";
    public decimal EpfWageCeiling { get; set; }
    public decimal EpfEmployeeRatePercent { get; set; } = 12m;
    public decimal EpfEmployerRatePercent { get; set; } = 12m;
    public string EsicMode { get; set; } = "STATUTORY";
    public decimal EsicWageCeiling { get; set; } = 21000m;
    public string TdsMode { get; set; } = "AUTO_NEW_REGIME";
    public decimal EsicEmployeeRatePercent { get; set; } = 0.75m;
    public decimal EsicEmployerRatePercent { get; set; } = 3.25m;
    public bool RequireAttendanceLock { get; set; }
    public DateTime CapturedAtUtc { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class PayrollRunEmployee
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;
    public required string CompanyId { get; set; }
    public required string PayrollRunId { get; set; }
    public required string EmployeeId { get; set; }
    public string Status { get; set; } = "READY";
    public string? InclusionReason { get; set; }
    public string? ExclusionReason { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SalaryRecordId { get; set; }
    public string? PayGroupId { get; set; }
    public string? DepartmentId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public PayrollEmployeeInputSnapshot InputSnapshot { get; set; } = new();
    public PayrollStatutorySnapshot StatutorySnapshot { get; set; } = new();
}

[BsonIgnoreExtraElements]
public sealed class PayrollEmployeeInputSnapshot
{
    public string? AssignmentId { get; set; }
    public int AssignmentVersion { get; set; }
    public string? SalaryStructureId { get; set; }
    public int SalaryStructureVersion { get; set; }
    public List<PayrollSalaryStructureSnapshot> SalaryStructures { get; set; } = [];
    public string? SalaryTemplateId { get; set; }
    public int SalaryTemplateVersion { get; set; }
    public string? PayGroupId { get; set; }
    public int PayGroupVersion { get; set; }
    public string? PolicyProfileId { get; set; }
    public int PolicyProfileVersion { get; set; }
    public string EpfMode { get; set; } = string.Empty;
    public decimal EpfWageCeiling { get; set; } = 15000m;
    public decimal EpfEmployeeRatePercent { get; set; } = 12m;
    public decimal EpfEmployerRatePercent { get; set; } = 12m;
    public string EsicMode { get; set; } = string.Empty;
    public decimal EsicWageCeiling { get; set; } = 21000m;
    public decimal EsicEmployeeRatePercent { get; set; } = 0.75m;
    public decimal EsicEmployerRatePercent { get; set; } = 3.25m;
    public string TdsMode { get; set; } = string.Empty;
    public bool? RequireAttendanceLock { get; set; }
    public List<PayrollComponentSnapshot> Components { get; set; } = [];
}

public sealed class PayrollStatutorySnapshot
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
public sealed class PayrollComponentSnapshot
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string Kind { get; set; } = "EARNING";
    public string CalculationType { get; set; } = "FIXED";
    public string? BaseComponentCode { get; set; }
    public decimal Value { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsRecurring { get; set; } = true;
    public bool IsStatutory { get; set; }
    public bool IsEmployerContribution { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PayrollSalaryStructureSnapshot
{
    public string SalaryStructureId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? PayGroupId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public List<PayrollComponentSnapshot> Components { get; set; } = [];
}

public sealed class PayrollRunAuditEvent
{
    public required string Action { get; set; }
    public required string ActorId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? Notes { get; set; }
}


