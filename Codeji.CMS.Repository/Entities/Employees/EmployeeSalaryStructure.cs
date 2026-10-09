using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

[BsonIgnoreExtraElements]
public sealed class EmployeeSalaryStructure
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;
    public required string CompanyId { get; set; }
    public required string EmployeeId { get; set; }
    public string? UserId { get; set; }
    public string? OfferId { get; set; }
    public int? OfferVersion { get; set; }
    public string? SalaryTemplateId { get; set; }
    public string? PayGroupId { get; set; }
    public string? EmployeeType { get; set; }
    public string? GradeId { get; set; }
    public string? DepartmentId { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentFrequency { get; set; } = "MONTHLY";
    public decimal? AnnualCtc { get; set; }
    public string? AnnualCtcSource { get; set; }
    public decimal? MonthlyGross { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string ApprovalStatus { get; set; } = "DRAFT";
    public string Source { get; set; } = "MANUAL";
    public int Version { get; set; } = 1;
    public string? CreatedBy { get; set; }
    public string? SubmittedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public List<EmployeeSalaryComponentValue> Components { get; set; } = [];
}

[BsonIgnoreExtraElements]
public sealed class EmployeeSalaryComponentValue
{
    public required string ComponentCode { get; set; }
    public required string ComponentName { get; set; }
    public string Kind { get; set; } = "EARNING";
    public decimal Value { get; set; }
    public decimal? Percentage { get; set; }
    public string CalculationType { get; set; } = "FIXED";
    public string? BaseComponentCode { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsRecurring { get; set; } = true;
    public bool IsStatutory { get; set; }
    public bool IsEmployerContribution { get; set; }
}
