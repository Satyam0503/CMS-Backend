namespace Codeji.CMS.DTO.Employee;

public sealed class ChangeEmployeeAvailabilityRequest
{
    public string EmployeeUserId { get; set; } = string.Empty;
    public string AvailabilityStatus { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public string? ReasonCode { get; set; }
    public string? Reason { get; set; }
}

public sealed class ChangeEmployeeLifecycleDimensionRequest
{
    public string EmployeeUserId { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public string? Reason { get; set; }
}

public sealed class ChangeEmployeeLifecycleRequest
{
    public string EmployeeUserId { get; set; } = string.Empty;
    public string AvailabilityStatus { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = string.Empty;
    public string PayrollStatus { get; set; } = string.Empty;
    public string? AlumniStatus { get; set; }
    public int Version { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public string? ReasonCode { get; set; }
    public string? Reason { get; set; }
}
