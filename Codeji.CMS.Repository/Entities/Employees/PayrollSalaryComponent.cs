using MongoDB.Bson.Serialization.Attributes;

namespace Codeji.CMS.Repository.Entities.Employees;

[BsonIgnoreExtraElements]
public sealed class PayrollSalaryComponentDefinition
{
    [BsonId(IdGenerator = typeof(UniqueIdGenerator))]
    public string Id { get; set; } = string.Empty;
    public required string CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
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

[BsonIgnoreExtraElements]
public sealed class PayrollSalaryTemplateComponent
{
    public string? ComponentId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
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
